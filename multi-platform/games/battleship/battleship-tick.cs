using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "battleship");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        Log($"tick: fired, game_active={CPH.GetGlobalVar<bool>("battleship_game_active", false)}, collecting={CPH.GetGlobalVar<bool>("battleship_collecting", false)}");

        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
            return true;

        if (!CPH.GetGlobalVar<bool>("battleship_collecting", false))
            return true;

        // Capture timer GUID on first run
        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (string.IsNullOrWhiteSpace(timerGuid) && CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId))
        {
            timerGuid = currentTimerId.ToString();
            CPH.SetGlobalVar("battleship_timer_guid", timerGuid, false);
        }

        // Stop collecting
        CPH.SetGlobalVar("battleship_collecting", false, false);
        CPH.SetGlobalVar("battleship_phase", "resolving", false);

        // Clear muted players — their timeout lasted through this round, now it's over
        CPH.SetGlobalVar("battleship_muted_players", "[]", false);

        // Get all coordinates for this round
        var coords = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_coords", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        int gridSize = CPH.GetGlobalVar<int>("battleship_grid_size", false);
        if (gridSize < 1) gridSize = 10;

        // No coordinates submitted
        if (coords.Count == 0)
        {
            Log("tick: no coordinates submitted this round");
            id736.Chat.SendMessage("No coordinates were seen, no shot fired.");

            SendEvent("round-end", new Dictionary<string, object>
            {
                { "row", 0 },
                { "col", 0 }
            });

            // Send shot-resolve with no-coords result
            SendEvent("shot-resolve", new Dictionary<string, object>
            {
                { "row", 0 },
                { "col", 0 },
                { "result", "no-coords" }
            });
            return true;
        }

        // Compute average
        double rowSum = 0, colSum = 0;
        foreach (var c in coords)
        {
            rowSum += Convert.ToInt32(c["row"]);
            colSum += Convert.ToInt32(c["col"]);
        }
        // Compute average using round-half-up (not banker's rounding) to match JS
        double rowAvg = rowSum / (double)coords.Count;
        double colAvg = colSum / (double)coords.Count;
        int avgRow = (int)Math.Floor(rowAvg + 0.5);
        int avgCol = (int)Math.Floor(colAvg + 0.5);
        avgRow = Math.Max(0, Math.Min(gridSize - 1, avgRow));
        avgCol = Math.Max(0, Math.Min(gridSize - 1, avgCol));

        string coordName = $"{(char)('A' + avgRow)}-{avgCol + 1}";
        Log($"tick: average of {coords.Count} coords = {coordName}");
        id736.Chat.SendMessage($"Firing on position {coordName}!");

        // Send round-end with final coordinate
        SendEvent("round-end", new Dictionary<string, object>
        {
            { "row", avgRow },
            { "col", avgCol }
        });

        // Check if already shot
        var shots = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_shots", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();

        string shotKey = $"{avgRow},{avgCol}";
        if (shots.ContainsKey(shotKey))
        {
            Log($"tick: {coordName} already fired upon, skipping round");
            id736.Chat.SendMessage($"Already fired on {coordName}! Skipping this round.");

            SendEvent("shot-resolve", new Dictionary<string, object>
            {
                { "row", avgRow },
                { "col", avgCol },
                { "result", "repeat" }
            });
            return true;
        }

        // Resolve the shot
        var ships = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_ships", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        var mines = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_mines", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        // Check mine hit first
        int mineIdx = -1;
        for (int i = 0; i < mines.Count; i++)
        {
            int mRow = Convert.ToInt32(mines[i]["row"]);
            int mCol = Convert.ToInt32(mines[i]["col"]);
            if (mRow == avgRow && mCol == avgCol)
            {
                mineIdx = i;
                break;
            }
        }

        if (mineIdx >= 0)
        {
            ResolveMineHit(avgRow, avgCol, mineIdx, mines, coords, shots, shotKey);
            return true;
        }

        // Check ship hit
        int shipIdx = -1;
        int cellIdx = -1;
        Log($"tick: checking {ships.Count} ships for hit at {avgRow},{avgCol}");
        for (int i = 0; i < ships.Count; i++)
        {
            object cellsObj;
            if (!ships[i].TryGetValue("cells", out cellsObj) || cellsObj == null)
            {
                Log($"tick: ship {i} has no cells property");
                continue;
            }

            List<object> cells = null;
            if (cellsObj is JArray ja)
                cells = ja.Cast<object>().ToList();
            else if (cellsObj is List<object> lo)
                cells = lo;

            if (cells == null)
            {
                Log($"tick: ship {i} cells is null after conversion");
                continue;
            }

            for (int j = 0; j < cells.Count; j++)
            {
                int cRow = GetCellIntProperty(cells, j, "row");
                int cCol = GetCellIntProperty(cells, j, "col");
                if (cRow == avgRow && cCol == avgCol)
                {
                    shipIdx = i;
                    cellIdx = j;
                    break;
                }
            }
            if (shipIdx >= 0) break;
        }

        if (shipIdx >= 0)
        {
            Log($"tick: HIT on ship {shipIdx} cell {cellIdx} at {avgRow},{avgCol}");
            ResolveShipHit(avgRow, avgCol, shipIdx, cellIdx, ships, coords, shots, shotKey);
            return true;
        }

        // Water miss
        Log($"tick: {coordName} is a miss");
        shots[shotKey] = new Dictionary<string, object> { { "row", avgRow }, { "col", avgCol }, { "result", "miss" } };
        CPH.SetGlobalVar("battleship_shots", id736.Data.ToJson(shots), false);

        SendEvent("shot-resolve", new Dictionary<string, object>
        {
            { "row", avgRow },
            { "col", avgCol },
            { "result", "miss" }
        });

        return true;
    }

    private void ResolveShipHit(int row, int col, int shipIdx, int cellIdx,
        List<Dictionary<string, object>> ships, List<Dictionary<string, object>> coords,
        Dictionary<string, Dictionary<string, object>> shots, string shotKey)
    {
        // Mark cell as hit directly on the JArray/JObject structure
        object cellsObj = ships[shipIdx]["cells"];
        IEnumerable<object> cellsEnum = null;
        if (cellsObj is JArray ja)
            cellsEnum = ja;
        else if (cellsObj is List<object> lo)
            cellsEnum = lo;

        if (cellsEnum == null)
        {
            Log($"ResolveShipHit: could not get cells for ship {shipIdx}");
            return;
        }

        var cellsList = cellsEnum.ToList();
        // Mark the specific cell as hit
        SetCellProperty(cellsList, cellIdx, "hit", true);

        CPH.SetGlobalVar("battleship_ships", id736.Data.ToJson(ships), false);

        // Award points to all round participants (no chat message yet — delayed below)
        AwardRoundPoints();

        // Check if ship is fully sunk
        bool allHit = true;
        for (int i = 0; i < cellsList.Count; i++)
        {
            bool isHit = GetCellBoolProperty(cellsList, i, "hit");
            if (!isHit)
            {
                allHit = false;
                break;
            }
        }

        bool shipSunk = false;
        int sunkCount = 0;
        if (allHit)
        {
            ships[shipIdx]["sunk"] = true;
            CPH.SetGlobalVar("battleship_ships", id736.Data.ToJson(ships), false);

            sunkCount = CPH.GetGlobalVar<int>("battleship_ships_sunk", false) + 1;
            CPH.SetGlobalVar("battleship_ships_sunk", sunkCount, false);

            string shipName = ships[shipIdx]["name"]?.ToString() ?? "Ship";
            Log($"tick: {shipName} sunk! ({sunkCount}/5)");
            shipSunk = true;

            if (sunkCountAllShips(ships))
            {
                Log("tick: all ships sunk, game win!");
                CPH.SetGlobalVar("battleship_pending_game_end", "win", false);
            }
        }

        string result = "hit";
        shots[shotKey] = new Dictionary<string, object>
        {
            { "row", row },
            { "col", col },
            { "result", "hit" },
            { "shipSunk", shipSunk }
        };
        CPH.SetGlobalVar("battleship_shots", id736.Data.ToJson(shots), false);

        var payload = new Dictionary<string, object>
        {
            { "row", row },
            { "col", col },
            { "result", "hit" },
            { "shipSunk", shipSunk }
        };
        if (shipSunk)
        {
            payload["shipId"] = shipIdx;
            bool allSunk = sunkCountAllShips(ships);
            payload["allShipsSunk"] = allSunk;
        }

        SendEvent("shot-resolve", payload);

        // Delay chat announcements until after the bomber animation (~10 seconds)
        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        int hitPoints = mode == "easy"
            ? CPH.GetGlobalVar<int>("battleship_hit_points_easy", false)
            : mode == "extreme"
                ? CPH.GetGlobalVar<int>("battleship_hit_points_extreme", false)
                : CPH.GetGlobalVar<int>("battleship_hit_points_normal", false);

        CPH.SetGlobalVar("battleship_pending_chat_hit", $"{hitPoints} {pointsName}", false);
        Log($"tick: stored pending chat hit msg: '{hitPoints} {pointsName}'");
        if (shipSunk)
        {
            string shipName2 = ships[shipIdx]["name"]?.ToString() ?? "Ship";
            CPH.SetGlobalVar("battleship_pending_chat_sunk", $"{shipName2} sunk! {sunkCount}/5 ships down!", false);
            Log($"tick: stored pending chat sunk msg: '{shipName2} sunk! {sunkCount}/5 ships down!'");
        }
    }

    private bool sunkCountAllShips(List<Dictionary<string, object>> ships)
    {
        foreach (var ship in ships)
        {
            if (!Convert.ToBoolean(ship["sunk"]))
                return false;
        }
        return true;
    }

    private static void SetCellProperty(List<object> cells, int idx, string prop, object value)
    {
        var cell = cells[idx];
        if (cell is JObject jo)
            jo[prop] = JToken.FromObject(value);
        else if (cell is Dictionary<string, object> dict)
            dict[prop] = value;
    }

    private static bool GetCellBoolProperty(List<object> cells, int idx, string prop)
    {
        var cell = cells[idx];
        if (cell is JObject jo)
            return jo[prop] != null && (bool)jo[prop];
        if (cell is Dictionary<string, object> dict)
            return dict.ContainsKey(prop) && Convert.ToBoolean(dict[prop]);
        return false;
    }

    private static int GetCellIntProperty(List<object> cells, int idx, string prop)
    {
        var cell = cells[idx];
        if (cell is JObject jo)
            return jo[prop] != null ? jo[prop].Value<int>() : 0;
        if (cell is Dictionary<string, object> dict)
            return dict.ContainsKey(prop) ? Convert.ToInt32(dict[prop]) : 0;
        return 0;
    }

    private void ResolveMineHit(int row, int col, int mineIdx,
        List<Dictionary<string, object>> mines, List<Dictionary<string, object>> coords,
        Dictionary<string, Dictionary<string, object>> shots, string shotKey)
    {
        mines[mineIdx]["hit"] = true;
        CPH.SetGlobalVar("battleship_mines", id736.Data.ToJson(mines), false);

        int minesHit = CPH.GetGlobalVar<int>("battleship_mines_hit", false) + 1;
        CPH.SetGlobalVar("battleship_mines_hit", minesHit, false);

        Log($"tick: mine hit at {row},{col} ({minesHit}/{mines.Count})");

        // Determine muted players (floor of half the round participants)
        var roundPlayers = new List<Dictionary<string, object>>();
        var seenUsers = new HashSet<string>();
        foreach (var c in coords)
        {
            string user = c["user"]?.ToString() ?? "";
            string platform = c["platform"]?.ToString() ?? "twitch";
            string userKey = $"{platform}:{user}";
            if (seenUsers.Add(userKey))
            {
                roundPlayers.Add(new Dictionary<string, object> { { "user", user }, { "platform", platform } });
            }
        }

        int muteCount = (int)Math.Floor(roundPlayers.Count / 2.0);
        // Ensure at least 1 player is muted if anyone played
        if (muteCount < 1 && roundPlayers.Count > 0)
            muteCount = 1;
        var mutedPlayers = new List<Dictionary<string, object>>();

        // Randomly select players to mute
        var shuffled = roundPlayers.OrderBy(x => Guid.NewGuid()).ToList();
        for (int i = 0; i < muteCount && i < shuffled.Count; i++)
        {
            mutedPlayers.Add(shuffled[i]);
        }

        CPH.SetGlobalVar("battleship_muted_players", id736.Data.ToJson(mutedPlayers), false);

        // Update mute counts in player stats
        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();

        foreach (var m in mutedPlayers)
        {
            string user = m["user"]?.ToString() ?? "";
            string platform = m["platform"]?.ToString() ?? "twitch";
            string userKey = $"{platform}:{user}";
            if (stats.ContainsKey(userKey))
            {
                stats[userKey]["muteCount"] = (stats[userKey].ContainsKey("muteCount") ? Convert.ToInt32(stats[userKey]["muteCount"]) : 0) + 1;
            }
        }
        CPH.SetGlobalVar("battleship_player_stats", id736.Data.ToJson(stats), false);

        // Apply mine penalty
        ApplyMinePenalty(roundPlayers);

        // Store muted player announcement for delayed chat (sent by bomber-complete)
        if (mutedPlayers.Count > 0)
        {
            var names = mutedPlayers.Select(m => $"@{m["user"]}").ToList();
            string nameList = string.Join(" ", names);
            string mineMsg = $"Mine hit! These players are timed out next round: {nameList}";

            int minePenalty = CPH.GetGlobalVar<int>("battleship_mine_penalty", false);
            if (minePenalty > 0)
            {
                string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
                mineMsg += $" All participants lose {minePenalty} {pointsName}!";
            }

            CPH.SetGlobalVar("battleship_pending_chat_mine", mineMsg, false);
            Log($"tick: stored pending chat mine msg");
        }

        shots[shotKey] = new Dictionary<string, object>
        {
            { "row", row },
            { "col", col },
            { "result", "mine" }
        };
        CPH.SetGlobalVar("battleship_shots", id736.Data.ToJson(shots), false);

        var mutedPayload = mutedPlayers.Select(m => new Dictionary<string, object>
        {
            { "user", m["user"] },
            { "platform", m["platform"] }
        }).ToList();

        SendEvent("shot-resolve", new Dictionary<string, object>
        {
            { "row", row },
            { "col", col },
            { "result", "mine" },
            { "mutedPlayers", mutedPayload }
        });

        // Check if all mines hit in extreme mode (game lost)
        if (minesHit >= mines.Count)
        {
            string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
            if (mode == "extreme")
            {
                Log("tick: all mines hit in extreme mode, game lost!");
                CPH.SetGlobalVar("battleship_pending_game_end", "lost", false);
            }
        }
    }

    private void AwardRoundPoints()
    {
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        int hitPoints = mode == "easy"
            ? CPH.GetGlobalVar<int>("battleship_hit_points_easy", false)
            : mode == "extreme"
                ? CPH.GetGlobalVar<int>("battleship_hit_points_extreme", false)
                : CPH.GetGlobalVar<int>("battleship_hit_points_normal", false);
        if (hitPoints <= 0) return;

        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        int round = CPH.GetGlobalVar<int>("battleship_round", false);
        string roundKey = $"round_{round}";

        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();

        // Points context already set by Core.LinkStreamerbot
        foreach (var kvp in stats)
        {
            bool playedThisRound = kvp.Value.TryGetValue(roundKey, out object pt) && Convert.ToBoolean(pt);
            if (!playedThisRound) continue;

            string userKey = kvp.Key;
            string displayName = playerNames.TryGetValue(userKey, out string name) ? name : userKey;
            var parts = userKey.Split(':');
            string platform = parts.Length > 0 ? parts[0] : "twitch";

            try
            {
                if (Enum.TryParse(platform, true, out Platform plat))
                    id736.Points.Add(displayName, plat, pointsName, hitPoints);
            }
            catch (Exception ex)
            {
                Log($"award points error for {displayName}: {ex.Message}");
            }
        }

        Log($"award: {hitPoints} {pointsName} to all round-{round} participants");
    }

    private void ApplyMinePenalty(List<Dictionary<string, object>> roundPlayers)
    {
        int minePenalty = CPH.GetGlobalVar<int>("battleship_mine_penalty", false);
        if (minePenalty <= 0) return;

        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();

        // Points context already set by Core.LinkStreamerbot
        foreach (var player in roundPlayers)
        {
            string user = player["user"]?.ToString() ?? "";
            string platform = player["platform"]?.ToString() ?? "twitch";
            string userKey = $"{platform}:{user}";
            string displayName = playerNames.TryGetValue(userKey, out string name) ? name : user;

            try
            {
                if (Enum.TryParse(platform, true, out Platform plat))
                    id736.Points.Subtract(displayName, plat, pointsName, minePenalty, false);
                Log($"mine penalty: {displayName}@{platform} -{minePenalty}");
            }
            catch (Exception ex)
            {
                Log($"mine penalty error for {displayName}: {ex.Message}");
            }
        }
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
    }
}