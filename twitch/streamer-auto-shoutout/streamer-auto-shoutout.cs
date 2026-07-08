using System;

public class CPHInline
{
    public bool Execute()
    {
        string userName;
        string displayName;
        string groupName = "streamers";

        if (!CPH.TryGetArg("userName", out userName) || string.IsNullOrWhiteSpace(userName))
            return false;

        CPH.TryGetArg("displayName", out displayName);

        if (CPH.UserInGroup(userName, Platform.Twitch, groupName) ||
            (!string.IsNullOrWhiteSpace(displayName) && CPH.UserInGroup(displayName, Platform.Twitch, groupName)))
            CPH.SendMessage($"!so {userName}");

        return true;
    }
}