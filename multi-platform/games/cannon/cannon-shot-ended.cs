using System;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Timers.SetContext(CPH);
        CPH.LogDebug("[cannon-ended] Started.");

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

        CPH.LogDebug($"[cannon-ended] userName={userName}, platform={platform}, score={score}");

        // Score is -1 for a miss, 0-100 for a hit.
        if (score >= 0)
        {
            try
            {
                id736.Points.SetContext(CPH);
                int total = id736.Points.Add(userName, platform, "cannon_points", score);
                id736.Chat.SendMessage($"🎯 {userName} scored {score} points! Total: {total}");
                CPH.LogDebug($"[cannon-ended] Chat message sent for score {score}, total {total}.");
            }
            catch (Exception ex)
            {
                id736.Chat.SendMessage($"🎯 {userName} scored {score} points!");
                CPH.LogDebug($"[cannon-ended] Points add failed ({ex.Message}); sent score-only chat message.");
            }
        }
        else
        {
            id736.Chat.SendMessage($"💨 {userName} missed the target!");
            CPH.LogDebug("[cannon-ended] Chat message sent for miss.");
        }

        // Mark the shot as complete so the next queued player can fire.
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_firing_started", 0L, false);
        CPH.LogDebug("[cannon-ended] cannon_firing reset to false.");

        // Mark activity and slow the timer to the hide-delay interval.
        CPH.SetGlobalVar("cannon_last_active", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        CPH.LogDebug("[cannon-ended] Activity timestamp reset.");
        SlowGameTimerIfQueueEmpty();

        CPH.LogDebug("[cannon-ended] Finished.");
        return true;
    }

    private void SlowGameTimerIfQueueEmpty()
    {
        string queueJson = CPH.GetGlobalVar<string>("cannon_queue", false) ?? "[]";
        bool queueEmpty = string.IsNullOrWhiteSpace(queueJson) || queueJson == "[]";
        CPH.LogDebug($"[cannon-ended] Queue empty={queueEmpty}, queueJson={queueJson}");
        if (!queueEmpty)
            return;

        string timerGuid = CPH.GetGlobalVar<string>("cannon_timer_guid", false) ?? "";
        CPH.LogDebug($"[cannon-ended] SlowGameTimerIfQueueEmpty guid={timerGuid}");

        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, 60, keepEnabled: true);
            CPH.LogDebug("[cannon-ended] Timer slowed to 60s hide delay by GUID.");
        }
    }
}
