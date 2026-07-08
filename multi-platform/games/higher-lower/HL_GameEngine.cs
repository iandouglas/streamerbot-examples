using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    string StreamerbotUserGroupName;

    bool IsInGame(string userName)
    {
        return id736.Groups.IsInGroup(userName, StreamerbotUserGroupName);
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

    private void ParseUserKey(string key, out Platform platform, out string userName)
    {
        platform = Platform.Twitch;
        userName = key;
        if (string.IsNullOrEmpty(key))
            return;
        int idx = key.IndexOf(':');
        if (idx > 0)
        {
            string platformStr = key.Substring(0, idx);
            if (Enum.TryParse(platformStr, true, out Platform parsed))
                platform = parsed;
            userName = key.Substring(idx + 1);
        }
    }

    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Groups.SetContext(CPH);
        id736.Points.SetContext(CPH);

        StreamerbotUserGroupName = "higher-lower-group";
        if (!CPH.TryGetArg("rawInput", out string rawInput))
            return false;

        string[] parts = rawInput.Trim().Split(' ');
        string subCommand = parts.Length > 0 ? parts[0].ToLower() : "";
        string pointsStr = parts.Length > 1 ? parts[1] : "";

        if (subCommand == "higher-lower" || subCommand == "hl")
        {
            StartGame(pointsStr);
            return true;
        }

        return false;
    }

    private void StartGame(string pointsStr)
    {
        if (CPH.GetGlobalVar<bool>("hl_game_active", false))
        {
            id736.Chat.SendMessage("A Higher or Lower game is already in progress!");
            return;
        }

        int gameRounds = CPH.GetGlobalVar<int>("hl_game_rounds", false);
        if (gameRounds < 1) gameRounds = 10;

        int defaultPoints = CPH.GetGlobalVar<int>("hl_default_points", false);
        if (defaultPoints < 0) defaultPoints = 1000;

        int rangeTop = CPH.GetGlobalVar<int>("hl_range_top", false);
        if (rangeTop < 10) rangeTop = 100;

        int joinTimer = CPH.GetGlobalVar<int>("hl_join_timer", false);
        if (joinTimer < 10) joinTimer = 60;

        int startingPoints = defaultPoints;
        if (!string.IsNullOrEmpty(pointsStr))
        {
            int.TryParse(pointsStr, out int parsed);
            if (parsed >= 100) startingPoints = parsed;
        }

        int pointDecay = CPH.GetGlobalVar<int>("hl_point_decay", false);
        if (pointDecay < 0) pointDecay = 100;
        int maxDecay = startingPoints / gameRounds;
        if (maxDecay < 1) maxDecay = 1;
        if (pointDecay > maxDecay)
        {
            pointDecay = maxDecay;
            string name = GetCurrencyName();
            id736.Chat.SendMessage($"Prize pool decay adjusted to {pointDecay} {name} per round to fit within {gameRounds} rounds.");
        }
        CPH.SetGlobalVar("hl_point_decay", pointDecay, false);

        id736.Groups.EnsureGroup(StreamerbotUserGroupName);
        CPH.SetGlobalVar("hl_game_active", true, false);
        CPH.SetGlobalVar("hl_max_rounds", gameRounds, false);
        CPH.SetGlobalVar("hl_phase", "join", false);
        CPH.SetGlobalVar("hl_participation", new Dictionary<string, int>(), true);
        CPH.SetGlobalVar("hl_exact_guessers", new List<string>(), true);
        CPH.SetGlobalVar("hl_starting_points", startingPoints, false);

        string currencyName = GetCurrencyName();
        id736.Chat.SendMessage($"A Higher or Lower game has started! Type !join to enter. You have {joinTimer} seconds! ({gameRounds} rounds, {startingPoints} {currencyName} prize pool per player, 1-{rangeTop})");

        ObsSetText($"Higher or Lower! Type !join to enter!\n{joinTimer}s remaining!");

        int bonus = CPH.GetGlobalVar<int>("hl_exact_bonus", false);
        if (bonus < 0) bonus = 1000;
        string guessMode = CPH.GetGlobalVar<string>("hl_guess_mode", false) ?? "first";

        id736.Chat.SendMessage($"Everyone who joins will have a few seconds to type a number in chat. Only your {guessMode} guess will be stored. Everyone's numbers will be averaged together each round.");
        id736.Chat.SendMessage($"If the average is lower than the target number, you'll be told to guess HIGHER, so next round pick a number larger than the average guess.");
        id736.Chat.SendMessage($"If the average was higher than the target number, you'll be told to guess LOWER, so next round pick a number lower than the average guess.");
        id736.Chat.SendMessage($"If anyone guesses the EXACT number during the game, they'll win an additional {bonus} {GetCurrencyName()}!");

        ObsShow();

        int elapsed = 0;
        while (elapsed < joinTimer)
        {
            int waitTime = Math.Min(10, joinTimer - elapsed);
            CPH.Wait(waitTime * 1000);
            if (!CPH.GetGlobalVar<bool>("hl_game_active", false))
                return;
            elapsed += waitTime;
            int remaining = joinTimer - elapsed;
            if (remaining > 0)
            {
                int count = GetPlayerCount();
                id736.Chat.SendMessage($"{remaining} seconds left to !join! Players so far: {count}");
            }
        }

        if (!CPH.GetGlobalVar<bool>("hl_game_active", false))
            return;

        ObsHide();
        CPH.SetGlobalVar("hl_phase", "idle", false);

        int joinedCount = GetPlayerCount();
        if (joinedCount == 0)
        {
            id736.Chat.SendMessage("No one joined! Game cancelled.");
            CleanupGame();
            return;
        }

        int targetNumber = CPH.Between(1, rangeTop);
        CPH.SetGlobalVar("hl_target_number", targetNumber, false);
        CPH.SetGlobalVar("hl_winnable_points", startingPoints, false);
        CPH.SetGlobalVar("hl_number_guessed", false, false);
        CPH.SetGlobalVar("hl_round", 1, false);

        id736.Chat.SendMessage($"Game on! {joinedCount} player(s) are playing. The number is between 1 and {rangeTop}. Starting round 1 in 15 seconds...");

        string joinplural = joinedCount == 1 ? "" : "s";
        ObsSetText($"Game on! {joinedCount} player{joinplural}!\nWe'll start the game in 15s...");
        ObsShow();
        CPH.Wait(15000);
        ObsHide();

        if (!CPH.GetGlobalVar<bool>("hl_game_active", false))
            return;

        RunGameLoop();
    }

    private void RunGameLoop()
    {
        int round = 1;
        int maxRounds = CPH.GetGlobalVar<int>("hl_max_rounds", false);
        bool numberGuessed = false;

        int guessTimer = CPH.GetGlobalVar<int>("hl_guess_timer", false);
        if (guessTimer < 5) guessTimer = 30;

        int rangeTop = CPH.GetGlobalVar<int>("hl_range_top", false);
        if (rangeTop < 10) rangeTop = 100;

        while (round <= maxRounds && !numberGuessed)
        {
            if (!CPH.GetGlobalVar<bool>("hl_game_active", false))
                return;

            CPH.SetGlobalVar("hl_phase", "guess", false);
            CPH.SetGlobalVar("hl_round", round, false);
            CPH.SetGlobalVar($"hl_round_{round}_guessers", new List<string>(), false);
            CPH.SetGlobalVar($"hl_round_{round}_guesses", new List<int>(), false);

            int winnable = CPH.GetGlobalVar<int>("hl_winnable_points", false);
            id736.Chat.SendMessage($"Round {round}/{maxRounds} - Type a number between 1 and {rangeTop}! You have {guessTimer} seconds. Prize pool: {winnable} {GetCurrencyName()}");

            ObsSetText($"Round {round}/{maxRounds} - Guess 1-{rangeTop}!\nPrize available per player: {winnable} {GetCurrencyName()}");
            ObsShow();

            CPH.Wait(guessTimer * 1000);

            if (!CPH.GetGlobalVar<bool>("hl_game_active", false))
                return;

            ObsHide();
            CPH.SetGlobalVar("hl_phase", "judging", false);

            ProcessRoundResults(round, ref numberGuessed);

            if (!numberGuessed)
            {
                round++;
                id736.Chat.SendMessage($"Next round starting in 5 seconds...");
                int wait = 5000;
                CPH.Wait(wait);
            }
        }

        int targetNumber = CPH.GetGlobalVar<int>("hl_target_number", false);
        int finalWinnable = CPH.GetGlobalVar<int>("hl_winnable_points", false);
        string currencyName = GetCurrencyName();

        if (!numberGuessed && round > maxRounds)
        {
            id736.Chat.SendMessage($"No one guessed the number in {maxRounds} rounds! The number was {targetNumber}. Better luck next time!");
            ObsSetText($"No one guessed the number! It was {targetNumber}");
            ObsShow();
            CPH.Wait(5000);
            ObsHide();
        }

        int totalRounds = numberGuessed ? round : maxRounds;

        Dictionary<string, int> participation = GetParticipation();
        List<string> allUsers = GetPlayers();
        var exactGuessers = GetExactGuessers();

        int bonus = CPH.GetGlobalVar<int>("hl_exact_bonus", false);
        if (bonus < 0) bonus = 0;

        Dictionary<string, int> awards = new Dictionary<string, int>(); // key = platform:username
        Dictionary<string, int> userRounds = new Dictionary<string, int>();
        int totalAwarded = 0;
        int participantCount = 0;

        foreach (string userKey in allUsers)
        {
            int roundsPlayed = participation.ContainsKey(userKey) ? participation[userKey] : 0;
            userRounds[userKey] = roundsPlayed;
            double pct = totalRounds > 0 ? (double)roundsPlayed / totalRounds : 0;
            int points = (int)Math.Round(finalWinnable * pct);
            if (points > 0)
            {
                awards[userKey] = points;
                totalAwarded += points;
                participantCount++;
            }
        }

        foreach (var kvp in awards)
        {
            ParseUserKey(kvp.Key, out Platform platform, out string userName);
            AwardPoints(userName, platform, kvp.Value);
        }

        if (allUsers.Count <= 10)
        {
            foreach (string userKey in allUsers)
            {
                ParseUserKey(userKey, out Platform platform, out string userName);
                int roundsPlayed = userRounds.ContainsKey(userKey) ? userRounds[userKey] : 0;
                int points = awards.ContainsKey(userKey) ? awards[userKey] : 0;
                bool isExactGuesser = exactGuessers.Contains(userKey);

                if (points == 0 && !isExactGuesser)
                {
                    continue;
                }

                string msg = $"@{userName} was awarded";
                if (points > 0)
                {
                    msg += $" {points} {currencyName} for participating in";
                    if (roundsPlayed == totalRounds)
                        msg += $" all {totalRounds} rounds";
                    else
                        msg += $" {roundsPlayed} of {totalRounds} rounds";
                }
                if (isExactGuesser)
                {
                    if (points > 0)
                        msg += $" AND";
                    msg += $" {bonus} {currencyName} for guessing the exact number at some point in the game";
                }
                msg += "!";
                id736.Chat.SendMessage(msg);
            }
        }

        int totalBonus = bonus * exactGuessers.Count;
        int grandTotal = totalAwarded + totalBonus;
        string playerWord = participantCount == 1 ? "player" : "players";
        id736.Chat.SendMessage($"Thanks for playing Higher or Lower! We awarded a total of {grandTotal} {currencyName} to {participantCount} {playerWord}.");

        if (exactGuessers.Count > 0)
        {
            foreach (string key in exactGuessers)
            {
                ParseUserKey(key, out Platform platform, out string userName);
                AwardPoints(userName, platform, bonus);
            }

            string bonusPrefix = $"We awarded {totalBonus} {currencyName} bonus points to the following players for guessing the number exactly: ";
            int maxMessageLength = 500;
            int prefixLength = bonusPrefix.Length;
            int budget = maxMessageLength - prefixLength;

            if (budget < 1) budget = 1;

            var names = exactGuessers.Select(u =>
            {
                ParseUserKey(u, out _, out string name);
                return $"@{name}";
            }).ToList();
            List<string> currentBatch = new List<string>();
            int currentLength = 0;

            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                int separator = currentBatch.Count > 0 ? 2 : 0;
                if (currentLength + separator + name.Length > budget && currentBatch.Count > 0)
                {
                    id736.Chat.SendMessage(bonusPrefix + string.Join(", ", currentBatch));
                    currentBatch.Clear();
                    currentLength = 0;
                }
                currentBatch.Add(name);
                currentLength += separator + name.Length;
            }

            if (currentBatch.Count > 0)
            {
                id736.Chat.SendMessage(bonusPrefix + string.Join(", ", currentBatch));
            }
        }

        CleanupGame();
    }

    private void ProcessRoundResults(int round, ref bool numberGuessed)
    {
        int pointDecay = CPH.GetGlobalVar<int>("hl_point_decay", false);
        if (pointDecay < 0) pointDecay = 100;

        var guesses = CPH.GetGlobalVar<List<int>>($"hl_round_{round}_guesses", false) ?? new List<int>();

        if (guesses.Count == 0)
        {
            int winnable = CPH.GetGlobalVar<int>("hl_winnable_points", false) - pointDecay;
            if (winnable < 0) winnable = 0;
            CPH.SetGlobalVar("hl_winnable_points", winnable, false);

            id736.Chat.SendMessage($"No one submitted a guess this round! Prize pool reduced to {winnable} {GetCurrencyName()}.");
            return;
        }

        int avg = (int)Math.Round(guesses.Average());
        int target = CPH.GetGlobalVar<int>("hl_target_number", false);

        var roundGuessers = CPH.GetGlobalVar<List<string>>($"hl_round_{round}_guessers", false) ?? new List<string>();

        UpdateParticipation(roundGuessers);

        if (avg == target)
        {
            numberGuessed = true;
            CPH.SetGlobalVar("hl_number_guessed", true, false);

            id736.Chat.SendMessage($"The average was {avg} - CORRECT! The number was {target}!");
            ObsSetText($"CORRECT! The number was {target}!");
            ObsShow();
            CPH.Wait(8000);
            ObsHide();
        }
        else
        {
            int winnable = CPH.GetGlobalVar<int>("hl_winnable_points", false) - pointDecay;
            if (winnable < 0) winnable = 0;
            CPH.SetGlobalVar("hl_winnable_points", winnable, false);

            string hint = avg < target ? "guess HIGHER" : "guess LOWER";
            id736.Chat.SendMessage($"Average guess was {avg}. {hint}! Prize pool reduced to {winnable} {GetCurrencyName()}.");
        }
    }

    private void UpdateParticipation(List<string> roundGuessers)
    {
        var participation = GetParticipation();
        foreach (string key in roundGuessers)
        {
            if (participation.ContainsKey(key))
                participation[key]++;
            else
                participation[key] = 1;
        }
        CPH.SetGlobalVar("hl_participation", participation, true);
    }

    private Dictionary<string, int> GetParticipation()
    {
        return CPH.GetGlobalVar<Dictionary<string, int>>("hl_participation", true) ?? new Dictionary<string, int>();
    }

    private List<string> GetExactGuessers()
    {
        return CPH.GetGlobalVar<List<string>>("hl_exact_guessers", true) ?? new List<string>();
    }

    private void AwardPoints(string userName, Platform platform, int points)
    {
        if (string.IsNullOrEmpty(userName) || points <= 0)
            return;

        string currencyName = CPH.GetGlobalVar<string>("hl_currency_name", false);
        if (string.IsNullOrEmpty(currencyName))
            currencyName = "points";

        int total = id736.Points.Add(userName, platform, currencyName, points);

        id736.Chat.SendMessage($"we just gave {userName} {points} points, they have {total} total points");
        CPH.LogInfo($"[HigherLower] Awarded {points} {currencyName} to {userName} ({platform})");
    }

    private List<string> GetPlayers()
    {
        return CPH.GetGlobalVar<List<string>>("hl_players", true) ?? new List<string>();
    }

    private int GetPlayerCount()
    {
        return GetPlayers().Count;
    }

    private string GetUserDisplayName(string key)
    {
        ParseUserKey(key, out _, out string userName);
        return userName;
    }

    private void CleanupGame()
    {
        ObsHide();

        int maxRounds = CPH.GetGlobalVar<int>("hl_max_rounds", false);

        for (int i = 1; i <= maxRounds; i++)
        {
            CPH.UnsetGlobalVar($"hl_round_{i}_guessers", false);
            CPH.UnsetGlobalVar($"hl_round_{i}_guesses", false);
        }

        CPH.UnsetGlobalVar("hl_target_number", false);
        CPH.UnsetGlobalVar("hl_winnable_points", false);
        CPH.UnsetGlobalVar("hl_number_guessed", false);
        CPH.UnsetGlobalVar("hl_round", false);
        CPH.UnsetGlobalVar("hl_max_rounds", false);
        CPH.UnsetGlobalVar("hl_phase", false);
        CPH.UnsetGlobalVar("hl_participation", true);
        CPH.UnsetGlobalVar("hl_exact_guessers", true);
        CPH.UnsetGlobalVar("hl_players", true);

        id736.Groups.Clear(StreamerbotUserGroupName);
        CPH.SetGlobalVar("hl_game_active", false, false);
    }

    private string GetCurrencyName()
    {
        string name = CPH.GetGlobalVar<string>("hl_currency_name", false);
        return string.IsNullOrEmpty(name) ? "points" : name;
    }

    private bool ShouldUseObs()
    {
        return CPH.GetGlobalVar<bool>("hl_use_obs", false)
            && !string.IsNullOrEmpty(CPH.GetGlobalVar<string>("hl_obs_scene", false))
            && !string.IsNullOrEmpty(CPH.GetGlobalVar<string>("hl_obs_source", false));
    }

    private void ObsShow()
    {
        if (!ShouldUseObs()) return;
        string scene = CPH.GetGlobalVar<string>("hl_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("hl_obs_source", false) ?? "";
        CPH.ObsSetSourceVisibility(scene, source, true);
    }

    private void ObsHide()
    {
        if (!ShouldUseObs()) return;
        string scene = CPH.GetGlobalVar<string>("hl_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("hl_obs_source", false) ?? "";
        CPH.ObsSetSourceVisibility(scene, source, false);
    }

    private void ObsSetText(string text)
    {
        if (!ShouldUseObs()) return;
        string source = CPH.GetGlobalVar<string>("hl_obs_source", false) ?? "";

        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = " " + lines[i] + " ";
        string padded = string.Join("\n", lines);

        string escaped = JsonEscape(padded);
        string json = "{\"inputName\":\"" + JsonEscape(source) + "\",\"inputSettings\":{\"text\":\"" + escaped + "\"},\"overlay\":true}";
        CPH.ObsSendRaw("SetInputSettings", json, 0);
    }

    private string JsonEscape(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }
}
