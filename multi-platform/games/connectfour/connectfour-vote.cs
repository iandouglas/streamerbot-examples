using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "connectfour");
    }

    private string MakeUserKey(string platform, string userId)
    {
        return $"{platform.ToLowerInvariant()}:{userId}";
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("user", out string user) || string.IsNullOrWhiteSpace(user))
            return false;
        if (!CPH.TryGetArg("userName", out string userName))
            userName = user;
        if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
            userId = userName;
        if (!CPH.TryGetArg("userType", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        object msgIdObj;
        string msgId = CPH.TryGetArg("msgId", out msgIdObj) ? msgIdObj?.ToString() : null;

        bool gameActive = CPH.GetGlobalVar<bool>("connectfour_game_active", false);
        if (!gameActive)
        {
            id736.Chat.SendReplyOrMessage("No Connect Four game is currently active.", msgId);
            return false;
        }

        string phase = CPH.GetGlobalVar<string>("connectfour_phase", false) ?? "";
        if (phase != "voting" && phase != "tiebreak")
        {
            id736.Chat.SendReplyOrMessage("Voting is not open right now.", msgId);
            return false;
        }

        int currentPlayer = CPH.GetGlobalVar<int>("connectfour_current_player", false);
        if (currentPlayer != 1)
        {
            id736.Chat.SendReplyOrMessage("It's not the chat's turn right now.", msgId);
            return false;
        }

        // The vote command uses a regex like ^\d$ so rawInput is just the digit.
        if (!CPH.TryGetArg("rawInput", out string rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            return false;
        }

        string colStr = rawInput.Trim();

        if (!int.TryParse(colStr, out int column) || column < 1)
        {
            return false;
        }

        int cols = CPH.GetGlobalVar<int>("connectfour_cols", false);
        if (cols < 1) cols = 7;

        int minCol = 1;
        int maxCol = cols;
        List<int> finalists = null;

        if (phase == "tiebreak")
        {
            finalists = id736.Data.FromJson<List<int>>(
                CPH.GetGlobalVar<string>("connectfour_tiebreak_finalists", false) ?? "[]")
                ?? new List<int>();
            if (finalists.Count > 0)
            {
                minCol = finalists.Min() + 1;
                maxCol = finalists.Max() + 1;
            }
        }

        bool outOfRange = column < minCol || column > maxCol;
        if (phase == "tiebreak" && finalists != null && finalists.Count > 0)
        {
            // Finalists are zero-based; user input is 1-based.
            if (!finalists.Contains(column - 1))
                outOfRange = true;
        }

        if (outOfRange)
        {
            return false;
        }

        string userKey = MakeUserKey(platform, userId);
        string groupName = "connectfour_players";

        if (!id736.Groups.IsInGroup(userName, groupName))
        {
            return false;
        }

        int zeroBasedCol = column - 1;
        var votes = id736.Data.FromJson<Dictionary<string, int>>(
            CPH.GetGlobalVar<string>("connectfour_voting_results", false) ?? "{}")
            ?? new Dictionary<string, int>();

        votes[userKey] = zeroBasedCol;
        CPH.SetGlobalVar("connectfour_voting_results", id736.Data.ToJson(votes), false);

        Log($"vote: {user} voted column {column} (phase={phase})");

        return true;
    }
}