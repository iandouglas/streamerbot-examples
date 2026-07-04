using System;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("user", out string user))
            return false;
        if (!CPH.TryGetArg("userName", out string userName))
            userName = user;
        if (!CPH.TryGetArg("userId", out string userId))
            return false;
        if (!CPH.TryGetArg("msgId", out string msgId))
            return false;
        if (!CPH.TryGetArg("userType", out string userType))
            userType = "twitch";

        if (!Enum.TryParse(userType, true, out Platform platform))
            platform = Platform.Twitch;

        bool gameActive = CPH.GetGlobalVar<bool>("hl_game_active", false);
        if (!gameActive)
            return false;

        string phase = CPH.GetGlobalVar<string>("hl_phase", false) ?? "";
        if (phase != "join")
        {
            CPH.TwitchReplyToMessage($"Sorry, the join window has already closed!", msgId, true);
            return false;
        }

        if (CPH.UserInGroup(userName, platform, "higher-lower-group"))
        {
            CPH.TwitchReplyToMessage($"You've already joined the game!", msgId, true);
            return false;
        }

        CPH.AddUserToGroup(userName, platform, "higher-lower-group");
        CPH.TwitchReplyToMessage($"@{user} welcome to Higher or Lower!", msgId, true);
        return true;
    }
}
