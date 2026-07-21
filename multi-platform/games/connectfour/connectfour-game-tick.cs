using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private const long JoinWindowMs = 60000;
    private const long VotingWindowMs = 30000;
    private const long TiebreakWindowMs = 15000;
    private const long AiThinkBudgetMs = 8000;
    private const long MoveAnimationMs = 2500;
    private const long GameEndDisplayMs = 8000;

    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "connectfour");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.GetGlobalVar<bool>("connectfour_game_active", false))
            return true;

        // Capture the timer GUID on the first tick.
        string timerGuid = CPH.GetGlobalVar<string>("connectfour_timer_guid", false) ?? "";
        if (string.IsNullOrWhiteSpace(timerGuid) && CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId))
        {
            timerGuid = currentTimerId.ToString();
            CPH.SetGlobalVar("connectfour_timer_guid", timerGuid, false);
        }

        string phase = CPH.GetGlobalVar<string>("connectfour_phase", false) ?? "";
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        switch (phase)
        {
            case "join":
                HandleJoinPhase(now);
                break;
            case "voting":
                HandleVotingPhase(now);
                break;
            case "tiebreak":
                HandleTiebreakPhase(now);
                break;
            case "ai":
                HandleAiPhase(now);
                break;
            case "animating":
                HandleAnimatingPhase(now);
                break;
            case "game_end":
                HandleGameEndPhase(now);
                break;
            case "idle":
            default:
                break;
        }

        return true;
    }

    // ===== Phase handlers =====

    private void HandleJoinPhase(long now)
    {
        // The join phase is driven synchronously by connectfour-game-setup.cs
        // (it blocks with CPH.Wait and sends chat reminders every 10 seconds).
        // The tick only fires after setup returns, so there's nothing to do here.
        // We still broadcast a phase update so a freshly-loaded browser shows the
        // join countdown if it connected mid-phase.
        long joinEndsAt = CPH.GetGlobalVar<long>("connectfour_join_ends_at", false);
        if (joinEndsAt > 0)
        {
            long remaining = Math.Max(0, joinEndsAt - now);
            SendEvent("phase", new Dictionary<string, object>
            {
                { "phase", "join" },
                { "remainingMs", remaining }
            });
        }
    }

    private void HandleVotingPhase(long now)
    {
        long startedAt = CPH.GetGlobalVar<long>("connectfour_phase_started_at", false);
        long remaining = Math.Max(0, VotingWindowMs - (now - startedAt));

        if (remaining > 0)
        {
            SendEvent("phase", new Dictionary<string, object>
            {
                { "phase", "voting" },
                { "remainingMs", remaining },
                { "voteTally", BuildVoteTally() }
            });
            return;
        }

        ResolveVotes(now, isTiebreak: false);
    }

    private void HandleTiebreakPhase(long now)
    {
        long startedAt = CPH.GetGlobalVar<long>("connectfour_phase_started_at", false);
        long remaining = Math.Max(0, TiebreakWindowMs - (now - startedAt));

        if (remaining > 0)
        {
            SendEvent("phase", new Dictionary<string, object>
            {
                { "phase", "tiebreak" },
                { "remainingMs", remaining },
                { "finalists", CurrentFinalists() },
                { "voteTally", BuildVoteTally() }
            });
            return;
        }

        ResolveVotes(now, isTiebreak: true);
    }

    private List<int> CurrentFinalists()
    {
        return id736.Data.FromJson<List<int>>(
            CPH.GetGlobalVar<string>("connectfour_tiebreak_finalists", false) ?? "[]")
            ?? new List<int>();
    }

    private Dictionary<string, object> BuildVoteTally()
    {
        var votes = id736.Data.FromJson<Dictionary<string, int>>(
            CPH.GetGlobalVar<string>("connectfour_voting_results", false) ?? "{}")
            ?? new Dictionary<string, int>();

        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        if (cols < 1) cols = 7;

        var counts = new int[cols];
        int total = 0;
        foreach (var kvp in votes)
        {
            if (kvp.Value >= 0 && kvp.Value < cols)
            {
                counts[kvp.Value]++;
                total++;
            }
        }

        var tally = new Dictionary<string, object>
        {
            { "total", total },
            { "counts", counts }
        };
        return tally;
    }

    private void HandleAiPhase(long now)
    {
        long startedAt = CPH.GetGlobalVar<long>("connectfour_phase_started_at", false);
        if (now - startedAt >= AiThinkBudgetMs)
        {
            Log("ai: budget expired, forcing fallback move");
            ExecuteAiFallback();
        }
    }

    private void HandleAnimatingPhase(long now)
    {
        long startedAt = CPH.GetGlobalVar<long>("connectfour_phase_started_at", false);
        if (now - startedAt >= MoveAnimationMs)
        {
            AfterMoveComplete();
        }
    }

    private void HandleGameEndPhase(long now)
    {
        long startedAt = CPH.GetGlobalVar<long>("connectfour_phase_started_at", false);
        if (now - startedAt >= GameEndDisplayMs)
        {
            HideGameSource();
            ResetState();
        }
    }

    // ===== Vote resolution =====

    private void ResolveVotes(long now, bool isTiebreak)
    {
        var votes = id736.Data.FromJson<Dictionary<string, int>>(
            CPH.GetGlobalVar<string>("connectfour_voting_results", false) ?? "{}")
            ?? new Dictionary<string, int>();

        if (votes.Count == 0)
        {
            if (isTiebreak)
            {
                // Still tied after tiebreak — pick randomly among finalists.
                var finalists = id736.Data.FromJson<List<int>>(
                    CPH.GetGlobalVar<string>("connectfour_tiebreak_finalists", false) ?? "[]")
                    ?? new List<int>();
                if (finalists.Count == 0)
                {
                    id736.Chat.SendMessageToAllPlatforms("No votes received. Picking a random column.");
                    int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
                    int pick = new Random().Next(cols);
                    ExecuteHumanMove(pick, now);
                    return;
                }

                int pickFinal = finalists[new Random().Next(finalists.Count)];
                id736.Chat.SendMessageToAllPlatforms($"Tiebreak ended with no votes. Picking column {pickFinal + 1}.");
                ExecuteHumanMove(pickFinal, now);
                return;
            }

            id736.Chat.SendMessageToAllPlatforms("No votes received this turn. Picking a random column.");
            int cols2 = CPH.GetGlobalVar<int>("connectfour_cols", false);
            int randomCol = new Random().Next(cols2);
            ExecuteHumanMove(randomCol, now);
            return;
        }

        var counts = new Dictionary<int, int>();
        foreach (var kvp in votes)
        {
            int c = kvp.Value;
            counts[c] = counts.TryGetValue(c, out int v) ? v + 1 : 1;
        }

        int bestCount = counts.Values.Max();
        var leaders = counts.Where(kvp => kvp.Value == bestCount).Select(kvp => kvp.Key).ToList();

        if (leaders.Count > 1 && !isTiebreak)
        {
            id736.Chat.SendMessageToAllPlatforms($"Tie between columns {string.Join(", ", leaders.Select(c => c + 1))}. 15-second tiebreak vote now!");
            CPH.SetGlobalVar("connectfour_tiebreak_finalists", id736.Data.ToJson(leaders), false);
            CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
            CPH.SetGlobalVar("connectfour_phase", "tiebreak", false);
            CPH.SetGlobalVar("connectfour_phase_started_at", now, false);

            SendEvent("phase", new Dictionary<string, object>
            {
                { "phase", "tiebreak" },
                { "remainingMs", TiebreakWindowMs },
                { "finalists", leaders }
            });
            return;
        }

        int chosen = leaders[0];
        string colWord = isTiebreak ? "Tiebreak resolved" : "Chat voted";
        id736.Chat.SendMessageToAllPlatforms($"{colWord} for column {chosen + 1} ({bestCount} vote{(bestCount == 1 ? "" : "s")}).");

        ExecuteHumanMove(chosen, now);
    }

    // ===== Move execution =====

    private void ExecuteHumanMove(int col, long now)
    {
        int rows = CPH.GetGlobalVar<int>("connectfour_rows", false);
        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        var grid = LoadGrid(rows, cols);

        if (!CanDrop(grid, col, out int dropRow))
        {
            id736.Chat.SendMessageToAllPlatforms($"Column {col + 1} is full. Picking the next available column.");
            col = FindFirstAvailableColumn(grid, cols);
            if (col < 0)
            {
                id736.Chat.SendMessageToAllPlatforms("The board is full. Ending the game as a draw.");
                EndGame("draw", null, now);
                return;
            }
            CanDrop(grid, col, out dropRow);
        }

        int player = 1;
        grid[dropRow][col] = player;
        SaveGrid(grid);

        CPH.SetGlobalVar("connectfour_last_move", $"{dropRow},{col}", false);
        CPH.SetGlobalVar("connectfour_phase", "animating", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);

        SendEvent("move", new Dictionary<string, object>
        {
            { "player", player },
            { "row", dropRow },
            { "col", col }
        });

        Log($"move: human dropped in col {col + 1}, row {dropRow}");
    }

    private void ExecuteAiFallback()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int rows = CPH.GetGlobalVar<int>("connectfour_rows", false);
        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        var grid = LoadGrid(rows, cols);

        int col = FindFirstAvailableColumn(grid, cols);
        if (col < 0)
        {
            EndGame("draw", null, now);
            return;
        }

        ExecuteAiMove(col, now);
    }

    public void ExecuteAiMove(int col, long now)
    {
        int rows = CPH.GetGlobalVar<int>("connectfour_rows", false);
        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        var grid = LoadGrid(rows, cols);

        if (!CanDrop(grid, col, out int dropRow))
        {
            col = FindFirstAvailableColumn(grid, cols);
            if (col < 0)
            {
                EndGame("draw", null, now);
                return;
            }
            CanDrop(grid, col, out dropRow);
        }

        int player = 2;
        grid[dropRow][col] = player;
        SaveGrid(grid);

        CPH.SetGlobalVar("connectfour_last_move", $"{dropRow},{col}", false);
        CPH.SetGlobalVar("connectfour_phase", "animating", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);
        CPH.SetGlobalVar("connectfour_ai_pending", false, false);

        SendEvent("move", new Dictionary<string, object>
        {
            { "player", player },
            { "row", dropRow },
            { "col", col }
        });

        Log($"move: AI dropped in col {col + 1}, row {dropRow}");
    }

    private void AfterMoveComplete()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int rows = CPH.GetGlobalVar<int>("connectfour_rows", false);
        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        var grid = LoadGrid(rows, cols);

        int currentPlayer = CPH.GetGlobalVar<int>("connectfour_current_player", false);
        int lastDropRow = -1, lastDropCol = -1;
        string lastMove = CPH.GetGlobalVar<string>("connectfour_last_move", false) ?? "";
        if (!string.IsNullOrWhiteSpace(lastMove))
        {
            var parts = lastMove.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out lastDropRow) && int.TryParse(parts[1], out lastDropCol))
            {
                // got it
            }
        }

        if (CheckWin(grid, lastDropRow, lastDropCol, currentPlayer, out var winningCells))
        {
            EndGame(currentPlayer == 1 ? "human" : "ai", winningCells, now);
            return;
        }

        if (IsBoardFull(grid))
        {
            EndGame("draw", null, now);
            return;
        }

        int turn = CPH.GetGlobalVar<int>("connectfour_turn", false) + 1;
        CPH.SetGlobalVar("connectfour_turn", turn, false);

        int nextPlayer = currentPlayer == 1 ? 2 : 1;
        CPH.SetGlobalVar("connectfour_current_player", nextPlayer, false);
        CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
        CPH.SetGlobalVar("connectfour_tiebreak_finalists", "[]", false);

        if (nextPlayer == 1)
        {
            StartVotingPhase(now);
        }
        else
        {
            StartAiPhase(now);
        }
    }

    private void StartVotingPhase(long now)
    {
        CPH.SetGlobalVar("connectfour_phase", "voting", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);
        CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
        CPH.SetGlobalVar("connectfour_tiebreak_finalists", "[]", false);

        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        string range = cols == 11 ? "1-11" : "1-7";

        id736.Chat.SendMessageToAllPlatforms($"Chat's turn! Type a column number ({range}) to vote. 30 seconds to vote.");

        SendEvent("phase", new Dictionary<string, object>
        {
            { "phase", "voting" },
            { "remainingMs", VotingWindowMs },
            { "currentPlayer", 1 }
        });
    }

    private void StartAiPhase(long now)
    {
        CPH.SetGlobalVar("connectfour_phase", "ai", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);
        CPH.SetGlobalVar("connectfour_ai_pending", true, false);

        string difficulty = CPH.GetGlobalVar<string>("connectfour_difficulty", false) ?? "normal";
        id736.Chat.SendMessageToAllPlatforms($"AI ({difficulty}) is thinking...");

        SendEvent("phase", new Dictionary<string, object>
        {
            { "phase", "ai" },
            { "currentPlayer", 2 }
        });

        CPH.RunAction("connectfour-ai-move", false);
    }

    // ===== End-game =====

    private void EndGame(string result, List<Dictionary<string, int>> winningCells, long now)
    {
        CPH.SetGlobalVar("connectfour_phase", "game_end", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);
        CPH.SetGlobalVar("connectfour_winner", result, false);

        AwardPoints(result);

        string msg;
        if (result == "draw")
            msg = "The board is full. It's a draw!";
        else if (result == "human")
            msg = "Chat wins! Great teamwork.";
        else
            msg = "The AI wins this round.";

        id736.Chat.SendMessageToAllPlatforms(msg);

        var payload = new Dictionary<string, object>
        {
            { "phase", "game_end" },
            { "result", result },
            { "winningCells", winningCells ?? new List<Dictionary<string, int>>() }
        };
        SendEvent("game-over", payload);

        Log($"game-end: result={result}");
    }

    private void AwardPoints(string result)
    {
        string pointsName = CPH.GetGlobalVar<string>("connectfour_points_name", false) ?? "points";
        if (string.IsNullOrWhiteSpace(pointsName)) pointsName = "points";

        string difficulty = CPH.GetGlobalVar<string>("connectfour_difficulty", false) ?? "normal";

        int winPoints, lossPoints, drawPoints;
        if (difficulty == "easy")
        {
            winPoints = CPH.GetGlobalVar<int>("connectfour_win_points_easy", false);
            lossPoints = CPH.GetGlobalVar<int>("connectfour_loss_points_easy", false);
            drawPoints = CPH.GetGlobalVar<int>("connectfour_draw_points_easy", false);
        }
        else if (difficulty == "extreme")
        {
            winPoints = CPH.GetGlobalVar<int>("connectfour_win_points_extreme", false);
            lossPoints = CPH.GetGlobalVar<int>("connectfour_loss_points_extreme", false);
            drawPoints = CPH.GetGlobalVar<int>("connectfour_draw_points_extreme", false);
        }
        else
        {
            winPoints = CPH.GetGlobalVar<int>("connectfour_win_points_normal", false);
            lossPoints = CPH.GetGlobalVar<int>("connectfour_loss_points_normal", false);
            drawPoints = CPH.GetGlobalVar<int>("connectfour_draw_points_normal", false);
        }

        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("connectfour_player_names", false) ?? "{}")
            ?? new Dictionary<string, string>();

        string playerOrder = CPH.GetGlobalVar<string>("connectfour_player_order", false) ?? "";
        var players = playerOrder.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        int awarded = 0;
        int totalAwarded = 0;
        foreach (string userKey in players)
        {
            string displayName = playerNames.TryGetValue(userKey, out string nm) && !string.IsNullOrWhiteSpace(nm) ? nm : userKey;
            var keyParts = userKey.Split(':');
            string platform = keyParts.Length > 0 ? keyParts[0] : "twitch";

            int amount;
            if (result == "human") amount = winPoints;
            else if (result == "ai") amount = lossPoints;
            else amount = drawPoints;

            try
            {
                if (Enum.TryParse(platform, true, out Platform plat))
                {
                    id736.Points.Add(displayName, plat, pointsName, amount);
                    awarded++;
                    totalAwarded += amount;
                }
            }
            catch (Exception ex)
            {
                Log($"award error for {displayName}@{platform}: {ex.Message}");
            }
        }

        string resultWord = result == "human" ? "win" : result == "ai" ? "loss" : "draw";
        int perPlayer = result == "human" ? winPoints : result == "ai" ? lossPoints : drawPoints;
        id736.Chat.SendMessageToAllPlatforms($"Awarded {perPlayer} {pointsName} each to {awarded} player{(awarded == 1 ? "" : "s")} ({totalAwarded} {pointsName} total) for the {resultWord}.");

        Log($"awarded {awarded} players: {winPoints}/{lossPoints}/{drawPoints} {pointsName} (win/loss/draw), total={totalAwarded}");
    }

    // ===== Grid helpers =====

    private int[][] LoadGrid(int rows, int cols)
    {
        string json = CPH.GetGlobalVar<string>("connectfour_grid", false) ?? "[]";
        try
        {
            var arr = id736.Data.FromJson<int[][]>(json);
            if (arr != null && arr.Length == rows)
                return arr;
        }
        catch { }

        var fresh = new int[rows][];
        for (int r = 0; r < rows; r++) fresh[r] = new int[cols];
        return fresh;
    }

    private void SaveGrid(int[][] grid)
    {
        CPH.SetGlobalVar("connectfour_grid", id736.Data.ToJson(grid), false);
    }

    private bool CanDrop(int[][] grid, int col, out int dropRow)
    {
        dropRow = -1;
        if (grid == null || grid.Length == 0) return false;
        if (col < 0 || col >= grid[0].Length) return false;
        for (int r = grid.Length - 1; r >= 0; r--)
        {
            if (grid[r][col] == 0)
            {
                dropRow = r;
                return true;
            }
        }
        return false;
    }

    private int FindFirstAvailableColumn(int[][] grid, int cols)
    {
        for (int c = 0; c < cols; c++)
        {
            if (CanDrop(grid, c, out _)) return c;
        }
        return -1;
    }

    private bool IsBoardFull(int[][] grid)
    {
        for (int r = 0; r < grid.Length; r++)
            for (int c = 0; c < grid[r].Length; c++)
                if (grid[r][c] == 0) return false;
        return true;
    }

    private bool CheckWin(int[][] grid, int row, int col, int player, out List<Dictionary<string, int>> winningCells)
    {
        winningCells = new List<Dictionary<string, int>>();
        if (row < 0 || col < 0) return false;

        int rows = grid.Length;
        int cols = grid[0].Length;
        int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };

        foreach (var d in dirs)
        {
            var cells = new List<Dictionary<string, int>>
            {
                new Dictionary<string, int> { { "row", row }, { "col", col } }
            };

            for (int i = 1; i < 4; i++)
            {
                int r = row + d[0] * i, c = col + d[1] * i;
                if (r < 0 || r >= rows || c < 0 || c >= cols || grid[r][c] != player) break;
                cells.Add(new Dictionary<string, int> { { "row", r }, { "col", c } });
            }
            for (int i = 1; i < 4; i++)
            {
                int r = row - d[0] * i, c = col - d[1] * i;
                if (r < 0 || r >= rows || c < 0 || c >= cols || grid[r][c] != player) break;
                cells.Insert(0, new Dictionary<string, int> { { "row", r }, { "col", c } });
            }

            if (cells.Count >= 4)
            {
                winningCells = cells.Take(4).ToList();
                return true;
            }
        }
        return false;
    }

    // ===== State reset =====

    private void HideGameSource()
    {
        string scene = CPH.GetGlobalVar<string>("connectfour_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("connectfour_obs_source", false) ?? "";
        if (!string.IsNullOrWhiteSpace(scene) && !string.IsNullOrWhiteSpace(source))
            CPH.ObsHideSource(scene, source);
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
        CPH.SetGlobalVar("connectfour_join_window_open", false, false);
        CPH.SetGlobalVar("connectfour_join_count", 0, false);
        CPH.SetGlobalVar("connectfour_player_order", "", false);
        CPH.SetGlobalVar("connectfour_player_names", "{}", false);
        CPH.SetGlobalVar("connectfour_voting_results", "{}", false);
        CPH.SetGlobalVar("connectfour_tiebreak_finalists", "[]", false);
        CPH.SetGlobalVar("connectfour_timer_guid", "", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", 0L, false);
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

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
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