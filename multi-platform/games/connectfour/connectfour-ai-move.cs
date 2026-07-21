using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using id736 = iandouglas736;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private static readonly Random _random = new Random();

    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "connectfour");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.GetGlobalVar<bool>("connectfour_game_active", false))
            return true;

        if (!CPH.GetGlobalVar<bool>("connectfour_ai_pending", false))
            return true;

        string difficulty = CPH.GetGlobalVar<string>("connectfour_difficulty", false) ?? "normal";
        int rows = CPH.GetGlobalVar<int>("connectfour_rows", false);
        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);

        var grid = LoadGrid(rows, cols);

        int col = ChooseColumn(grid, rows, cols, difficulty);
        Log($"ai: difficulty={difficulty} chose col {col + 1}");

        if (!CanDrop(grid, col, out int dropRow))
        {
            col = FindFirstAvailableColumn(grid, cols);
            if (col < 0)
            {
                CPH.SetGlobalVar("connectfour_ai_pending", false, false);
                return true;
            }
            CanDrop(grid, col, out dropRow);
        }

        grid[dropRow][col] = 2;
        SaveGrid(grid);

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        CPH.SetGlobalVar("connectfour_last_move", $"{dropRow},{col}", false);
        CPH.SetGlobalVar("connectfour_phase", "animating", false);
        CPH.SetGlobalVar("connectfour_phase_started_at", now, false);
        CPH.SetGlobalVar("connectfour_ai_pending", false, false);

        SendEvent("move", new Dictionary<string, object>
        {
            { "player", 2 },
            { "row", dropRow },
            { "col", col }
        });

        return true;
    }

    // ===== Difficulty strategies =====

    private int ChooseColumn(int[][] grid, int rows, int cols, string difficulty)
    {
        switch (difficulty)
        {
            case "easy":
                return ChooseEasy(grid, cols);
            case "extreme":
                return ChooseExtreme(grid, rows, cols);
            case "normal":
            default:
                return ChooseNormal(grid, rows, cols);
        }
    }

    private int ChooseEasy(int[][] grid, int cols)
    {
        // 20% chance to block an immediate human win, otherwise random.
        if (_random.NextDouble() < 0.2)
        {
            int block = FindWinningMove(grid, 1);
            if (block >= 0) return block;
        }

        var valid = ValidColumns(grid, cols);
        return valid[_random.Next(valid.Count)];
    }

    private int ChooseNormal(int[][] grid, int rows, int cols)
    {
        // 1. Win immediately if possible.
        int winMove = FindWinningMove(grid, 2);
        if (winMove >= 0) return winMove;

        // 2. Block opponent's immediate win 70% of the time (30% mistake rate).
        if (_random.NextDouble() < 0.7)
        {
            int block = FindWinningMove(grid, 1);
            if (block >= 0) return block;
        }

        // 3. One-step lookahead: pick the move that maximizes AI's resulting
        //    threat count while minimizing opponent's immediate wins.
        int bestCol = -1;
        int bestScore = int.MinValue;
        var valid = ValidColumns(grid, cols);

        foreach (int c in valid)
        {
            if (!CanDrop(grid, c, out int r)) continue;
            grid[r][c] = 2;
            int score = EvaluateBoard(grid, rows, cols, 2);
            // Penalty if this move lets opponent win directly above
            if (r > 0 && grid[r - 1][c] == 0)
            {
                grid[r - 1][c] = 1;
                if (FindWinningMoveAt(grid, 1, c) >= 0)
                    score -= 1000;
                grid[r - 1][c] = 0;
            }
            grid[r][c] = 0;

            // Small random jitter to avoid deterministic play
            score += _random.Next(-2, 3);

            if (score > bestScore)
            {
                bestScore = score;
                bestCol = c;
            }
        }

        return bestCol >= 0 ? bestCol : valid[_random.Next(valid.Count)];
    }

    private int ChooseExtreme(int[][] grid, int rows, int cols)
    {
        // Try Ollama first; fall back to a stronger heuristic if it fails.
        int ollamaCol = TryOllamaMove(grid, rows, cols);
        if (ollamaCol >= 0) return ollamaCol;

        // Minimax with depth 4 + immediate win/block.
        int winMove = FindWinningMove(grid, 2);
        if (winMove >= 0) return winMove;

        int block = FindWinningMove(grid, 1);
        if (block >= 0) return block;

        return MinimaxBest(grid, rows, cols, depth: 4);
    }

    // ===== Win detection helpers =====

    private int FindWinningMove(int[][] grid, int player)
    {
        int cols = grid[0].Length;
        for (int c = 0; c < cols; c++)
        {
            if (CanDrop(grid, c, out int r))
            {
                grid[r][c] = player;
                bool wins = WouldWin(grid, r, c, player);
                grid[r][c] = 0;
                if (wins) return c;
            }
        }
        return -1;
    }

    private int FindWinningMoveAt(int[][] grid, int player, int col)
    {
        if (!CanDrop(grid, col, out int r)) return -1;
        grid[r][col] = player;
        bool wins = WouldWin(grid, r, col, player);
        grid[r][col] = 0;
        return wins ? r : -1;
    }

    private bool WouldWin(int[][] grid, int row, int col, int player)
    {
        int rows = grid.Length, cols = grid[0].Length;
        int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };

        foreach (var d in dirs)
        {
            int count = 1;
            for (int i = 1; i < 4; i++)
            {
                int r = row + d[0] * i, c = col + d[1] * i;
                if (r < 0 || r >= rows || c < 0 || c >= cols || grid[r][c] != player) break;
                count++;
            }
            for (int i = 1; i < 4; i++)
            {
                int r = row - d[0] * i, c = col - d[1] * i;
                if (r < 0 || r >= rows || c < 0 || c >= cols || grid[r][c] != player) break;
                count++;
            }
            if (count >= 4) return true;
        }
        return false;
    }

    // ===== Heuristic board evaluation =====

    private int EvaluateBoard(int[][] grid, int rows, int cols, int player)
    {
        int opponent = player == 1 ? 2 : 1;
        int score = 0;

        // Center column preference
        int center = cols / 2;
        for (int r = 0; r < rows; r++)
            if (grid[r][center] == player) score += 3;

        score += ScoreWindows(grid, rows, cols, player);
        score -= ScoreWindows(grid, rows, cols, opponent) * 2;

        return score;
    }

    private int ScoreWindows(int[][] grid, int rows, int cols, int player)
    {
        int score = 0;
        int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };

        foreach (var d in dirs)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int endR = r + d[0] * 3, endC = c + d[1] * 3;
                    if (endR < 0 || endR >= rows || endC < 0 || endC >= cols) continue;

                    int count = 0, empties = 0, opp = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        int val = grid[r + d[0] * i][c + d[1] * i];
                        if (val == player) count++;
                        else if (val == 0) empties++;
                        else opp++;
                    }
                    if (opp > 0) continue;
                    if (count == 4) score += 1000;
                    else if (count == 3 && empties == 1) score += 50;
                    else if (count == 2 && empties == 2) score += 5;
                }
            }
        }
        return score;
    }

    // ===== Minimax (depth-limited) =====

    private int MinimaxBest(int[][] grid, int rows, int cols, int depth)
    {
        int bestCol = -1;
        int bestScore = int.MinValue;
        var valid = ValidColumns(grid, cols);
        int orderCenter = valid.OrderBy(c => Math.Abs(c - cols / 2)).ToList().First();
        // Search center-outward for better pruning
        var ordered = valid.OrderBy(c => Math.Abs(c - cols / 2)).ToList();

        foreach (int c in ordered)
        {
            if (!CanDrop(grid, c, out int r)) continue;
            grid[r][c] = 2;
            int score = Minimax(grid, rows, cols, depth - 1, false, int.MinValue, int.MaxValue);
            grid[r][c] = 0;
            if (score > bestScore)
            {
                bestScore = score;
                bestCol = c;
            }
        }

        return bestCol >= 0 ? bestCol : valid[_random.Next(valid.Count)];
    }

    private int Minimax(int[][] grid, int rows, int cols, int depth, bool maximizing, int alpha, int beta)
    {
        int winner = CheckAnyWin(grid, rows, cols);
        if (winner == 2) return 100000 + depth;
        if (winner == 1) return -100000 - depth;
        if (depth == 0 || IsBoardFull(grid)) return EvaluateBoard(grid, rows, cols, 2);

        var valid = ValidColumns(grid, cols);
        var ordered = valid.OrderBy(c => Math.Abs(c - cols / 2)).ToList();

        if (maximizing)
        {
            int value = int.MinValue;
            foreach (int c in ordered)
            {
                if (!CanDrop(grid, c, out int r)) continue;
                grid[r][c] = 2;
                value = Math.Max(value, Minimax(grid, rows, cols, depth - 1, false, alpha, beta));
                grid[r][c] = 0;
                alpha = Math.Max(alpha, value);
                if (alpha >= beta) break;
            }
            return value;
        }
        else
        {
            int value = int.MaxValue;
            foreach (int c in ordered)
            {
                if (!CanDrop(grid, c, out int r)) continue;
                grid[r][c] = 1;
                value = Math.Min(value, Minimax(grid, rows, cols, depth - 1, true, alpha, beta));
                grid[r][c] = 0;
                beta = Math.Min(beta, value);
                if (alpha >= beta) break;
            }
            return value;
        }
    }

    private int CheckAnyWin(int[][] grid, int rows, int cols)
    {
        int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int p = grid[r][c];
                if (p == 0) continue;
                foreach (var d in dirs)
                {
                    int endR = r + d[0] * 3, endC = c + d[1] * 3;
                    if (endR < 0 || endR >= rows || endC < 0 || endC >= cols) continue;
                    if (grid[r + d[0]][c + d[1]] == p &&
                        grid[r + d[0] * 2][c + d[1] * 2] == p &&
                        grid[r + d[0] * 3][c + d[1] * 3] == p)
                        return p;
                }
            }
        }
        return 0;
    }

    // ===== Ollama integration (extreme mode) =====

    private int TryOllamaMove(int[][] grid, int rows, int cols)
    {
        // Skip the HTTP call entirely if game-setup's pre-flight check failed.
        bool available = CPH.GetGlobalVar<bool>("connectfour_ollama_available", false);
        if (!available)
            return -1;

        string ollamaUrl = CPH.GetGlobalVar<string>("connectfour_ollama_url", false) ?? "";
        if (string.IsNullOrWhiteSpace(ollamaUrl))
        {
            // Try sub-action arg fallback
            if (!CPH.TryGetArg("ollamaUrl", out ollamaUrl) || string.IsNullOrWhiteSpace(ollamaUrl))
                return -1;
        }

        string ollamaModel = CPH.GetGlobalVar<string>("connectfour_ollama_model", false) ?? "";
        if (string.IsNullOrWhiteSpace(ollamaModel))
        {
            if (!CPH.TryGetArg("ollamaModel", out ollamaModel) || string.IsNullOrWhiteSpace(ollamaModel))
                ollamaModel = "llama3";
        }

        if (!ollamaUrl.EndsWith("/api/generate"))
        {
            ollamaUrl = ollamaUrl.TrimEnd('/') + "/api/generate";
        }

        string prompt = BuildOllamaPrompt(grid, rows, cols);
        string body = id736.Data.ToJson(new Dictionary<string, object>
        {
            { "model", ollamaModel },
            { "prompt", prompt },
            { "stream", false },
            { "options", new Dictionary<string, object> { { "temperature", 0.2 }, { "num_predict", 16 } } }
        });

        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                string response = client.UploadString(ollamaUrl, "POST", body);
                var parsed = JObject.Parse(response);
                string text = parsed["response"]?.ToString() ?? "";
                return ParseColumnFromText(text, cols);
            }
        }
        catch (Exception ex)
        {
            Log($"ollama error: {ex.Message}");
            return -1;
        }
    }

    private string BuildOllamaPrompt(int[][] grid, int rows, int cols)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are playing Connect Four. The board has " + cols + " columns (1-" + cols + ") and " + rows + " rows.");
        sb.AppendLine("You are player 2 (O). The opponent is player 1 (X).");
        sb.AppendLine("Pieces fall to the lowest empty row in a column.");
        sb.AppendLine("Connect 4 in a row horizontally, vertically, or diagonally to win.");
        sb.AppendLine("Current board (top row first, 0=empty, 1=opponent, 2=you):");
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                sb.Append(grid[r][c]);
                if (c < cols - 1) sb.Append(' ');
            }
            sb.AppendLine();
        }
        sb.AppendLine("Reply with ONLY a single integer 1-" + cols + " indicating the column to drop your piece. No other text.");
        return sb.ToString();
    }

    private int ParseColumnFromText(string text, int cols)
    {
        if (string.IsNullOrWhiteSpace(text)) return -1;
        foreach (var token in text.Split(new[] { ' ', ',', '.', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, out int n) && n >= 1 && n <= cols)
                return n - 1;
        }
        return -1;
    }

    // ===== Grid helpers =====

    private int[][] LoadGrid(int rows, int cols)
    {
        string json = CPH.GetGlobalVar<string>("connectfour_grid", false) ?? "[]";
        try
        {
            var arr = id736.Data.FromJson<int[][]>(json);
            if (arr != null && arr.Length == rows) return arr;
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
            if (CanDrop(grid, c, out _)) return c;
        return -1;
    }

    private List<int> ValidColumns(int[][] grid, int cols)
    {
        var list = new List<int>();
        for (int c = 0; c < cols; c++)
            if (CanDrop(grid, c, out _)) list.Add(c);
        return list;
    }

    private bool IsBoardFull(int[][] grid)
    {
        for (int r = 0; r < grid.Length; r++)
            for (int c = 0; c < grid[r].Length; c++)
                if (grid[r][c] == 0) return false;
        return true;
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
    }
}