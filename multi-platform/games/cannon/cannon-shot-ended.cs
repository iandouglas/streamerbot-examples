using System;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("userName", out string userName) || string.IsNullOrWhiteSpace(userName))
            userName = "Player";

        if (!CPH.TryGetArg("platform", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        int currentShotId = CPH.GetGlobalVar<int>("cannon_current_shot_id", false);
        int shotId = 0;
        if (CPH.TryGetArg("shotId", out int shotIdInt))
        {
            shotId = shotIdInt;
        }
        else if (CPH.TryGetArg("shotId", out string shotIdStr) && !string.IsNullOrWhiteSpace(shotIdStr))
        {
            int.TryParse(shotIdStr, out shotId);
        }

        if (currentShotId <= 0 || shotId <= 0 || shotId != currentShotId)
            return true;

        int score = -1;
        if (CPH.TryGetArg("score", out int scoreInt))
        {
            score = scoreInt;
        }
        else if (CPH.TryGetArg("score", out string scoreStr) && !string.IsNullOrWhiteSpace(scoreStr))
        {
            int.TryParse(scoreStr, out score);
        }

        // Announce the result in chat and award points only for hits.
        if (score >= 0)
        {
            id736.Chat.SendMessageToAllPlatforms($"{userName}@{platform} was fired from the cannon and scored {score} points for landing on the target!");

            try
            {
                id736.Points.Add(userName, platform, "points", score);
            }
            catch (Exception)
            {
                // Ignore points errors; the chat message already went out.
            }
        }
        else
        {
            id736.Chat.SendMessageToAllPlatforms($"{userName}@{platform} was fired from the cannon and scored 0 points for missing completely.");
        }

        // Mark the shot as complete so the next queued player can fire.
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_firing_started", 0L, false);
        CPH.SetGlobalVar("cannon_current_shot_id", 0, false);
        CPH.SetGlobalVar("cannon_next_fire_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3000, false);

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
