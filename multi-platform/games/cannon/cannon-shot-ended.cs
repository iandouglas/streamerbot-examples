using System;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Timers.SetContext(CPH);

        if (!CPH.TryGetArg("userName", out string userName) || string.IsNullOrWhiteSpace(userName))
            userName = "Player";

        if (!CPH.TryGetArg("platform", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        int score = -1;
        if (CPH.TryGetArg("score", out int scoreInt))
        {
            score = scoreInt;
        }
        else if (CPH.TryGetArg("score", out string scoreStr) && !string.IsNullOrWhiteSpace(scoreStr))
        {
            int.TryParse(scoreStr, out score);
        }

        // Only announce scores for shots that land on the target. Misses are silent.
        if (score >= 0)
        {
            id736.Chat.SendMessage($"🎯 {userName} scored {score} points!");

            try
            {
                id736.Points.SetContext(CPH);
                id736.Points.Add(userName, platform, "points", score);
            }
            catch (Exception)
            {
                // Ignore points errors; the chat message already went out.
            }
        }

        // Mark the shot as complete so the next queued player can fire.
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_firing_started", 0L, false);

        // Mark activity and slow the timer to the hide-delay interval.
        CPH.SetGlobalVar("cannon_last_active", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        SlowGameTimerIfQueueEmpty();

        return true;
    }

    private void SlowGameTimerIfQueueEmpty()
    {
        string queueJson = CPH.GetGlobalVar<string>("cannon_queue", false) ?? "[]";
        bool queueEmpty = string.IsNullOrWhiteSpace(queueJson) || queueJson == "[]";
        if (!queueEmpty)
            return;

        string timerGuid = CPH.GetGlobalVar<string>("cannon_timer_guid", false) ?? "";

        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, 60, keepEnabled: true);
        }
    }
}
