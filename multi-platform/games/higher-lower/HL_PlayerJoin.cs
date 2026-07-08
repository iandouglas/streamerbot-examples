using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    bool IsInGame(string userName)
    {
        return id736.Groups.IsInGroup(userName, "higher-lower-group");
    }

    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Groups.SetContext(CPH);

        if (!CPH.TryGetArg("user", out string user))
            return false;
        if (!CPH.TryGetArg("userName", out string userName))
            userName = user;
        if (!CPH.TryGetArg("userId", out string userId))
            return false;
        string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;
        string platform = id736.Chat.GetCurrentPlatform();

        bool gameActive = CPH.GetGlobalVar<bool>("hl_game_active", false);
        if (!gameActive)
            return false;

        string phase = CPH.GetGlobalVar<string>("hl_phase", false) ?? "";
        if (phase != "join")
        {
            id736.Chat.SendReplyOrMessage("Sorry, the join window has already closed!", msgId);
            return false;
        }

        if (IsInGame(userName))
        {
            id736.Chat.SendReplyOrMessage("You've already joined the game!", msgId);
            return false;
        }

        id736.Groups.AddUser(userName, platform, "higher-lower-group");

        var players = CPH.GetGlobalVar<List<string>>("hl_players", true) ?? new List<string>();
        string userKey = $"{platform}:{userName}";
        if (!players.Contains(userKey))
        {
            players.Add(userKey);
            CPH.SetGlobalVar("hl_players", players, true);
        }

        id736.Chat.SendReplyOrMessage($"@{user} welcome to Higher or Lower!", msgId);
        return true;
    }
}
