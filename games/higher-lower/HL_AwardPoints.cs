using System;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("hlAwardUser", out string userName))
            return false;
        if (!CPH.TryGetArg("hlAwardPoints", out int points))
            return false;

        string currencyName = CPH.GetGlobalVar<string>("hl_currency_name", false);
        if (string.IsNullOrEmpty(currencyName))
            currencyName = "points";

        int current = CPH.GetTwitchUserVar<int>(userName, currencyName, true);
        current += points;
        CPH.SetTwitchUserVar(userName, currencyName, current, true);

        CPH.LogInfo($"Awarded {points} {currencyName} to {userName} (total: {current})");
        return true;
    }
}
