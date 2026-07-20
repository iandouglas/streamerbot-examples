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
        CPH.SetGlobalVar("battleship_join_ends_at", 0L, false);

        // Clear muted players — their timeout lasted through this round, now it's over
        CPH.SetGlobalVar("battleship_muted_players", "[]", false);

        // Get all coordinates for this round
        var coords = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_coords", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";

        int gridSize = CPH.GetGlobalVar<int>("battleship_grid_size", false);
        if (gridSize < 1) gridSize = 10;

        // No coordinates submitted
        if (coords.Count == 0)
        {
            Log("tick: no coordinates submitted this round");
            id736.Chat.SendMessageToAllPlatforms("No coordinates were seen, no shot fired.");

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

        // Platform teams mode: resolve one target per platform
        bool platformTeams = CPH.GetGlobalVar<bool>("battleship_platform_teams", false);
        if (platformTeams)
        {
            ResolveTeamsRound(coords, mode, gridSize);
            return true;
        }

        int targetRow;
        int targetCol;

        if (mode == "extreme")
        {
            // Extreme mode uses the averaged coordinate.
            double rowSum = 0, colSum = 0;
            foreach (var c in coords)
            {
                rowSum += Convert.ToInt32(c["row"]);
                colSum += Convert.ToInt32(c["col"]);
            }

            double rowAvg = rowSum / (double)coords.Count;
            double colAvg = colSum / (double)coords.Count;
            targetRow = (int)Math.Floor(rowAvg + 0.5);
            targetCol = (int)Math.Floor(colAvg + 0.5);
            targetRow = Math.Max(0, Math.Min(gridSize - 1, targetRow));
            targetCol = Math.Max(0, Math.Min(gridSize - 1, targetCol));
            Log($"tick: average of {coords.Count} coords = {(char)('A' + targetRow)}-{targetCol + 1}");
        }
        else
        {
            // Easy/normal modes use the most common coordinate from the round.
            int roundSeed = CPH.GetGlobalVar<int>("battleship_round_seed", false);
            var coordCounts = new Dictionary<string, int>();
            foreach (var c in coords)
            {
                int row = Convert.ToInt32(c["row"]);
                int col = Convert.ToInt32(c["col"]);
                string key = $"{row},{col}";
                coordCounts[key] = coordCounts.TryGetValue(key, out int existing) ? existing + 1 : 1;
            }

            int bestCount = -1;
            int bestIndex = int.MaxValue;
            uint bestHash = 0;
            targetRow = 0;
            targetCol = 0;

            for (int i = 0; i < coords.Count; i++)
            {
                int row = Convert.ToInt32(coords[i]["row"]);
                int col = Convert.ToInt32(coords[i]["col"]);
                string key = $"{row},{col}";
                int count = coordCounts[key];
                uint hash = HashTieBreak(roundSeed, row, col);

                if (count > bestCount || (count == bestCount && (hash > bestHash || (hash == bestHash && i < bestIndex))))
                {
                    bestCount = count;
                    bestIndex = i;
                    bestHash = hash;
                    targetRow = row;
                    targetCol = col;
                }
            }

            Log($"tick: most common of {coords.Count} coords ({coordCounts.Count} unique) = {(char)('A' + targetRow)}-{targetCol + 1} ({bestCount} votes)");
        }

        string coordName = $"{(char)('A' + targetRow)}-{targetCol + 1}";
        id736.Chat.SendMessageToAllPlatforms($"Firing on position {coordName}!");

        // Send round-end with final coordinate
        SendEvent("round-end", new Dictionary<string, object>
        {
            { "row", targetRow },
            { "col", targetCol }
        });

        // Check if already shot
        var shots = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_shots", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();

        string shotKey = $"{targetRow},{targetCol}";
        if (shots.ContainsKey(shotKey))
        {
            Log($"tick: {coordName} already fired upon, skipping round");
            id736.Chat.SendMessageToAllPlatforms($"Already fired on {coordName}! Skipping this round.");

            SendEvent("shot-resolve", new Dictionary<string, object>
            {
                { "row", targetRow },
                { "col", targetCol },
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
            if (mRow == targetRow && mCol == targetCol)
            {
                mineIdx = i;
                break;
            }
        }

        if (mineIdx >= 0)
        {
            ResolveMineHit(targetRow, targetCol, mineIdx, mines, coords, shots, shotKey);
            return true;
        }

        // Check ship hit
        int shipIdx = -1;
        int cellIdx = -1;
        Log($"tick: checking {ships.Count} ships for hit at {targetRow},{targetCol}");
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
                if (cRow == targetRow && cCol == targetCol)
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
            Log($"tick: HIT on ship {shipIdx} cell {cellIdx} at {targetRow},{targetCol}");
            ResolveShipHit(targetRow, targetCol, shipIdx, cellIdx, ships, coords, shots, shotKey);
            return true;
        }

        // Water miss
        Log($"tick: {coordName} is a miss");
        shots[shotKey] = new Dictionary<string, object> { { "row", targetRow }, { "col", targetCol }, { "result", "miss" } };
        CPH.SetGlobalVar("battleship_shots", id736.Data.ToJson(shots), false);

        SendEvent("shot-resolve", new Dictionary<string, object>
        {
            { "row", targetRow },
            { "col", targetCol },
            { "result", "miss" }
        });

        return true;
    }

    // ===== Platform teams mode =====

    private void ResolveTeamsRound(List<Dictionary<string, object>> coords, string mode, int gridSize)
    {
        // Group coords by platform
        var byPlatform = new Dictionary<string, List<Dictionary<string, object>>>();
        foreach (var c in coords)
        {
            string platform = (c["platform"]?.ToString() ?? "twitch").ToLowerInvariant();
            if (!byPlatform.ContainsKey(platform))
                byPlatform[platform] = new List<Dictionary<string, object>>();
            byPlatform[platform].Add(c);
        }

        int roundSeed = CPH.GetGlobalVar<int>("battleship_round_seed", false);

        // Resolve each platform's target
        var platformTargets = new Dictionary<string, Tuple<int, int>>(); // platform -> (row,col)
        foreach (var kvp in byPlatform)
        {
            var target = ResolvePlatformTarget(kvp.Key, kvp.Value, mode, gridSize, roundSeed);
            if (target != null)
                platformTargets[kvp.Key] = target;
            Log($"tick[teams]: {kvp.Key} target = {(char)('A' + target.Item1)}-{target.Item2 + 1} ({kvp.Value.Count} coords)");
        }

        if (platformTargets.Count == 0)
        {
            Log("tick[teams]: no platform targets resolved");
            SendEvent("round-end", new Dictionary<string, object> { { "row", 0 }, { "col", 0 } });
            SendEvent("shot-resolve", new Dictionary<string, object>
            {
                { "row", 0 }, { "col", 0 }, { "result", "no-coords" }
            });
            return;
        }

        // Dedupe target cells -> one plane per distinct cell. Map cell -> platforms that targeted it.
        var cellPlatforms = new Dictionary<string, List<string>>(); // "row,col" -> [platforms]
        foreach (var kvp in platformTargets)
        {
            string key = $"{kvp.Value.Item1},{kvp.Value.Item2}";
            if (!cellPlatforms.ContainsKey(key))
                cellPlatforms[key] = new List<string>();
            cellPlatforms[key].Add(kvp.Key);
        }

        // Announce each cell
        foreach (var kvp in cellPlatforms)
        {
            var parts = kvp.Key.Split(',');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);
            string coordName = $"{(char)('A' + row)}-{col + 1}";
            string platList = string.Join(", ", kvp.Value);
            id736.Chat.SendMessageToAllPlatforms($"Firing on position {coordName} ({platList})!");
        }

        // Send round-end with first target (browser uses cells array for animation)
        var firstCell = platformTargets.First().Value;
        SendEvent("round-end", new Dictionary<string, object>
        {
            { "row", firstCell.Item1 },
            { "col", firstCell.Item2 }
        });

        // Load shots / ships / mines once
        var shots = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_shots", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();
        var ships = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_ships", false) ?? "[]") ?? new List<Dictionary<string, object>>();
        var mines = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_mines", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        // Determine result per distinct cell
        var cellsPayload = new List<Dictionary<string, object>>();
        string worstResult = "miss"; // for audio: mine > hit > miss
        bool anyShipSunk = false;
        bool anyRepeat = false;

        // Pending state — store per-platform roundParticipants for bomber-complete to use
        var pendingTeamsResolve = new List<Dictionary<string, object>>();

        foreach (var kvp in cellPlatforms)
        {
            var parts = kvp.Key.Split(',');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);
            string shotKey = kvp.Key;
            var platformsForCell = kvp.Value;

            string result;
            bool shipSunk = false;
            int shipIdx = -1;
            int cellIdx = -1;
            int mineIdx = -1;

            // Check if already shot
            if (shots.ContainsKey(shotKey))
            {
                result = "repeat";
                anyRepeat = true;
            }
            else
            {
                // Check mine
                for (int i = 0; i < mines.Count; i++)
                {
                    if (Convert.ToInt32(mines[i]["row"]) == row && Convert.ToInt32(mines[i]["col"]) == col)
                    {
                        mineIdx = i;
                        break;
                    }
                }

                if (mineIdx >= 0)
                {
                    result = "mine";
                }
                else
                {
                    // Check ship
                    for (int i = 0; i < ships.Count; i++)
                    {
                        object cellsObj;
                        if (!ships[i].TryGetValue("cells", out cellsObj) || cellsObj == null) continue;
                        List<object> cellsList = null;
                        if (cellsObj is JArray ja) cellsList = ja.Cast<object>().ToList();
                        else if (cellsObj is List<object> lo) cellsList = lo;
                        if (cellsList == null) continue;

                        for (int j = 0; j < cellsList.Count; j++)
                        {
                            int cRow = GetCellIntProperty(cellsList, j, "row");
                            int cCol = GetCellIntProperty(cellsList, j, "col");
                            if (cRow == row && cCol == col)
                            {
                                shipIdx = i;
                                cellIdx = j;
                                break;
                            }
                        }
                        if (shipIdx >= 0) break;
                    }

                    result = shipIdx >= 0 ? "hit" : "miss";
                }
            }

            // Track worst result for audio
            if (result == "mine") worstResult = "mine";
            else if (result == "hit" && worstResult != "mine") worstResult = "hit";
            else if (result == "repeat" && worstResult == "miss") worstResult = "repeat";

            // Determine shipSunk for hit
            if (result == "hit")
            {
                object cellsObj = ships[shipIdx]["cells"];
                List<object> cellsList = null;
                if (cellsObj is JArray ja) cellsList = ja.Cast<object>().ToList();
                else if (cellsObj is List<object> lo) cellsList = lo;

                SetCellProperty(cellsList, cellIdx, "hit", true);

                bool allHit = true;
                for (int i = 0; i < cellsList.Count; i++)
                {
                    if (!GetCellBoolProperty(cellsList, i, "hit")) { allHit = false; break; }
                }
                if (allHit)
                {
                    ships[shipIdx]["sunk"] = true;
                    shipSunk = true;
                    anyShipSunk = true;
                }
            }

            // Record shot
            if (result != "repeat")
            {
                shots[shotKey] = new Dictionary<string, object>
                {
                    { "row", row }, { "col", col }, { "result", result }, { "shipSunk", shipSunk }
                };
            }

            // Build cell payload for browser
            var cellEntry = new Dictionary<string, object>
            {
                { "row", row },
                { "col", col },
                { "platforms", platformsForCell },
                { "result", result },
                { "shipSunk", shipSunk }
            };
            if (shipSunk)
            {
                cellEntry["shipId"] = shipIdx;
                bool allSunk = sunkCountAllShips(ships);
                cellEntry["allShipsSunk"] = allSunk;
            }
            cellsPayload.Add(cellEntry);

            // Stash per-cell resolve info for bomber-complete
            pendingTeamsResolve.Add(new Dictionary<string, object>
            {
                { "row", row }, { "col", col }, { "result", result }, { "shipSunk", shipSunk },
                { "shipIdx", shipIdx }, { "mineIdx", mineIdx },
                { "platforms", platformsForCell }
            });
        }

        // Persist ships/shots/mines
        CPH.SetGlobalVar("battleship_ships", id736.Data.ToJson(ships), false);
        CPH.SetGlobalVar("battleship_shots", id736.Data.ToJson(shots), false);

        // For mine hits: mark mines, apply penalties + mute per offending platform
        bool anyMineHit = pendingTeamsResolve.Any(p => (p["result"]?.ToString() == "mine"));
        if (anyMineHit)
        {
            ApplyTeamsMineEffects(pendingTeamsResolve, mines, byPlatform);
        }

        // For hits: award points only to platforms whose target hit
        foreach (var cellInfo in pendingTeamsResolve)
        {
            if (cellInfo["result"]?.ToString() == "hit")
                AwardTeamsHitPoints((List<string>)cellInfo["platforms"], byPlatform);
        }

        // Update platform hits totals
        UpdatePlatformHits(pendingTeamsResolve, byPlatform);

        // Update sunk count + pending game end
        int sunkCount = 0;
        foreach (var s in ships) if (Convert.ToBoolean(s["sunk"])) sunkCount++;
        CPH.SetGlobalVar("battleship_ships_sunk", sunkCount, false);
        if (sunkCountAllShips(ships))
        {
            Log("tick[teams]: all ships sunk, game win!");
            CPH.SetGlobalVar("battleship_pending_game_end", "win", false);
        }

        // Pending chat messages for bomber-complete to announce after animation
        if (anyShipSunk)
        {
            string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
            int hitPoints = mode == "easy"
                ? CPH.GetGlobalVar<int>("battleship_hit_points_easy", false)
                : mode == "extreme"
                    ? CPH.GetGlobalVar<int>("battleship_hit_points_extreme", false)
                    : CPH.GetGlobalVar<int>("battleship_hit_points_normal", false);
            CPH.SetGlobalVar("battleship_pending_chat_hit", $"{hitPoints} {pointsName}", false);

            // Sunk announcement(s)
            var sunkShips = pendingTeamsResolve
                .Where(p => p["result"]?.ToString() == "hit" && Convert.ToBoolean(p["shipSunk"]))
                .Select(p => Convert.ToInt32(p["shipIdx"]))
                .Distinct()
                .Select(idx => ships[idx]["name"]?.ToString() ?? "Ship")
                .ToList();
            if (sunkShips.Count > 0)
            {
                string sunkMsg = $"{string.Join(", ", sunkShips)} sunk! {sunkCount}/5 ships down!";
                CPH.SetGlobalVar("battleship_pending_chat_sunk", sunkMsg, false);
            }
        }

        // Store pending teams resolve so bomber-complete can finalize (mine mute already applied here)
        CPH.SetGlobalVar("battleship_pending_teams_resolve", id736.Data.ToJson(pendingTeamsResolve), false);

        // Send shot-resolve with cells array + worstResult audio hint
        SendEvent("shot-resolve", new Dictionary<string, object>
        {
            { "row", firstCell.Item1 },
            { "col", firstCell.Item2 },
            { "result", worstResult },
            { "cells", cellsPayload },
            { "teams", true }
        });

        Log($"tick[teams]: resolved {cellsPayload.Count} distinct cell(s), worstResult={worstResult}, anyRepeat={anyRepeat}");
    }

    private Tuple<int, int> ResolvePlatformTarget(string platform, List<Dictionary<string, object>> coords, string mode, int gridSize, int roundSeed)
    {
        if (coords.Count == 0) return null;

        if (mode == "extreme")
        {
            double rowSum = 0, colSum = 0;
            foreach (var c in coords)
            {
                rowSum += Convert.ToInt32(c["row"]);
                colSum += Convert.ToInt32(c["col"]);
            }
            int row = (int)Math.Floor(rowSum / (double)coords.Count + 0.5);
            int col = (int)Math.Floor(colSum / (double)coords.Count + 0.5);
            row = Math.Max(0, Math.Min(gridSize - 1, row));
            col = Math.Max(0, Math.Min(gridSize - 1, col));
            return Tuple.Create(row, col);
        }

        // Easy/normal: most-voted among this platform's coords, seeded tie-break
        var coordCounts = new Dictionary<string, int>();
        foreach (var c in coords)
        {
            int r = Convert.ToInt32(c["row"]);
            int col2 = Convert.ToInt32(c["col"]);
            string key = $"{r},{col2}";
            coordCounts[key] = coordCounts.TryGetValue(key, out int ex) ? ex + 1 : 1;
        }

        int bestCount = -1;
        int bestIndex = int.MaxValue;
        uint bestHash = 0;
        int bestRow = 0;
        int bestCol = 0;
        for (int i = 0; i < coords.Count; i++)
        {
            int r = Convert.ToInt32(coords[i]["row"]);
            int col2 = Convert.ToInt32(coords[i]["col"]);
            string key = $"{r},{col2}";
            int count = coordCounts[key];
            uint hash = HashTieBreak(roundSeed, r, col2);
            if (count > bestCount || (count == bestCount && (hash > bestHash || (hash == bestHash && i < bestIndex))))
            {
                bestCount = count;
                bestIndex = i;
                bestHash = hash;
                bestRow = r;
                bestCol = col2;
            }
        }
        return Tuple.Create(bestRow, bestCol);
    }

    private void AwardTeamsHitPoints(List<string> platformsThatHit, Dictionary<string, List<Dictionary<string, object>>> byPlatform)
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

        foreach (string platform in platformsThatHit)
        {
            if (!byPlatform.ContainsKey(platform)) continue;
            var players = byPlatform[platform];
            foreach (var p in players)
            {
                string user = p["user"]?.ToString() ?? "";
                string userKey = $"{platform}:{user}";
                if (!stats.ContainsKey(userKey)) continue;
                bool playedThisRound = stats[userKey].TryGetValue(roundKey, out object pt) && Convert.ToBoolean(pt);
                if (!playedThisRound) continue;

                string displayName = playerNames.TryGetValue(userKey, out string nm) ? nm : user;
                try
                {
                    if (Enum.TryParse(platform, true, out Platform plat))
                        id736.Points.Add(displayName, plat, pointsName, hitPoints);
                }
                catch (Exception ex)
                {
                    Log($"teams award error {displayName}@{platform}: {ex.Message}");
                }
            }
        }
        Log($"teams award: {hitPoints} {pointsName} to platforms {string.Join(",", platformsThatHit)}");
    }

    private void ApplyTeamsMineEffects(List<Dictionary<string, object>> pendingResolve, List<Dictionary<string, object>> mines, Dictionary<string, List<Dictionary<string, object>>> byPlatform)
    {
        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();
        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        int minePenalty = CPH.GetGlobalVar<int>("battleship_mine_penalty", false);
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";

        var mutedPlayers = new List<Dictionary<string, object>>();
        var mineNames = new List<string>();

        foreach (var cellInfo in pendingResolve)
        {
            if (cellInfo["result"]?.ToString() != "mine") continue;
            int row = Convert.ToInt32(cellInfo["row"]);
            int col = Convert.ToInt32(cellInfo["col"]);
            int mineIdx = Convert.ToInt32(cellInfo["mineIdx"]);
            var platforms = (List<string>)cellInfo["platforms"];

            // Mark mine hit
            int minesHit = CPH.GetGlobalVar<int>("battleship_mines_hit", false) + 1;
            CPH.SetGlobalVar("battleship_mines_hit", minesHit, false);
            mines[mineIdx]["hit"] = true;
            mineNames.Add($"{(char)('A' + row)}-{col + 1}");

            foreach (string platform in platforms)
            {
                if (!byPlatform.ContainsKey(platform)) continue;
                var players = byPlatform[platform];

                // Mute floor(half) of this platform's round participants
                var roundPlayers = new List<Dictionary<string, object>>();
                var seen = new HashSet<string>();
                foreach (var p in players)
                {
                    string u = p["user"]?.ToString() ?? "";
                    string k = $"{platform}:{u}";
                    if (seen.Add(k))
                        roundPlayers.Add(new Dictionary<string, object> { { "user", u }, { "platform", platform } });
                }
                int muteCount = (int)Math.Floor(roundPlayers.Count / 2.0);
                if (muteCount < 1 && roundPlayers.Count > 0) muteCount = 1;
                var shuffled = roundPlayers.OrderBy(x => Guid.NewGuid()).ToList();
                for (int i = 0; i < muteCount && i < shuffled.Count; i++)
                {
                    mutedPlayers.Add(shuffled[i]);
                    string u = shuffled[i]["user"]?.ToString() ?? "";
                    string uk = $"{platform}:{u}";
                    if (stats.ContainsKey(uk))
                        stats[uk]["muteCount"] = (stats[uk].ContainsKey("muteCount") ? Convert.ToInt32(stats[uk]["muteCount"]) : 0) + 1;
                }

                // Penalty
                if (minePenalty > 0 && mode == "extreme")
                {
                    foreach (var p in players)
                    {
                        string u = p["user"]?.ToString() ?? "";
                        string uk = $"{platform}:{u}";
                        string displayName = playerNames.TryGetValue(uk, out string nm) ? nm : u;
                        try
                        {
                            if (Enum.TryParse(platform, true, out Platform plat))
                                id736.Points.Subtract(displayName, plat, pointsName, minePenalty, false);
                        }
                        catch (Exception ex)
                        {
                            Log($"teams mine penalty error {displayName}@{platform}: {ex.Message}");
                        }
                    }
                }
            }
        }

        CPH.SetGlobalVar("battleship_mines", id736.Data.ToJson(mines), false);
        CPH.SetGlobalVar("battleship_muted_players", id736.Data.ToJson(mutedPlayers), false);
        CPH.SetGlobalVar("battleship_player_stats", id736.Data.ToJson(stats), false);

        // Pending mine chat
        if (mutedPlayers.Count > 0)
        {
            var names = mutedPlayers.Select(m => $"@{m["user"]}").ToList();
            string msg = $"Mine hit at {string.Join(", ", mineNames)}! Muted next round: {string.Join(" ", names)}";
            if (minePenalty > 0 && mode == "extreme")
                msg += $" All participants lose {minePenalty} {pointsName}!";
            CPH.SetGlobalVar("battleship_pending_chat_mine", msg, false);
            Log($"teams: stored pending chat mine msg");
        }

        // Check game lost
        int minesHitTotal = CPH.GetGlobalVar<int>("battleship_mines_hit", false);
        if (minesHitTotal >= mines.Count && mode == "extreme")
        {
            Log("tick[teams]: all mines hit in extreme mode, game lost!");
            CPH.SetGlobalVar("battleship_pending_game_end", "lost", false);
        }
    }

    private void UpdatePlatformHits(List<Dictionary<string, object>> pendingResolve, Dictionary<string, List<Dictionary<string, object>>> byPlatform)
    {
        var platformHits = id736.Data.FromJson<Dictionary<string, int>>(
            CPH.GetGlobalVar<string>("battleship_platform_hits", false) ?? "{}") ?? new Dictionary<string, int>();

        // Ensure all platforms that joined are tracked
        foreach (string platform in byPlatform.Keys)
        {
            if (!platformHits.ContainsKey(platform))
                platformHits[platform] = 0;
        }

        foreach (var cellInfo in pendingResolve)
        {
            if (cellInfo["result"]?.ToString() != "hit") continue;
            foreach (string platform in (List<string>)cellInfo["platforms"])
            {
                if (!platformHits.ContainsKey(platform)) platformHits[platform] = 0;
                platformHits[platform] += 1;
            }
        }
        CPH.SetGlobalVar("battleship_platform_hits", id736.Data.ToJson(platformHits), false);
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
            string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
            if (minePenalty > 0 && mode == "extreme")
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
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        if (minePenalty <= 0 || mode != "extreme") return;

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

    private static uint HashTieBreak(int seed, int row, int col)
    {
        unchecked
        {
            uint hash = 2166136261;
            string value = $"{seed}:{row},{col}";
            foreach (char ch in value)
            {
                hash ^= ch;
                hash *= 16777619;
            }
            return hash;
        }
    }
}
