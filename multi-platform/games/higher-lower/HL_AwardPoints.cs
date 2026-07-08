using System;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("hlAwardUser", out string userName))
            return false;
        if (!CPH.TryGetArg("hlAwardPoints", out int points))
            return false;
        if (!CPH.TryGetArg("hlAwardPlatform", out string platformStr))
            platformStr = "twitch";

        if (!Enum.TryParse(platformStr, true, out Platform platform))
            platform = Platform.Twitch;

        string currencyName = CPH.GetGlobalVar<string>("hl_currency_name", false);
        if (string.IsNullOrEmpty(currencyName))
            currencyName = "points";

        int current = 0;

        switch (platform)
        {
            case Platform.Twitch:
                current = CPH.GetTwitchUserVar<int>(userName, currencyName, true);
                current += points;
                CPH.SetTwitchUserVar(userName, currencyName, current, true);
                break;
            case Platform.YouTube:
                current = CPH.GetYouTubeUserVar<int>(userName, currencyName, true);
                current += points;
                CPH.SetYouTubeUserVar(userName, currencyName, current, true);
                break;
            case Platform.Kick:
                current = CPH.GetKickUserVar<int>(userName, currencyName, true);
                current += points;
                CPH.SetKickUserVar(userName, currencyName, current, true);
                break;
        }

        CPH.LogInfo($"[HigherLower] Awarded {points} {currencyName} to {userName} ({platform})");
        return true;
    }
}
