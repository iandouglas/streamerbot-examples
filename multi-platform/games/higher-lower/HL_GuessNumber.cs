using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
    bool IsInGame(string userName)
    {
        return CPH.UserInGroup(userName, Platform.Twitch, "higher-lower-group")
            || CPH.UserInGroup(userName, Platform.YouTube, "higher-lower-group")
            || CPH.UserInGroup(userName, Platform.Kick, "higher-lower-group");
    }

    private string MakeUserKey(Platform platform, string userId)
    {
        return $"{platform.ToString().ToLower()}:{userId}";
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("user", out string userName))
            return false;
        if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
            userId = userName;
        if (!CPH.TryGetArg("userType", out string userType))
            userType = "twitch";
        if (!CPH.TryGetArg("rawInput", out string rawInput))
            return false;

        if (!Enum.TryParse(userType, true, out Platform platform))
            platform = Platform.Twitch;

        string userKey = MakeUserKey(platform, userId);

        bool gameActive = CPH.GetGlobalVar<bool>("hl_game_active", false);
        if (!gameActive)
            return false;

        string phase = CPH.GetGlobalVar<string>("hl_phase", false) ?? "";
        if (phase != "guess")
            return false;

        if (!IsInGame(userName))
        {
            return false;
        }

        string trimmed = rawInput.Trim();

        bool narrowRange = CPH.GetGlobalVar<bool>("hl_mode_narrow_range", false);
        int currentMin = CPH.GetGlobalVar<int>("hl_current_min", false);
        int currentMax = CPH.GetGlobalVar<int>("hl_current_max", false);
        int rangeTop = CPH.GetGlobalVar<int>("hl_mode_range_top", false);
        if (rangeTop < 10) rangeTop = 100;

        int effectiveMin = narrowRange ? currentMin : 1;
        int effectiveMax = narrowRange ? currentMax : rangeTop;

        if (!int.TryParse(trimmed, out int guess) || guess < effectiveMin || guess > effectiveMax)
        {
            if (narrowRange && (guess < effectiveMin || guess > effectiveMax))
                id736.Chat.SendMessage($"Sorry {userName}, that number is outside the current range {effectiveMin}-{effectiveMax}. Try again.");
            else
                id736.Chat.SendMessage($"Sorry {userName} that number was out of range");
            return false;
        }

        int round = CPH.GetGlobalVar<int>("hl_round", false);
        string guessMode = CPH.GetGlobalVar<string>("hl_guess_mode", false) ?? "first";

        var roundGuessers = CPH.GetGlobalVar<List<string>>($"hl_round_{round}_guessers", false) ?? new List<string>();
        var guesses = CPH.GetGlobalVar<List<int>>($"hl_round_{round}_guesses", false) ?? new List<int>();

        if (roundGuessers.Contains(userKey))
        {
            if (guessMode == "last")
            {
                int idx = roundGuessers.IndexOf(userKey);
                guesses[idx] = guess;
                CPH.SetGlobalVar($"hl_round_{round}_guesses", guesses, false);
            }
            else
            {
                id736.Chat.SendMessage($"@{userName}, you already submitted a guess this round!", true);
            }
            return true;
        }

        roundGuessers.Add(userKey);
        CPH.SetGlobalVar($"hl_round_{round}_guessers", roundGuessers, false);

        guesses.Add(guess);
        CPH.SetGlobalVar($"hl_round_{round}_guesses", guesses, false);

        int target = CPH.GetGlobalVar<int>("hl_target_number", false);
        int maxRounds = CPH.GetGlobalVar<int>("hl_mode_rounds", false);
        if (maxRounds < 1) maxRounds = 10;
        bool allowExactBonus = round < maxRounds;

        if (allowExactBonus && guess == target)
        {
            var exactGuessers = CPH.GetGlobalVar<List<string>>("hl_exact_guessers", false) ?? new List<string>();
            if (!exactGuessers.Contains(userKey))
            {
                exactGuessers.Add(userKey);
                CPH.SetGlobalVar("hl_exact_guessers", exactGuessers, false);
            }
        }

        return true;
    }
}