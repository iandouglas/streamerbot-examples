using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using id736 = iandouglas736;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "coord");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);
        Log("coord: action triggered");

        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
        {
            Log("coord: game not active, ignoring");
            return false;
        }

        if (!CPH.GetGlobalVar<bool>("battleship_collecting", false))
        {
            Log("coord: not collecting, ignoring");
            return false;
        }

        if (!CPH.TryGetArg("user", out string userName) || string.IsNullOrWhiteSpace(userName))
        {
            Log("coord: no user argument");
            return false;
        }

        if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
            userId = userName;

        if (!CPH.TryGetArg("userType", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        if (!CPH.TryGetArg("rawInput", out string rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            Log($"coord: no rawInput for {userName}");
            return false;
        }

        string groupName = CPH.GetGlobalVar<string>("battleship_group", false) ?? "battleship-players";
        if (!id736.Groups.IsInGroup(userName, groupName))
        {
            Log($"coord: {userName} not in group '{groupName}'");
            return false;
        }

        int gridSize = CPH.GetGlobalVar<int>("battleship_grid_size", false);
        if (gridSize < 1) gridSize = 10;

        // Parse coordinate from rawInput
        string trimmed = rawInput.Trim().ToUpperInvariant();
        Log($"coord: parsing '{trimmed}' from {userName}@{platform}");

        // Remove the command name if present
        if (trimmed.StartsWith("BATTLESHIP") || trimmed.StartsWith("BS"))
        {
            int spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx >= 0 && spaceIdx < trimmed.Length - 1)
                trimmed = trimmed.Substring(spaceIdx + 1).Trim();
        }

        // Regex: letter A-J (or A-O for 15x15) followed by 1-10 (or 1-15)
        // Allow optional whitespace between letter and number
        char maxLetter = (char)('A' + gridSize - 1);
        string rowPattern = $"[A-{maxLetter}]";
        string colPattern = gridSize <= 10
            ? "(10|[1-9])"
            : $"(1[0-{gridSize - 10}]|[1-9])";

        string pattern = $@"^\s*({rowPattern})\s*({colPattern})\s*$";
        Log($"coord: regex pattern = {pattern}");
        var match = Regex.Match(trimmed, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            Log($"coord: '{trimmed}' did not match pattern {pattern}");
            return false;
        }

        char rowLetter = match.Groups[1].Value.ToUpperInvariant()[0];
        int row = rowLetter - 'A'; // 0-indexed
        if (!int.TryParse(match.Groups[2].Value, out int col))
        {
            Log($"coord: could not parse column from '{match.Groups[2].Value}'");
            return false;
        }
        col = col - 1; // 0-indexed

        Log($"coord: {userName} fired {rowLetter}{col + 1} -> row={row} col={col}");

        if (row < 0 || row >= gridSize || col < 0 || col >= gridSize)
            return false;

        string userKey = $"{platform}:{userId}";

        // Check if player is muted
        var mutedPlayers = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_muted_players", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        Log($"coord: checking mute status for {userName}@{platform}, muted list has {mutedPlayers.Count} entries");
        foreach (var mp in mutedPlayers)
        {
            string mpUser = mp["user"]?.ToString() ?? "";
            string mpPlatform = mp["platform"]?.ToString() ?? "";
            Log($"coord: muted list entry: user='{mpUser}' platform='{mpPlatform}'");
        }

        bool isMuted = mutedPlayers.Exists(m =>
            (m["user"]?.ToString() ?? "") == userName &&
            (m["platform"]?.ToString() ?? "").Equals(platform, StringComparison.OrdinalIgnoreCase));

        if (isMuted)
        {
            Log($"coord: {userName}@{platform} is muted, dropping coordinate {rowLetter}{col + 1}");
            return true;
        }

        // Add coordinate to pool
        var coords = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_coords", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        coords.Add(new Dictionary<string, object>
        {
            { "row", row },
            { "col", col },
            { "user", userName },
            { "platform", platform }
        });
        CPH.SetGlobalVar("battleship_coords", id736.Data.ToJson(coords), false);

        // Update player stats
        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();

        if (!stats.ContainsKey(userKey))
        {
            stats[userKey] = new Dictionary<string, object>
            {
                { "coordCount", 0 },
                { "muteCount", 0 },
                { "roundsPlayed", 0 }
            };
        }
        stats[userKey]["coordCount"] = Convert.ToInt32(stats[userKey]["coordCount"]) + 1;

        // Track if player participated this round
        int currentRound = CPH.GetGlobalVar<int>("battleship_round", false);
        string roundKey = $"round_{currentRound}";
        if (!stats[userKey].ContainsKey(roundKey) || !Convert.ToBoolean(stats[userKey][roundKey]))
        {
            stats[userKey][roundKey] = true;
            stats[userKey]["roundsPlayed"] = Convert.ToInt32(stats[userKey]["roundsPlayed"]) + 1;
        }

        CPH.SetGlobalVar("battleship_player_stats", id736.Data.ToJson(stats), false);

        // Store player display name
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();
        if (!playerNames.ContainsKey(userKey))
            playerNames[userKey] = userName;
        CPH.SetGlobalVar("battleship_player_names", id736.Data.ToJson(playerNames), false);

        // Forward coordinate to JS
        SendEvent("coord", new Dictionary<string, object>
        {
            { "row", row },
            { "col", col },
            { "user", userName },
            { "platform", platform }
        });
        Log($"coord: sent coord event to browser for {userName} {rowLetter}{col + 1}");

        return true;
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
    }
}