using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("user", out string user))
            return false;
        if (!CPH.TryGetArg("userName", out string userName))
            userName = user;
        if (!CPH.TryGetArg("userType", out string userType))
            userType = "twitch";
        if (!CPH.TryGetArg("rawInput", out string rawInput))
            return false;

        if (!Enum.TryParse(userType, true, out Platform platform))
            platform = Platform.Twitch;

        bool gameActive = CPH.GetGlobalVar<bool>("hl_game_active", false);
        if (!gameActive)
            return true;

        string phase = CPH.GetGlobalVar<string>("hl_phase", false) ?? "";
        if (phase != "guess")
            return true;

        if (!CPH.UserInGroup(userName, platform, "higher-lower-group"))
            return true;

        string trimmed = rawInput.Trim();
        int rangeTop = CPH.GetGlobalVar<int>("hl_range_top", false);
        if (rangeTop < 10) rangeTop = 100;
        if (!int.TryParse(trimmed, out int guess) || guess < 1 || guess > rangeTop)
            return true;

        int round = CPH.GetGlobalVar<int>("hl_round", false);
        string guessMode = CPH.GetGlobalVar<string>("hl_guess_mode", false) ?? "first";

        var roundGuessers = CPH.GetGlobalVar<List<string>>($"hl_round_{round}_guessers", false) ?? new List<string>();
        var guesses = CPH.GetGlobalVar<List<int>>($"hl_round_{round}_guesses", false) ?? new List<int>();

        if (roundGuessers.Contains(userName))
        {
            if (guessMode == "last")
            {
                int idx = roundGuessers.IndexOf(userName);
                guesses[idx] = guess;
                CPH.SetGlobalVar($"hl_round_{round}_guesses", guesses, false);
            }
            else
            {
                CPH.SendMessage($"@{user}, you already submitted a guess this round!", true);
            }
            return true;
        }

        roundGuessers.Add(userName);
        CPH.SetGlobalVar($"hl_round_{round}_guessers", roundGuessers, false);

        guesses.Add(guess);
        CPH.SetGlobalVar($"hl_round_{round}_guesses", guesses, false);

        int target = CPH.GetGlobalVar<int>("hl_target_number", false);
        if (guess == target)
        {
            var exactGuessers = CPH.GetGlobalVar<List<string>>("hl_exact_guessers", true) ?? new List<string>();
            if (!exactGuessers.Contains(userName))
            {
                exactGuessers.Add(userName);
                CPH.SetGlobalVar("hl_exact_guessers", exactGuessers, true);
            }
        }

        return true;
    }
}
