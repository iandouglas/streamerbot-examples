using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    string StreamerbotUserGroupName;

    bool IsInGame(string userName)
    {
        return id736.Groups.IsInGroup(userName, StreamerbotUserGroupName);
    }

    private string MakeUserKey(Platform platform, string userId)
    {
        return $"{platform.ToString().ToLower()}:{userId}";
    }

    private string MakeUserKey(string platform, string userId)
    {
        if (Enum.TryParse(platform, true, out Platform parsed))
            return MakeUserKey(parsed, userId);
        return $"twitch:{userId}";
    }

    private void ParseUserKey(string key, out Platform platform, out string userId)
    {
        platform = Platform.Twitch;
        userId = key;
        if (string.IsNullOrEmpty(key))
            return;
        int idx = key.IndexOf(':');
        if (idx > 0)
        {
            string platformStr = key.Substring(0, idx);
            if (Enum.TryParse(platformStr, true, out Platform parsed))
                platform = parsed;
            userId = key.Substring(idx + 1);
        }
    }

    private string GetUserDisplayName(string userKey)
    {
        var playerNames = CPH.GetGlobalVar<Dictionary<string, string>>("hl_player_names", false) ?? new Dictionary<string, string>();
        return playerNames.TryGetValue(userKey, out string name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : userKey;
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        StreamerbotUserGroupName = "higher-lower-group";
        if (!CPH.TryGetArg("rawInput", out string rawInput))
            return false;

        string[] parts = rawInput.Trim().Split(' ');
        string subCommand = parts.Length > 0 ? parts[0].ToLower() : "";
        string modeArg = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : "normal";

        if (subCommand == "higher-lower" || subCommand == "hl")
        {
            StartGame(modeArg);
            return true;
        }

        return false;
    }

    private void LoadModeConfig(string mode, int playerCount,
        out int rounds, out int rangeTop, out int startingPool,
        out int exactBonus, out int roundDelayMs, out bool narrowRange)
    {
        roundDelayMs = CPH.GetGlobalVar<int>("hl_round_delay_ms", false);
        if (roundDelayMs < 1000) roundDelayMs = 5000;

        switch (mode)
        {
            case "easy":
                rounds = CPH.GetGlobalVar<int>("hl_easy_rounds", false);
                if (rounds < 1) rounds = 10;
                rangeTop = CPH.GetGlobalVar<int>("hl_easy_range_top", false);
                if (rangeTop < 10) rangeTop = 100;
                startingPool = CPH.GetGlobalVar<int>("hl_easy_starting_pool", false);
                if (startingPool < 100) startingPool = 1000;
                exactBonus = CPH.GetGlobalVar<int>("hl_easy_exact_bonus", false);
                if (exactBonus < 10) exactBonus = 100;
                narrowRange = true;
                break;
            case "extreme":
                rounds = Math.Min(15, 8 + ((playerCount + 1) / 2));
                rangeTop = Math.Max(2048, playerCount * playerCount * 256);
                startingPool = CPH.GetGlobalVar<int>("hl_extreme_starting_pool", false);
                if (startingPool < 1000) startingPool = 100000;
                exactBonus = CPH.GetGlobalVar<int>("hl_extreme_exact_bonus", false);
                if (exactBonus < 100) exactBonus = 10000;
                narrowRange = false;
                break;
            default:
                mode = "normal";
                rounds = CPH.GetGlobalVar<int>("hl_normal_rounds", false);
                if (rounds < 1) rounds = 10;
                rangeTop = CPH.GetGlobalVar<int>("hl_normal_range_top", false);
                if (rangeTop < 10) rangeTop = 100;
                startingPool = CPH.GetGlobalVar<int>("hl_normal_starting_pool", false);
                if (startingPool < 100) startingPool = 10000;
                exactBonus = CPH.GetGlobalVar<int>("hl_normal_exact_bonus", false);
                if (exactBonus < 10) exactBonus = 1000;
                narrowRange = false;
                break;
        }
    }

    private void StartGame(string modeArg)
    {
        string mode = modeArg;
        if (mode != "easy" && mode != "normal" && mode != "extreme")
            mode = "normal";

        if (CPH.GetGlobalVar<bool>("hl_game_active", false))
        {
            id736.Chat.SendMessage("A Higher or Lower game is already in progress!");
            return;
        }

        int joinTimer = CPH.GetGlobalVar<int>("hl_join_timer", false);
        if (joinTimer < 10) joinTimer = 60;

        id736.Groups.EnsureGroup(StreamerbotUserGroupName);
        CPH.SetGlobalVar("hl_game_active", true, false);
        CPH.SetGlobalVar("hl_mode", mode, false);
        CPH.SetGlobalVar("hl_phase", "join", false);
        CPH.SetGlobalVar("hl_participation", new Dictionary<string, int>(), false);
        CPH.SetGlobalVar("hl_exact_guessers", new List<string>(), false);
        CPH.SetGlobalVar("hl_player_names", new Dictionary<string, string>(), false);

        CPH.EnableAction("HL_PlayerJoin");
        EnableGuessCommand();

        string currencyName = GetCurrencyName();
        string guessMode = CPH.GetGlobalVar<string>("hl_guess_mode", false) ?? "first";

        if (mode == "extreme")
        {
            id736.Chat.SendMessage($"A Higher or Lower EXTREME game has started! Type !join to enter. You have {joinTimer} seconds! Number range and number of rounds will be determined by how many players join. iandouScream");
        }
        else
        {
            id736.Chat.SendMessage($"A Higher or Lower ({mode.ToUpper()} mode) game has started! Type !join to enter. You have {joinTimer} seconds!");
        }

        ObsSetText($"Higher or Lower ({mode} mode)!\n{joinTimer}s remaining to !join");
        ObsShow();

        id736.Chat.SendMessage($"Only your {guessMode} guess will be used. Everyone's numbers will be averaged together each round. The game will tell everyone playing to guess HIGHER or LOWER. If anyone guesses the EXACT number before the final round, they'll win an additional bonus!");

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
        CPH.DisableAction("HL_PlayerJoin");

        int joinedCount = GetPlayerCount();
        if (joinedCount == 0)
        {
            id736.Chat.SendMessage("No one joined! Game cancelled.");
            CleanupGame();
            return;
        }

        int rounds, rangeTop, startingPool, exactBonus, roundDelayMs;
        bool narrowRange;
        LoadModeConfig(mode, joinedCount, out rounds, out rangeTop, out startingPool, out exactBonus, out roundDelayMs, out narrowRange);

        CPH.SetGlobalVar("hl_mode_rounds", rounds, false);
        CPH.SetGlobalVar("hl_mode_range_top", rangeTop, false);
        CPH.SetGlobalVar("hl_mode_starting_pool", startingPool, false);
        CPH.SetGlobalVar("hl_mode_exact_bonus", exactBonus, false);
        CPH.SetGlobalVar("hl_mode_round_delay_ms", roundDelayMs, false);
        CPH.SetGlobalVar("hl_mode_narrow_range", narrowRange, false);
        CPH.SetGlobalVar("hl_max_rounds", rounds, false);

        int finalPool = (int)Math.Round(startingPool * 0.05m);
        int decayStep = rounds > 1
            ? (int)Math.Round((startingPool - finalPool) / (decimal)(rounds - 1))
            : 0;
        CPH.SetGlobalVar("hl_decay_step", decayStep, false);
        CPH.SetGlobalVar("hl_final_pool", finalPool, false);

        CPH.SetGlobalVar("hl_current_min", 1, false);
        CPH.SetGlobalVar("hl_current_max", rangeTop, false);

        int targetNumber = CPH.Between(1, rangeTop);
        CPH.SetGlobalVar("hl_target_number", targetNumber, false);
        CPH.SetGlobalVar("hl_winnable_points", startingPool, false);
        CPH.SetGlobalVar("hl_number_guessed", false, false);
        CPH.SetGlobalVar("hl_round", 1, false);

        id736.Chat.SendMessage($"Game on! {joinedCount} player(s) are playing ({mode} mode). The number is between 1 and {rangeTop}. {rounds} rounds. Prize pool: {startingPool} {currencyName}. Starting round 1 in 15 seconds...");

        string joinplural = joinedCount == 1 ? "" : "s";
        ObsSetText($"Game on! {joinedCount} player{joinplural}!\n{mode} mode - 1 to {rangeTop}\nStarting in 15s...");
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
        int maxRounds = CPH.GetGlobalVar<int>("hl_mode_rounds", false);
        if (maxRounds < 1) maxRounds = 10;
        bool numberGuessed = false;

        int guessTimer = CPH.GetGlobalVar<int>("hl_guess_timer", false);
        if (guessTimer < 5) guessTimer = 30;

        int rangeTop = CPH.GetGlobalVar<int>("hl_mode_range_top", false);
        if (rangeTop < 10) rangeTop = 100;

        string mode = CPH.GetGlobalVar<string>("hl_mode", false) ?? "normal";
        bool narrowRange = CPH.GetGlobalVar<bool>("hl_mode_narrow_range", false);
        int roundDelayMs = CPH.GetGlobalVar<int>("hl_mode_round_delay_ms", false);
        if (roundDelayMs < 1000) roundDelayMs = 5000;

        while (round <= maxRounds && !numberGuessed)
        {
            if (!CPH.GetGlobalVar<bool>("hl_game_active", false))
                return;

            CPH.SetGlobalVar("hl_phase", "guess", false);
            CPH.SetGlobalVar("hl_round", round, false);
            CPH.SetGlobalVar($"hl_round_{round}_guessers", new List<string>(), false);
            CPH.SetGlobalVar($"hl_round_{round}_guesses", new List<int>(), false);

            int winnable = CPH.GetGlobalVar<int>("hl_winnable_points", false);

            int displayMin, displayMax;
            if (narrowRange)
            {
                displayMin = CPH.GetGlobalVar<int>("hl_current_min", false);
                displayMax = CPH.GetGlobalVar<int>("hl_current_max", false);
            }
            else
            {
                displayMin = 1;
                displayMax = rangeTop;
            }

            id736.Chat.SendMessage($"Round {round}/{maxRounds} - Type a number between {displayMin} and {displayMax}! You have {guessTimer} seconds. Prize pool: {winnable} {GetCurrencyName()}");

            ObsSetText($"Round {round}/{maxRounds} - Guess {displayMin}-{displayMax}!\nPrize: {winnable} {GetCurrencyName()}");
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
                id736.Chat.SendMessage($"Next round starting in {roundDelayMs / 1000} seconds...");
                CPH.Wait(roundDelayMs);
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

        int bonus = CPH.GetGlobalVar<int>("hl_mode_exact_bonus", false);
        if (bonus < 0) bonus = 0;

        Dictionary<string, int> awards = new Dictionary<string, int>();
        int totalAwarded = 0;
        int participantCount = 0;

        foreach (string userKey in allUsers)
        {
            int roundsPlayed = participation.ContainsKey(userKey) ? participation[userKey] : 0;
            double pct = totalRounds > 0 ? (double)roundsPlayed / totalRounds : 0;
            int points = (int)Math.Round(finalWinnable * pct);
            if (points > 0)
            {
                awards[userKey] = points;
                totalAwarded += points;
            }
            if (roundsPlayed > 0)
                participantCount++;
        }

        if (exactGuessers.Count > 0)
        {
            foreach (string key in exactGuessers)
            {
                if (awards.ContainsKey(key))
                    awards[key] += bonus;
                else
                    awards[key] = bonus;
                totalAwarded += bonus;
            }
        }

        foreach (var kvp in awards)
        {
            ParseUserKey(kvp.Key, out Platform platform, out string userId);
            string displayName = GetUserDisplayName(kvp.Key);
            AwardPoints(displayName, platform, kvp.Value);
        }

        int grandTotal = totalAwarded;

        var sortedAwards = awards.OrderByDescending(kvp => kvp.Value).Take(10).ToList();
        var line = new StringBuilder("Points awarded: ");
        bool firstEntry = true;
        foreach (var award in sortedAwards)
        {
            string entry = $"{GetUserDisplayName(award.Key)} {award.Value} pts";
            string candidate = firstEntry ? entry : ", " + entry;
            if (Encoding.UTF8.GetByteCount(line.ToString() + candidate) > 400)
            {
                id736.Chat.SendMessage(line.ToString());
                line = new StringBuilder("Points awarded: ");
                candidate = entry;
                firstEntry = true;
            }
            if (!firstEntry)
                line.Append(", ");
            line.Append(entry);
            firstEntry = false;
        }
        if (line.Length > "Points awarded: ".Length)
            id736.Chat.SendMessage(line.ToString());

        id736.Chat.SendMessage($"In total, we gave out {grandTotal} {currencyName} to everyone who played");

        CleanupGame();
    }

    private void ProcessRoundResults(int round, ref bool numberGuessed)
    {
        int decayStep = CPH.GetGlobalVar<int>("hl_decay_step", false);
        if (decayStep < 0) decayStep = 100;

        var guesses = CPH.GetGlobalVar<List<int>>($"hl_round_{round}_guesses", false) ?? new List<int>();

        if (guesses.Count == 0)
        {
            int winnable = CPH.GetGlobalVar<int>("hl_winnable_points", false) - decayStep;
            int finalPool = CPH.GetGlobalVar<int>("hl_final_pool", false);
            if (winnable < finalPool) winnable = finalPool;
            CPH.SetGlobalVar("hl_winnable_points", winnable, false);

            id736.Chat.SendMessage($"No one submitted a guess this round! Prize pool reduced to {winnable} {GetCurrencyName()}.");
            return;
        }

        int avg = (int)Math.Round(guesses.Average());
        int target = CPH.GetGlobalVar<int>("hl_target_number", false);

        var roundGuessers = CPH.GetGlobalVar<List<string>>($"hl_round_{round}_guessers", false) ?? new List<string>();

        UpdateParticipation(roundGuessers);

        string mode = CPH.GetGlobalVar<string>("hl_mode", false) ?? "normal";
        bool narrowRange = CPH.GetGlobalVar<bool>("hl_mode_narrow_range", false);

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
            int winnable = CPH.GetGlobalVar<int>("hl_winnable_points", false) - decayStep;
            int finalPool = CPH.GetGlobalVar<int>("hl_final_pool", false);
            if (winnable < finalPool) winnable = finalPool;
            CPH.SetGlobalVar("hl_winnable_points", winnable, false);

            if (narrowRange)
            {
                int currentMin = CPH.GetGlobalVar<int>("hl_current_min", false);
                int currentMax = CPH.GetGlobalVar<int>("hl_current_max", false);

                if (avg < target)
                    currentMin = Math.Max(currentMin, avg + 1);
                else
                    currentMax = Math.Min(currentMax, avg - 1);

                CPH.SetGlobalVar("hl_current_min", currentMin, false);
                CPH.SetGlobalVar("hl_current_max", currentMax, false);

                string hint = avg < target ? "guess HIGHER" : "guess LOWER";
                id736.Chat.SendMessage($"Average guess was {avg}. {hint}! New range: {currentMin}-{currentMax}. Prize pool: {winnable} {GetCurrencyName()}.");
            }
            else
            {
                string hint = avg < target ? "guess HIGHER" : "guess LOWER";
                id736.Chat.SendMessage($"Average guess was {avg}. {hint}! Prize pool reduced to {winnable} {GetCurrencyName()}.");
            }
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
        CPH.SetGlobalVar("hl_participation", participation, false);
    }

    private Dictionary<string, int> GetParticipation()
    {
        return CPH.GetGlobalVar<Dictionary<string, int>>("hl_participation", false) ?? new Dictionary<string, int>();
    }

    private List<string> GetExactGuessers()
    {
        return CPH.GetGlobalVar<List<string>>("hl_exact_guessers", false) ?? new List<string>();
    }

    private void AwardPoints(string displayName, Platform platform, int points)
    {
        if (string.IsNullOrEmpty(displayName) || points <= 0)
            return;

        string currencyName = CPH.GetGlobalVar<string>("hl_currency_name", false);
        if (string.IsNullOrEmpty(currencyName))
            currencyName = "points";

        id736.Points.Add(displayName, platform, currencyName, points);
        id736.Log.Message($"Awarded {points} {currencyName} to {displayName} ({platform})", filenamePrefix: "higherlower");
    }

    private List<string> GetPlayers()
    {
        var playerNames = CPH.GetGlobalVar<Dictionary<string, string>>("hl_player_names", false) ?? new Dictionary<string, string>();
        var result = new List<string>();
        foreach (var key in playerNames.Keys)
        {
            if (!result.Contains(key))
                result.Add(key);
        }
        return result;
    }

    private int GetPlayerCount()
    {
        return id736.Groups.Count(StreamerbotUserGroupName);
    }

    private void CleanupGame()
    {
        DisableGuessCommand();
        CPH.DisableAction("HL_PlayerJoin");
        ObsHide();

        int maxRounds = CPH.GetGlobalVar<int>("hl_max_rounds", false);
        if (maxRounds < 1) maxRounds = 10;

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
        CPH.UnsetGlobalVar("hl_participation", false);
        CPH.UnsetGlobalVar("hl_exact_guessers", false);
        CPH.UnsetGlobalVar("hl_player_names", false);
        CPH.UnsetGlobalVar("hl_mode", false);
        CPH.UnsetGlobalVar("hl_mode_rounds", false);
        CPH.UnsetGlobalVar("hl_mode_range_top", false);
        CPH.UnsetGlobalVar("hl_mode_starting_pool", false);
        CPH.UnsetGlobalVar("hl_mode_exact_bonus", false);
        CPH.UnsetGlobalVar("hl_mode_round_delay_ms", false);
        CPH.UnsetGlobalVar("hl_mode_narrow_range", false);
        CPH.UnsetGlobalVar("hl_current_min", false);
        CPH.UnsetGlobalVar("hl_current_max", false);
        CPH.UnsetGlobalVar("hl_decay_step", false);
        CPH.UnsetGlobalVar("hl_final_pool", false);

        id736.Groups.Clear(StreamerbotUserGroupName);
        CPH.SetGlobalVar("hl_game_active", false, false);
    }

    private void EnableGuessCommand()
    {
        string commandId = GetGuessCommandId();
        if (!string.IsNullOrEmpty(commandId))
            CPH.EnableCommand(commandId);
    }

    private void DisableGuessCommand()
    {
        string commandId = GetGuessCommandId();
        if (!string.IsNullOrEmpty(commandId))
            CPH.DisableCommand(commandId);
    }

    private string GetGuessCommandId()
    {
        string commandId = CPH.GetGlobalVar<string>("hl_guess_command_id", false) ?? "";
        if (!string.IsNullOrWhiteSpace(commandId))
            return commandId;

        var commands = CPH.GetCommands();
        if (commands == null)
            return "";

        foreach (var cmd in commands)
        {
            if (cmd == null)
                continue;

            var type = cmd.GetType();
            string name = type.GetProperty("Name")?.GetValue(cmd, null)?.ToString() ?? "";
            if (name != "higher-lower number guess")
                continue;

            commandId = type.GetProperty("Id")?.GetValue(cmd, null)?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(commandId))
            {
                CPH.SetGlobalVar("hl_guess_command_id", commandId, false);
                return commandId;
            }
        }

        return "";
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