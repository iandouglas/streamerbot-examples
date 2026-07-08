using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    public void SendChatMessage(string message, bool fromBot = true)
    {
        string platform;
        CPH.TryGetArg("userType", out platform);
        platform = platform?.ToLower() ?? "twitch";

        switch (platform)
        {
            case "youtube":
                CPH.SendYouTubeMessageToLatestMonitored(message, false);
                break;
            case "kick":
                CPH.SendKickMessage(message, fromBot);
                break;
            default:
                CPH.SendMessage(message, fromBot);
                break;
        }
    }

    bool IsInGame(string userName)
    {
        return CPH.UserInGroup(userName, Platform.Twitch, "higher-lower-group")
            || CPH.UserInGroup(userName, Platform.YouTube, "higher-lower-group")
            || CPH.UserInGroup(userName, Platform.Kick, "higher-lower-group");
    }

    private string MakeUserKey(Platform platform, string userName)
    {
        return $"{platform.ToString().ToLower()}:{userName}";
    }

    private string MakeUserKey(string platform, string userName)
    {
        if (Enum.TryParse(platform, true, out Platform parsed))
            return MakeUserKey(parsed, userName);
        return $"twitch:{userName}";
    }

    public bool Execute()
    {
        if (!CPH.TryGetArg("user", out string userName))
            return false;
        if (!CPH.TryGetArg("userType", out string userType))
            userType = "twitch";
        if (!CPH.TryGetArg("rawInput", out string rawInput))
            return false;

        if (!Enum.TryParse(userType, true, out Platform platform))
            platform = Platform.Twitch;

        string userKey = MakeUserKey(platform, userName);

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
        int rangeTop = CPH.GetGlobalVar<int>("hl_range_top", false);
        if (rangeTop < 10) rangeTop = 10;
        if (!int.TryParse(trimmed, out int guess) || guess < 1 || guess > rangeTop)
        {
            SendChatMessage($"Sorry {userName} that number was out of range");
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
                SendChatMessage($"@{userName}, you already submitted a guess this round!", true);
            }
            return true;
        }

        roundGuessers.Add(userKey);
        CPH.SetGlobalVar($"hl_round_{round}_guessers", roundGuessers, false);

        guesses.Add(guess);
        CPH.SetGlobalVar($"hl_round_{round}_guesses", guesses, false);

        int target = CPH.GetGlobalVar<int>("hl_target_number", false);
        if (guess == target)
        {
            var exactGuessers = CPH.GetGlobalVar<List<string>>("hl_exact_guessers", true) ?? new List<string>();
            if (!exactGuessers.Contains(userKey))
            {
                exactGuessers.Add(userKey);
                CPH.SetGlobalVar("hl_exact_guessers", exactGuessers, true);
            }
        }

        return true;
    }
}
