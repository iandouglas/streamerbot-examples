using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using id736 = iandouglas736;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "connectfour");
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

    private int ColumnsFor(string difficulty)
    {
        return difficulty == "extreme" ? 11 : 7;
    }

    private int RowsFor(string difficulty)
    {
        return 6;
    }

    private string EmptyGrid(int rows, int cols)
    {
        var grid = new int[rows][];
        for (int r = 0; r < rows; r++)
        {
            grid[r] = new int[cols];
        }
        return id736.Data.ToJson(grid);
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("rawInput", out string rawInput))
            rawInput = "";
        if (!CPH.TryGetArg("command", out string command))
            command = "";

        string[] parts = rawInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // rawInput may be "connectfour easy", "connectfour end", "easy", or "end"
        // depending on how the command is configured. Strip a leading "connectfour"
        // token so the rest is consistently <mode|end> [...].
        int startIdx = 0;
        if (parts.Length > 0 && parts[0].Equals("connectfour", StringComparison.OrdinalIgnoreCase))
            startIdx = 1;

        string subCommand = parts.Length > startIdx ? parts[startIdx].Trim().ToLowerInvariant() : "";
        string modeArg = parts.Length > startIdx ? parts[startIdx].Trim().ToLowerInvariant() : "";

        if (subCommand == "end" || subCommand == "stop" || subCommand == "cancel")
        {
            EndGame();
            return true;
        }

        if (!string.IsNullOrWhiteSpace(subCommand))
        {
            StartGame(modeArg);
            return true;
        }

        id736.Chat.SendMessageToAllPlatforms("Usage: !game connectfour [easy|normal|extreme|end]");
        return true;
    }

    private void StartGame(string difficulty)
    {
        if (difficulty != "easy" && difficulty != "normal" && difficulty != "extreme")
        {
            id736.Chat.SendMessageToAllPlatforms("Invalid difficulty. Use: !game connectfour [easy|normal|extreme]");
            return;
        }

        bool gameActive = CPH.GetGlobalVar<bool>("connectfour_game_active", false);
        if (gameActive)
        {
            string currentDifficulty = CPH.GetGlobalVar<string>("connectfour_difficulty", false) ?? "normal";
            id736.Chat.SendMessageToAllPlatforms($"A Connect Four game is already active (difficulty: {currentDifficulty}). Type !game connectfour end to cancel it.");
            return;
        }

        int rows = RowsFor(difficulty);
        int cols = ColumnsFor(difficulty);
        string grid = EmptyGrid(rows, cols);

        CPH.SetGlobalVar("connectfour_game_active", true, false);
        CPH.SetGlobalVar("connectfour_difficulty", difficulty, false);
        CPH.SetGlobalVar("connectfour_rows", rows, false);
        CPH.SetGlobalVar("connectfour_cols", cols, false);
        CPH.SetGlobalVar("connectfour_grid", grid, false);
        CPH.SetGlobalVar("connectfour_turn", 0, false);
        CPH.SetGlobalVar("connectfour_current_player", 1, false);
        CPH.SetGlobalVar("connectfour_winner", "", false);
        CPH.SetGlobalVar("connectfour_phase", "join", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        CPH.SetGlobalVar("connectfour_join_window_open", true, false);
        CPH.SetGlobalVar("connectfour_join_count", 0, false);
        CPH.SetGlobalVar("connectfour_player_order", "", false);
        CPH.SetGlobalVar("connectfour_player_names", "{}", false);
        CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
        CPH.SetGlobalVar("connectfour_tiebreak_finalists", "[]", false);
        CPH.SetGlobalVar("connectfour_ai_pending", false, false);
        CPH.SetGlobalVar("connectfour_setup_sent", false, false);
        CPH.SetGlobalVar("connectfour_last_move", "", false);

        // Points configuration (overridable via sub-action arguments).
        string pointsName = GetArg("pointsName", "points");
        CPH.SetGlobalVar("connectfour_points_name", pointsName, false);
        CPH.SetGlobalVar("connectfour_win_points_easy", GetArgInt("winPointsEasy", 10), false);
        CPH.SetGlobalVar("connectfour_win_points_normal", GetArgInt("winPointsNormal", 25), false);
        CPH.SetGlobalVar("connectfour_win_points_extreme", GetArgInt("winPointsExtreme", 50), false);
        CPH.SetGlobalVar("connectfour_loss_points_easy", GetArgInt("lossPointsEasy", 2), false);
        CPH.SetGlobalVar("connectfour_loss_points_normal", GetArgInt("lossPointsNormal", 5), false);
        CPH.SetGlobalVar("connectfour_loss_points_extreme", GetArgInt("lossPointsExtreme", 10), false);
        CPH.SetGlobalVar("connectfour_draw_points_easy", GetArgInt("drawPointsEasy", 1), false);
        CPH.SetGlobalVar("connectfour_draw_points_normal", GetArgInt("drawPointsNormal", 2), false);
        CPH.SetGlobalVar("connectfour_draw_points_extreme", GetArgInt("drawPointsExtreme", 5), false);

        // AI/Ollama configuration (used by extreme mode; harmless for other difficulties).
        string ollamaUrl = GetArg("ollamaUrl", "");
        string ollamaModel = GetArg("ollamaModel", "llama3");
        CPH.SetGlobalVar("connectfour_ollama_url", ollamaUrl, false);
        CPH.SetGlobalVar("connectfour_ollama_model", ollamaModel, false);
        CPH.SetGlobalVar("connectfour_ollama_available", false, false);

        if (difficulty == "extreme" && !string.IsNullOrWhiteSpace(ollamaUrl))
        {
            bool reachable = CheckOllama(ollamaUrl, ollamaModel, out string checkMsg);
            CPH.SetGlobalVar("connectfour_ollama_available", reachable, false);
            if (reachable)
            {
                Log($"game-setup: ollama reachable at {ollamaUrl}, model '{ollamaModel}' available");
            }
            else
            {
                Log($"game-setup: ollama check failed - {checkMsg}");
                id736.Chat.SendMessageToAllPlatforms($"Warning: Ollama check failed ({checkMsg}). Extreme AI will fall back to minimax.");
            }
        }

        // OBS source to show/hide the game browser source.
        string obsScene = GetArg("obsScene", "");
        string obsSource = GetArg("obsSource", "");
        CPH.SetGlobalVar("connectfour_obs_scene", obsScene, false);
        CPH.SetGlobalVar("connectfour_obs_source", obsSource, false);

        // Join window duration (overridable via sub-action argument).
        int joinSeconds = GetArgInt("joinSeconds", 60);
        if (joinSeconds < 10) joinSeconds = 60;
        CPH.SetGlobalVar("connectfour_join_seconds", joinSeconds, false);

        // Name of the Streamer.bot command that triggers connectfour-vote (regex ^\d$ etc).
        string voteCommandName = GetArg("voteCommandName", "connectfour vote");
        CPH.SetGlobalVar("connectfour_vote_command_name", voteCommandName, false);

        // Store the timer GUID (passed as a sub-action argument) so the tick can
        // enable/disable the timer by ID and reset its interval.
        if (CPH.TryGetArg("timerGuid", out string setupTimerGuid) && !string.IsNullOrWhiteSpace(setupTimerGuid))
            CPH.SetGlobalVar("connectfour_timer_guid", setupTimerGuid, false);
        CPH.SetGlobalVar("connectfour_timer_name", "connectfour-game", false);

        // Clear any stale group from a previous game and start fresh.
        id736.Groups.EnsureGroup("connectfour_players");
        id736.Groups.Clear("connectfour_players");

        // Enable the game timer so the tick starts firing.
        EnableGameTimer();

        // The vote command is disabled by default; enable it now so players can
        // type a bare number to vote once the voting phase begins.
        EnableCommand(voteCommandName);

        // Reveal the OBS browser source so the display is visible.
        ShowGameSource();

        Log($"game-setup: started {difficulty} game ({cols}x{rows})");

        string colsRange = difficulty == "extreme" ? "1-11" : "1-7";
        long joinEndsAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + joinSeconds * 1000L;
        CPH.SetGlobalVar("connectfour_join_ends_at", joinEndsAt, false);

        id736.Chat.SendMessageToAllPlatforms($"Connect Four ({difficulty}) started! You have {joinSeconds} seconds to !join. You'll vote for a column ({colsRange}) each turn.");

        SendEvent("setup", new Dictionary<string, object>
        {
            { "difficulty", difficulty },
            { "rows", rows },
            { "cols", cols },
            { "phase", "join" },
            { "joinEndsAt", joinEndsAt },
            { "joinSeconds", joinSeconds }
        });

        // --- Join phase: synchronous countdown with chat reminders every 10 seconds ---
        int elapsed = 0;
        while (elapsed < joinSeconds)
        {
            int waitTime = Math.Min(10, joinSeconds - elapsed);
            CPH.Wait(waitTime * 1000);
            if (!CPH.GetGlobalVar<bool>("connectfour_game_active", false))
                return;
            elapsed += waitTime;
            int remaining = joinSeconds - elapsed;
            if (remaining > 0)
            {
                int count = id736.Groups.Count("connectfour_players");
                id736.Chat.SendMessageToAllPlatforms($"{remaining} seconds left to !join! Players so far: {count}");
            }
        }

        if (!CPH.GetGlobalVar<bool>("connectfour_game_active", false))
            return;

        // Close the join window and let the tick take over.
        CPH.SetGlobalVar("connectfour_join_window_open", false, false);
        CPH.SetGlobalVar("connectfour_join_ends_at", 0L, false);

        int joinedCount = id736.Groups.Count("connectfour_players");
        if (joinedCount == 0)
        {
            id736.Chat.SendMessageToAllPlatforms("Nobody joined the Connect Four game. Cancelling.");
            Log("game-setup: no players joined, cancelling game");
            ResetState();
            HideGameSource();
            SendEvent("game-end", new Dictionary<string, object> { { "reason", "no-players" } });
            return;
        }

        Log($"game-setup: {joinedCount} players joined, starting voting phase");
        id736.Chat.SendMessageToAllPlatforms($"Join window closed! {joinedCount} player(s) will compete against the AI. Chat will vote for the human's moves.");

        // Start the first voting phase.
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        CPH.SetGlobalVar("connectfour_phase", "voting", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);
        CPH.SetGlobalVar("connectfour_current_player", 1, false);
        CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
        CPH.SetGlobalVar("connectfour_tiebreak_finalists", "[]", false);

        SendEvent("phase", new Dictionary<string, object>
        {
            { "phase", "voting" },
            { "remainingMs", 30000 },
            { "currentPlayer", 1 }
        });

        id736.Chat.SendMessageToAllPlatforms($"Chat's turn! Type a column number ({colsRange}) to vote. 30 seconds to vote.");
    }

    private void EndGame()
    {
        bool gameActive = CPH.GetGlobalVar<bool>("connectfour_game_active", false);
        if (!gameActive)
        {
            id736.Chat.SendMessageToAllPlatforms("No Connect Four game is currently active.");
            return;
        }

        ResetState();
        HideGameSource();
        id736.Chat.SendMessageToAllPlatforms("Connect Four game has been ended.");

        SendEvent("game-end", new Dictionary<string, object> { { "reason", "cancelled" } });
    }

    private void ResetState()
    {
        string timerGuid = CPH.GetGlobalVar<string>("connectfour_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
            id736.Timers.Disable(timerGuid);

        // Disable the vote command so bare numbers aren't intercepted after the game.
        string voteCommandName = CPH.GetGlobalVar<string>("connectfour_vote_command_name", false) ?? "connectfour vote";
        DisableCommand(voteCommandName);

        // Clear the player group so stale members don't block !join next game.
        id736.Groups.Clear("connectfour_players");

        CPH.SetGlobalVar("connectfour_game_active", false, false);
        CPH.SetGlobalVar("connectfour_difficulty", "", false);
        CPH.SetGlobalVar("connectfour_rows", 0, false);
        CPH.SetGlobalVar("connectfour_cols", 0, false);
        CPH.SetGlobalVar("connectfour_grid", "[]", false);
        CPH.SetGlobalVar("connectfour_turn", 0, false);
        CPH.SetGlobalVar("connectfour_current_player", 0, false);
        CPH.SetGlobalVar("connectfour_winner", "", false);
        CPH.SetGlobalVar("connectfour_phase", "", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", 0L, false);
        CPH.SetGlobalVar("connectfour_join_window_open", false, false);
        CPH.SetGlobalVar("connectfour_join_count", 0, false);
        CPH.SetGlobalVar("connectfour_player_order", "", false);
        CPH.SetGlobalVar("connectfour_player_names", "{}", false);
        CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
        CPH.SetGlobalVar("connectfour_tiebreak_finalists", "[]", false);
        CPH.SetGlobalVar("connectfour_ai_pending", false, false);
        CPH.SetGlobalVar("connectfour_setup_sent", false, false);
        CPH.SetGlobalVar("connectfour_last_move", "", false);
        CPH.SetGlobalVar("connectfour_points_name", "", false);
        CPH.SetGlobalVar("connectfour_win_points_easy", 0, false);
        CPH.SetGlobalVar("connectfour_win_points_normal", 0, false);
        CPH.SetGlobalVar("connectfour_win_points_extreme", 0, false);
        CPH.SetGlobalVar("connectfour_loss_points_easy", 0, false);
        CPH.SetGlobalVar("connectfour_loss_points_normal", 0, false);
        CPH.SetGlobalVar("connectfour_loss_points_extreme", 0, false);
        CPH.SetGlobalVar("connectfour_draw_points_easy", 0, false);
        CPH.SetGlobalVar("connectfour_draw_points_normal", 0, false);
        CPH.SetGlobalVar("connectfour_draw_points_extreme", 0, false);
        CPH.SetGlobalVar("connectfour_ollama_url", "", false);
        CPH.SetGlobalVar("connectfour_ollama_model", "", false);
        CPH.SetGlobalVar("connectfour_ollama_available", false, false);
        CPH.SetGlobalVar("connectfour_obs_scene", "", false);
        CPH.SetGlobalVar("connectfour_obs_source", "", false);
        CPH.SetGlobalVar("connectfour_join_seconds", 0, false);
        CPH.SetGlobalVar("connectfour_join_ends_at", 0L, false);
        CPH.SetGlobalVar("connectfour_vote_command_name", "", false);
    }

    private void EnableGameTimer()
    {
        string timerGuid = CPH.GetGlobalVar<string>("connectfour_timer_guid", false) ?? "";
        string timerName = CPH.GetGlobalVar<string>("connectfour_timer_name", false) ?? "connectfour-game";

        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, 1, keepEnabled: true);
        }
        else
        {
            // No GUID captured yet; enable by name so the tick can capture it.
            CPH.EnableTimer(timerName);
        }
    }

    private void ShowGameSource()
    {
        string scene = CPH.GetGlobalVar<string>("connectfour_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("connectfour_obs_source", false) ?? "";
        if (!string.IsNullOrWhiteSpace(scene) && !string.IsNullOrWhiteSpace(source))
            CPH.ObsShowSource(scene, source);
    }

    private void HideGameSource()
    {
        string scene = CPH.GetGlobalVar<string>("connectfour_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("connectfour_obs_source", false) ?? "";
        if (!string.IsNullOrWhiteSpace(scene) && !string.IsNullOrWhiteSpace(source))
            CPH.ObsHideSource(scene, source);
    }

    // ===== Ollama pre-flight check (extreme mode) =====

    private bool CheckOllama(string baseUrl, string model, out string message)
    {
        message = "";
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            message = "no ollamaUrl configured";
            return false;
        }

        string tagsUrl = baseUrl.TrimEnd('/') + "/api/tags";
        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                string response = client.DownloadString(tagsUrl);
                var parsed = JObject.Parse(response);
                var modelsArr = parsed["models"] as JArray;
                if (modelsArr == null || modelsArr.Count == 0)
                {
                    message = $"no models installed at {baseUrl}";
                    return false;
                }

                var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in modelsArr)
                {
                    string name = m["name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        installed.Add(name);
                }

                if (string.IsNullOrWhiteSpace(model))
                {
                    message = "no ollamaModel configured";
                    return false;
                }

                // Ollama model names are often matched by prefix (e.g. "llama3" matches "llama3:latest").
                bool exact = installed.Contains(model);
                bool prefixMatch = installed.Any(n => n.StartsWith(model + ":", StringComparison.OrdinalIgnoreCase));
                if (!exact && !prefixMatch)
                {
                    string sample = string.Join(", ", installed.OrderBy(n => n).Take(5));
                    message = $"model '{model}' not found at {baseUrl}. Available: {sample}{(installed.Count > 5 ? "..." : "")}";
                    return false;
                }

                return true;
            }
        }
        catch (WebException wex)
        {
            message = $"cannot reach {baseUrl} ({wex.Status})";
            return false;
        }
        catch (Exception ex)
        {
            message = $"ollama check error: {ex.Message}";
            return false;
        }
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
        foreach (var cmd in commands)
        {
            if (cmd == null) continue;
            var type = cmd.GetType();
            string name = type.GetProperty("Name")?.GetValue(cmd, null)?.ToString() ?? "";
            string id = type.GetProperty("Id")?.GetValue(cmd, null)?.ToString() ?? "";
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
}