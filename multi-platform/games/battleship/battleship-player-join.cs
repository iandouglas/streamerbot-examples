using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "battleship");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("user", out string userName) || string.IsNullOrWhiteSpace(userName))
            return false;

        if (!CPH.TryGetArg("userName", out string displayName))
            displayName = userName;

        if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
            userId = userName;

        if (!CPH.TryGetArg("userType", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        object msgIdObj;
        string msgId = CPH.TryGetArg("msgId", out msgIdObj) ? msgIdObj?.ToString() : null;

        bool gameActive = CPH.GetGlobalVar<bool>("battleship_game_active", false);
        if (!gameActive)
        {
            id736.Chat.SendReplyOrMessage("No Battleship game is currently running!", msgId);
            return false;
        }

        string groupName = CPH.GetGlobalVar<string>("battleship_group", false) ?? "battleship-players";

        if (id736.Groups.IsInGroup(userName, groupName))
        {
            id736.Chat.SendReplyOrMessage("You've already joined the game!", msgId);
            return false;
        }

        id736.Groups.AddUser(userName, platform, groupName);

        string userKey = $"{platform}:{userId}";

        // Store display name
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();
        playerNames[userKey] = displayName;
        CPH.SetGlobalVar("battleship_player_names", id736.Data.ToJson(playerNames), false);

        // Initialize player stats
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
            CPH.SetGlobalVar("battleship_player_stats", id736.Data.ToJson(stats), false);
        }

        Log($"join: {displayName}@{platform} joined the game");
        id736.Chat.SendReplyOrMessage($"@{userName} welcome to Battleship! Enter coordinates like 'B5' to fire!", msgId);
        return true;
    }
}