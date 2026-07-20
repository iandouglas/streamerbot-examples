using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
    private static readonly Random _random = new Random();

    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "battleship");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("rawInput", out string rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            Log("setup: no rawInput provided");
            return false;
        }

        string[] parts = rawInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string subCommand = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        string modeArg = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "normal";

        if (subCommand == "battleship" || subCommand == "bs")
        {
            if (modeArg == "end" || modeArg == "stop")
            {
                EndGame();
                return true;
            }
            StartGame(modeArg);
            return true;
        }

        Log($"setup: unrecognized subcommand '{subCommand}'");
        return false;
    }

    private void StartGame(string modeArg)
    {
        string mode = modeArg;
        if (mode != "easy" && mode != "normal" && mode != "extreme")
            mode = "normal";

        if (CPH.GetGlobalVar<bool>("battleship_game_active", false))
        {
            id736.Chat.SendMessage("A Battleship game is already in progress! Use !game battleship end to stop it.");
            return;
        }

        string groupName = "battleship-players";
        id736.Groups.EnsureGroup(groupName);
        id736.Groups.Clear(groupName);

        CPH.SetGlobalVar("battleship_game_active", true, false);
        CPH.SetGlobalVar("battleship_mode", mode, false);
        CPH.SetGlobalVar("battleship_phase", "join", false);
        CPH.SetGlobalVar("battleship_group", groupName, false);
        CPH.SetGlobalVar("battleship_round", 0, false);
        CPH.SetGlobalVar("battleship_round_seed", 0, false);
        CPH.SetGlobalVar("battleship_join_ends_at", 0L, false);
        CPH.SetGlobalVar("battleship_collecting", false, false);
        CPH.SetGlobalVar("battleship_coords", "[]", false);
        CPH.SetGlobalVar("battleship_shots", "{}", false);
        CPH.SetGlobalVar("battleship_muted_players", "[]", false);
        CPH.SetGlobalVar("battleship_player_stats", "{}", false);
        CPH.SetGlobalVar("battleship_player_names", "{}", false);
        CPH.SetGlobalVar("battleship_mines_hit", 0, false);
        CPH.SetGlobalVar("battleship_ships_sunk", 0, false);

        string obsScene = GetArg("obsScene", "");
        string obsSource = GetArg("obsSource", "");
        string timerGuid = GetArg("timerGuid", "");
        CPH.SetGlobalVar("battleship_obs_scene", obsScene, false);
        CPH.SetGlobalVar("battleship_obs_source", obsSource, false);
        CPH.SetGlobalVar("battleship_timer_guid", timerGuid, false);

        int gridSize = mode == "extreme" ? 15 : 10;
        CPH.SetGlobalVar("battleship_grid_size", gridSize, false);

        int roundSeconds = GetArgInt("roundSeconds", 30);
        CPH.SetGlobalVar("battleship_round_seconds", roundSeconds, false);

        int joinTimer = GetArgInt("joinTimer", 60);
        if (joinTimer < 10) joinTimer = 60;
        CPH.SetGlobalVar("battleship_join_seconds", joinTimer, false);

        int interRoundSeconds = GetArgInt("interRoundSeconds", 3);
        CPH.SetGlobalVar("battleship_inter_round_seconds", interRoundSeconds, false);

        string pointsName = GetArg("pointsName", "points");
        CPH.SetGlobalVar("battleship_points_name", pointsName, false);

        int shipHitPointsEasy = GetArgInt("shipHitPointsEasy", 100);
        int shipHitPointsNormal = GetArgInt("shipHitPointsNormal", 250);
        int shipHitPointsExtreme = GetArgInt("shipHitPointsExtreme", 500);
        CPH.SetGlobalVar("battleship_hit_points_easy", shipHitPointsEasy, false);
        CPH.SetGlobalVar("battleship_hit_points_normal", shipHitPointsNormal, false);
        CPH.SetGlobalVar("battleship_hit_points_extreme", shipHitPointsExtreme, false);

        int minePenalty = GetArgInt("minePenalty", 0);
        CPH.SetGlobalVar("battleship_mine_penalty", minePenalty, false);

        int flawlessBonusNormal = GetArgInt("flawlessBonusNormal", 1000);
        int flawlessBonusExtreme = GetArgInt("flawlessBonusExtreme", 10000);
        CPH.SetGlobalVar("battleship_flawless_bonus_normal", flawlessBonusNormal, false);
        CPH.SetGlobalVar("battleship_flawless_bonus_extreme", flawlessBonusExtreme, false);

        int minRoundsForBonus = GetArgInt("minRoundsForBonus", 5);
        CPH.SetGlobalVar("battleship_min_rounds_for_bonus", minRoundsForBonus, false);

        int normalMines = GetArgInt("normalMines", 5);
        normalMines = Math.Max(0, Math.Min(25, normalMines));
        int extremeMultiplier = GetArgInt("extremeMultiplier", 2);
        extremeMultiplier = Math.Max(1, Math.Min(5, extremeMultiplier));
        CPH.SetGlobalVar("battleship_normal_mines", normalMines, false);
        CPH.SetGlobalVar("battleship_extreme_multiplier", extremeMultiplier, false);

        bool platformTeams = GetArgBool("platformTeams", false);
        CPH.SetGlobalVar("battleship_platform_teams", platformTeams, false);
        CPH.SetGlobalVar("battleship_platform_hits", "{}", false);

        // Ship definitions: carrier 5, battleship 4, cruiser 3, submarine 3, destroyer 2
        int[] shipSizes = { 5, 4, 3, 3, 2 };
        string[] shipNames = { "Carrier", "Battleship", "Cruiser", "Submarine", "Destroyer" };

        // Place ships using backtracking
        var ships = PlaceShips(shipSizes, gridSize);
        if (ships == null)
        {
            Log("setup: failed to place ships after 100 attempts");
            id736.Chat.SendMessage("Battleship setup failed - could not place ships. Please try again.");
            CPH.SetGlobalVar("battleship_game_active", false, false);
            return;
        }

        // Place mines
        int totalShipCells = shipSizes.Sum();
        int mineCount;
        if (mode == "easy")
            mineCount = 0;
        else if (mode == "extreme")
            mineCount = totalShipCells * extremeMultiplier;
        else
            mineCount = normalMines;

        var mines = PlaceMines(ships, mineCount, gridSize, mode == "extreme");
        if (mines == null)
        {
            Log("setup: failed to place mines");
            id736.Chat.SendMessage("Battleship setup failed - could not place mines. Please try again.");
            CPH.SetGlobalVar("battleship_game_active", false, false);
            return;
        }

        // Store ship and mine data
        var shipDataList = new List<Dictionary<string, object>>();
        for (int i = 0; i < ships.Count; i++)
        {
            var shipData = new Dictionary<string, object>
            {
                { "id", i },
                { "name", shipNames[i] },
                { "size", shipSizes[i] },
                { "cells", ships[i].ConvertAll(c => new Dictionary<string, object> { { "row", c.Item1 }, { "col", c.Item2 }, { "hit", false } }) },
                { "sunk", false }
            };
            shipDataList.Add(shipData);
        }
        CPH.SetGlobalVar("battleship_ships", id736.Data.ToJson(shipDataList), false);

        var mineDataList = mines.ConvertAll(m => new Dictionary<string, object> { { "row", m.Item1 }, { "col", m.Item2 }, { "hit", false } });
        CPH.SetGlobalVar("battleship_mines", id736.Data.ToJson(mineDataList), false);

        // Debug: reveal one mine coordinate in chat for testing
        bool debugMines = GetArgInt("debugMines", 0) != 0;
        if (debugMines && mines.Count > 0)
        {
            var firstMine = mines[0];
            string mineCoord = $"{(char)('A' + firstMine.Item1)}-{firstMine.Item2 + 1}";
            id736.Chat.SendMessage($"[DEBUG] Mine at {mineCoord}");
            Log($"setup: debug mine revealed at {mineCoord}");
        }

        // Enable the coordinate command and join action
        string coordCommandName = mode == "extreme" ? "battleship-extreme" : "battleship-normal";
        Log($"setup: enabling coord command '{coordCommandName}' and join action");
        EnableCommand(coordCommandName);
        CPH.EnableAction("battleship-player-join");
        CPH.EnableAction("battleship-coord");
        Log("setup: enabled battleship-coord and battleship-player-join actions");

        // Show OBS source
        if (!string.IsNullOrWhiteSpace(obsScene) && !string.IsNullOrWhiteSpace(obsSource))
            CPH.ObsShowSource(obsScene, obsSource);

        // Give the browser source time to load and connect to WebSocket before sending setup
        CPH.Wait(3000);

        // Send setup event to browser
        SendEvent("setup", new Dictionary<string, object>
        {
            { "mode", mode },
            { "gridSize", gridSize },
            { "round", 0 },
            { "phase", "join" },
            { "joinEndsAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + joinTimer * 1000L },
            { "ships", shipDataList },
            { "mines", mineDataList },
            { "playAudio", true },
            { "platformTeams", platformTeams }
        });
        Log("setup event sent to browser");

        Log($"setup: game started mode={mode}, grid={gridSize}x{gridSize}, mines={mineCount}, roundSeconds={roundSeconds}");

        // Mode-specific welcome message
        string welcomeMsg;
        if (mode == "easy")
        {
            welcomeMsg = "Battleship (EASY mode) has started. Type !join in chat to play. Players will work together to sink 5 ships on a 10x10 grid, 30 seconds per round. Multiple shots per round are allowed.";
        }
        else if (mode == "extreme")
        {
            welcomeMsg = $"Battleship (EXTREME mode) has started. Type !join in chat to play. Players will work together to sink 5 ships and avoid {mineCount} mines on a 15x15 grid, *15* seconds per round! Multiple shots per round are allowed.";
        }
        else
        {
            welcomeMsg = $"Battleship (NORMAL mode) has started. Type !join in chat to play. Players will work together to sink 5 ships and avoid {mineCount} mines on a 10x10 grid, 30 seconds per round. Multiple shots per round are allowed.";
        }
        id736.Chat.SendMessageToAllPlatforms(welcomeMsg);

        if (platformTeams)
            id736.Chat.SendMessageToAllPlatforms("Platform teams mode on: each platform fires its own shot.");

        // --- Join phase: 60 seconds ---
        CPH.SetGlobalVar("battleship_phase", "join", false);
        long joinEndsAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + joinTimer * 1000L;
        CPH.SetGlobalVar("battleship_join_ends_at", joinEndsAt, false);

        id736.Chat.SendMessageToAllPlatforms($"You have {joinTimer} seconds to !join!");

        int elapsed = 0;
        while (elapsed < joinTimer)
        {
            int waitTime = Math.Min(10, joinTimer - elapsed);
            CPH.Wait(waitTime * 1000);
            if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
                return;
            elapsed += waitTime;
            int remaining = joinTimer - elapsed;
            if (remaining > 0)
            {
                int count = id736.Groups.Count(groupName);
                id736.Chat.SendMessageToAllPlatforms($"{remaining} seconds left to !join! Players so far: {count}");
            }
        }

        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
            return;

        int joinedCount = id736.Groups.Count(groupName);
        if (joinedCount == 0)
        {
            id736.Chat.SendMessageToAllPlatforms("No one joined! Game cancelled.");
            Log("setup: no players joined, cancelling game");
            CleanupGame();
            return;
        }

        Log($"setup: {joinedCount} players joined, starting round 1");

        // Start round 1
        StartRound();
        return;
    }

    private void StartRound()
    {
        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
            return;

        int round = CPH.GetGlobalVar<int>("battleship_round", false) + 1;
        CPH.SetGlobalVar("battleship_round", round, false);
        CPH.SetGlobalVar("battleship_phase", "collect", false);
        CPH.SetGlobalVar("battleship_collecting", true, false);
        CPH.SetGlobalVar("battleship_join_ends_at", 0L, false);
        CPH.SetGlobalVar("battleship_coords", "[]", false);
        int roundSeed = _random.Next();
        CPH.SetGlobalVar("battleship_round_seed", roundSeed, false);

        int roundSeconds = CPH.GetGlobalVar<int>("battleship_round_seconds", false);
        if (roundSeconds < 1) roundSeconds = 30;

        long endsAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + roundSeconds * 1000L;
        CPH.SetGlobalVar("battleship_round_ends_at", endsAt, false);

        // Get muted players list
        var mutedPlayers = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_muted_players", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        var mutedPayload = mutedPlayers.Select(m => new Dictionary<string, object>
        {
            { "user", m["user"] },
            { "platform", m["platform"] }
        }).ToList();

        SendEvent("round-start", new Dictionary<string, object>
        {
            { "round", round },
            { "endsAt", endsAt },
            { "duration", roundSeconds },
            { "roundSeed", roundSeed },
            { "mutedPlayers", mutedPayload }
        });
        Log($"round-start: sent round-start event to browser, round={round}, endsAt={endsAt}, duration={roundSeconds}s");
        id736.Chat.SendMessageToAllPlatforms($"Round {round}! Enter a coordinate (e.g. B5) — you have {roundSeconds} seconds!");

        // Set the timer to fire at round end
        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, roundSeconds, keepEnabled: true);
            Log($"round-start: timer {timerGuid} enabled, interval={roundSeconds}s");
        }
        else
        {
            Log("round-start: WARNING - no timerGuid set! Round will never end. Set the timerGuid subaction argument.");
            id736.Chat.SendMessage("WARNING: Battleship timer is not configured. Set the timerGuid subaction argument.");
        }

        Log($"round-start: round {round}, {roundSeconds}s, ends at {endsAt}");
    }

    private void EndGame()
    {
        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
        {
            id736.Chat.SendMessage("No Battleship game is currently running.");
            return;
        }

        Log("end: streamer ended game early");
        DoGameEnd("ended");
    }

    private void DoGameEnd(string result)
    {
        bool wasActive = CPH.GetGlobalVar<bool>("battleship_game_active", false);
        if (!wasActive)
            return;

        CPH.SetGlobalVar("battleship_game_active", false, false);
        CPH.SetGlobalVar("battleship_collecting", false, false);
        CPH.SetGlobalVar("battleship_phase", "ended", false);

        // Disable coordinate command
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        string coordCommandName = mode == "extreme" ? "battleship-extreme" : "battleship-normal";
        DisableCommand(coordCommandName);

        // Disable timer
        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
            id736.Timers.Disable(timerGuid);

        // Award points
        if (result == "win")
            AwardFlawlessBonus();

        // Get full board state for reveal
        string shipsJson = CPH.GetGlobalVar<string>("battleship_ships", false) ?? "[]";
        string minesJson = CPH.GetGlobalVar<string>("battleship_mines", false) ?? "[]";
        var ships = id736.Data.FromJson<List<Dictionary<string, object>>>(shipsJson) ?? new List<Dictionary<string, object>>();
        var mines = id736.Data.FromJson<List<Dictionary<string, object>>>(minesJson) ?? new List<Dictionary<string, object>>();

        // Send game-end event
        SendEvent("game-end", new Dictionary<string, object>
        {
            { "result", result },
            { "ships", ships },
            { "mines", mines }
        });

        // Hide OBS source after a delay
        string obsScene = CPH.GetGlobalVar<string>("battleship_obs_scene", false) ?? "";
        string obsSource = CPH.GetGlobalVar<string>("battleship_obs_source", false) ?? "";
        CPH.Wait(15000);
        if (!string.IsNullOrWhiteSpace(obsScene) && !string.IsNullOrWhiteSpace(obsSource))
            CPH.ObsHideSource(obsScene, obsSource);

        // Cleanup
        id736.Groups.Clear(CPH.GetGlobalVar<string>("battleship_group", false) ?? "battleship-players");

        string endMsg;
        if (result == "win") endMsg = "All ships sunk! Victory! Thanks for playing Battleship!";
        else if (result == "lost") endMsg = "All mines hit! The fleet is lost! Game over.";
        else endMsg = "Battleship game ended by the streamer.";

        id736.Chat.SendMessageToAllPlatforms(endMsg);
        Log($"game-end: {result}");

        // Clear global vars
        ClearGlobalVars();
    }

    private void AwardFlawlessBonus()
    {
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        int bonus = mode == "extreme"
            ? CPH.GetGlobalVar<int>("battleship_flawless_bonus_extreme", false)
            : CPH.GetGlobalVar<int>("battleship_flawless_bonus_normal", false);
        if (bonus <= 0) return;

        int minRounds = CPH.GetGlobalVar<int>("battleship_min_rounds_for_bonus", false);
        if (minRounds < 1) minRounds = 5;

        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();

        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();

        // Points context already set by Core.LinkStreamerbot
        foreach (var kvp in stats)
        {
            string userKey = kvp.Key;
            int roundsPlayed = kvp.Value.TryGetValue("roundsPlayed", out object rp) ? Convert.ToInt32(rp) : 0;
            if (roundsPlayed < minRounds) continue;

            string displayName = playerNames.TryGetValue(userKey, out string name) ? name : userKey;
            var parts = userKey.Split(':');
            string platform = parts.Length > 0 ? parts[0] : "twitch";
            string userId = parts.Length > 1 ? parts[1] : displayName;

            try
            {
                if (Enum.TryParse(platform, true, out Platform plat))
                    id736.Points.Add(displayName, plat, pointsName, bonus);
                Log($"flawless bonus: {displayName}@{platform} +{bonus}");
            }
            catch (Exception ex)
            {
                Log($"flawless bonus error for {displayName}: {ex.Message}");
            }
        }

        id736.Chat.SendMessageToAllPlatforms($"Flawless victory bonus! {bonus} {pointsName} awarded to all players who played {minRounds}+ rounds!");
    }

    private void AwardRoundPoints(int blockRow, int blockCol)
    {
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        int hitPoints = mode == "easy"
            ? CPH.GetGlobalVar<int>("battleship_hit_points_easy", false)
            : mode == "extreme"
                ? CPH.GetGlobalVar<int>("battleship_hit_points_extreme", false)
                : CPH.GetGlobalVar<int>("battleship_hit_points_normal", false);
        if (hitPoints <= 0) return;

        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();

        int round = CPH.GetGlobalVar<int>("battleship_round", false);
        string roundKey = $"round_{round}";

        // Points context already set by Core.LinkStreamerbot
        foreach (var kvp in stats)
        {
            string userKey = kvp.Key;
            bool playedThisRound = kvp.Value.TryGetValue(roundKey, out object pt) && Convert.ToBoolean(pt);
            if (!playedThisRound) continue;

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

        Log($"award: {hitPoints} {pointsName} to all round-{round} participants for hit at {blockRow},{blockCol}");
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

    private List<List<Tuple<int, int>>> PlaceShips(int[] shipSizes, int gridSize)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var occupied = new HashSet<string>();
            var ships = new List<List<Tuple<int, int>>>();
            bool success = true;

            for (int i = 0; i < shipSizes.Length; i++)
            {
                bool placed = false;
                for (int tryCount = 0; tryCount < 100 && !placed; tryCount++)
                {
                    bool horizontal = _random.Next(2) == 0;
                    int size = shipSizes[i];

                    // The ship occupies 'size' consecutive cells in one direction.
                    // The start position must allow all cells to stay within the grid.
                    int maxRow, maxCol;
                    if (horizontal)
                    {
                        // Ship extends right: row can be 0..gridSize-1, col can be 0..gridSize-size
                        maxRow = gridSize - 1;
                        maxCol = gridSize - size;
                    }
                    else
                    {
                        // Ship extends down: row can be 0..gridSize-size, col can be 0..gridSize-1
                        maxRow = gridSize - size;
                        maxCol = gridSize - 1;
                    }

                    int startRow = _random.Next(0, maxRow + 1);
                    int startCol = _random.Next(0, maxCol + 1);

                    var cells = new List<Tuple<int, int>>();
                    bool conflict = false;
                    for (int j = 0; j < shipSizes[i]; j++)
                    {
                        int r = horizontal ? startRow : startRow + j;
                        int c = horizontal ? startCol + j : startCol;

                        // Hard bounds check — never allow a cell outside the grid
                        if (r < 0 || r >= gridSize || c < 0 || c >= gridSize)
                        {
                            conflict = true;
                            Log($"PLACEMENT BUG: cell ({r},{c}) is out of bounds for {gridSize}x{gridSize} grid. Ship {i} size={shipSizes[i]} horizontal={horizontal} start=({startRow},{startCol})");
                            break;
                        }

                        string key = $"{r},{c}";
                        if (occupied.Contains(key))
                        {
                            conflict = true;
                            break;
                        }
                        cells.Add(Tuple.Create(r, c));
                    }

                    if (!conflict)
                    {
                        foreach (var cell in cells)
                            occupied.Add($"{cell.Item1},{cell.Item2}");
                        ships.Add(cells);
                        placed = true;
                    }
                }
                if (!placed)
                {
                    success = false;
                    break;
                }
            }

            if (success)
            {
                // Final validation: verify every cell of every ship is within bounds
                bool allValid = true;
                foreach (var ship in ships)
                {
                    foreach (var cell in ship)
                    {
                        if (cell.Item1 < 0 || cell.Item1 >= gridSize || cell.Item2 < 0 || cell.Item2 >= gridSize)
                        {
                            Log($"PLACEMENT VALIDATION FAILED: cell ({cell.Item1},{cell.Item2}) out of bounds for {gridSize}x{gridSize}. Discarding attempt {attempt}.");
                            allValid = false;
                            break;
                        }
                    }
                    if (!allValid) break;
                }
                if (allValid)
                    return ships;
                success = false;
            }
        }
        return null;
    }

    private List<Tuple<int, int>> PlaceMines(List<List<Tuple<int, int>>> ships, int mineCount, int gridSize, bool halfAdjacent)
    {
        var occupied = new HashSet<string>();
        foreach (var ship in ships)
            foreach (var cell in ship)
                occupied.Add($"{cell.Item1},{cell.Item2}");

        // Get adjacent cells to ships
        var adjacentCells = new List<Tuple<int, int>>();
        foreach (var ship in ships)
        {
            foreach (var cell in ship)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int r = cell.Item1 + dr;
                        int c = cell.Item2 + dc;
                        if (r >= 0 && r < gridSize && c >= 0 && c < gridSize)
                        {
                            string key = $"{r},{c}";
                            if (!occupied.Contains(key) && !adjacentCells.Any(a => a.Item1 == r && a.Item2 == c))
                                adjacentCells.Add(Tuple.Create(r, c));
                        }
                    }
                }
            }
        }

        var mines = new List<Tuple<int, int>>();
        int adjacentCount = halfAdjacent ? mineCount / 2 : 0;

        // Place adjacent mines first
        for (int i = 0; i < adjacentCount && adjacentCells.Count > 0; i++)
        {
            int idx = _random.Next(adjacentCells.Count);
            var cell = adjacentCells[idx];
            adjacentCells.RemoveAt(idx);
            string key = $"{cell.Item1},{cell.Item2}";
            if (occupied.Contains(key)) continue;
            mines.Add(cell);
            occupied.Add(key);
        }

        // Place remaining mines randomly
        int remaining = mineCount - mines.Count;
        for (int i = 0; i < remaining; i++)
        {
            int attempts = 0;
            while (attempts < 200)
            {
                int r = _random.Next(gridSize);
                int c = _random.Next(gridSize);
                string key = $"{r},{c}";
                if (!occupied.Contains(key))
                {
                    mines.Add(Tuple.Create(r, c));
                    occupied.Add(key);
                    break;
                }
                attempts++;
            }
            if (attempts >= 200)
            {
                Log($"mine placement: could not place mine {i + 1} after 200 attempts");
                break;
            }
        }

        return mines;
    }

    private string GetArg(string name, string defaultVal)
    {
        if (CPH.TryGetArg(name, out string val) && !string.IsNullOrWhiteSpace(val))
            return val;
        return defaultVal;
    }

    private int GetArgInt(string name, int defaultVal)
    {
        if (CPH.TryGetArg(name, out int val))
            return val;
        if (CPH.TryGetArg(name, out string valStr) && int.TryParse(valStr, out int parsed))
            return parsed;
        return defaultVal;
    }

    private bool GetArgBool(string name, bool defaultVal)
    {
        if (CPH.TryGetArg(name, out bool val))
            return val;
        if (CPH.TryGetArg(name, out string valStr))
        {
            if (string.IsNullOrWhiteSpace(valStr)) return defaultVal;
            string s = valStr.Trim().ToLowerInvariant();
            if (s == "true" || s == "1" || s == "yes" || s == "on") return true;
            if (s == "false" || s == "0" || s == "no" || s == "off") return false;
        }
        return defaultVal;
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
    }

    private void EnableCommand(string commandName)
    {
        var commands = CPH.GetCommands();
        if (commands == null)
        {
            Log($"EnableCommand: GetCommands() returned null");
            return;
        }
        Log($"EnableCommand: looking for '{commandName}', GetCommands returned {commands.Count} commands");
        foreach (var cmd in commands)
        {
            if (cmd == null) continue;
            var type = cmd.GetType();
            string name = type.GetProperty("Name")?.GetValue(cmd, null)?.ToString() ?? "";
            string id = type.GetProperty("Id")?.GetValue(cmd, null)?.ToString() ?? "";
            Log($"EnableCommand: found command '{name}' (id={id})");
            if (name == commandName)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    CPH.EnableCommand(id);
                    Log($"enabled command: {commandName} ({id})");
                    return;
                }
            }
        }
        Log($"warning: could not find command '{commandName}' to enable");
    }

    private void DisableCommand(string commandName)
    {
        var commands = CPH.GetCommands();
        if (commands == null) return;
        foreach (var cmd in commands)
        {
            if (cmd == null) continue;
            var type = cmd.GetType();
            string name = type.GetProperty("Name")?.GetValue(cmd, null)?.ToString() ?? "";
            if (name == commandName)
            {
                string id = type.GetProperty("Id")?.GetValue(cmd, null)?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(id))
                {
                    CPH.DisableCommand(id);
                    Log($"disabled command: {commandName} ({id})");
                    return;
                }
            }
        }
        Log($"warning: could not find command '{commandName}' to disable");
    }

    private void CleanupGame()
    {
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        string coordCommandName = mode == "extreme" ? "battleship-extreme" : "battleship-normal";
        DisableCommand(coordCommandName);
        CPH.DisableAction("battleship-player-join");
        CPH.DisableAction("battleship-coord");

        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
            id736.Timers.Disable(timerGuid);

        string obsScene = CPH.GetGlobalVar<string>("battleship_obs_scene", false) ?? "";
        string obsSource = CPH.GetGlobalVar<string>("battleship_obs_source", false) ?? "";
        if (!string.IsNullOrWhiteSpace(obsScene) && !string.IsNullOrWhiteSpace(obsSource))
            CPH.ObsHideSource(obsScene, obsSource);

        id736.Groups.Clear(CPH.GetGlobalVar<string>("battleship_group", false) ?? "battleship-players");
        ClearGlobalVars();
    }

    private void ClearGlobalVars()
    {
        string[] vars = {
            "battleship_game_active", "battleship_mode", "battleship_phase",
            "battleship_group", "battleship_round", "battleship_collecting",
            "battleship_coords", "battleship_shots", "battleship_muted_players",
            "battleship_player_stats", "battleship_player_names",
            "battleship_mines_hit", "battleship_ships_sunk",
            "battleship_grid_size", "battleship_round_seconds",
            "battleship_inter_round_seconds", "battleship_points_name",
            "battleship_hit_points_easy", "battleship_hit_points_normal",
            "battleship_hit_points_extreme", "battleship_mine_penalty",
            "battleship_flawless_bonus_normal", "battleship_flawless_bonus_extreme",
            "battleship_min_rounds_for_bonus", "battleship_normal_mines",
            "battleship_extreme_multiplier", "battleship_ships", "battleship_mines",
            "battleship_obs_scene", "battleship_obs_source", "battleship_timer_guid",
            "battleship_round_ends_at", "battleship_pending_game_end",
            "battleship_pending_chat_hit", "battleship_pending_chat_sunk", "battleship_pending_chat_mine",
            "battleship_platform_teams", "battleship_platform_hits"
        };
        foreach (string v in vars)
            CPH.UnsetGlobalVar(v, false);
    }
}
