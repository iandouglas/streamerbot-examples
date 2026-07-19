using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        long now = id736.Time.NowEpoch();
        long lastShoutoutEpoch = CPH.GetGlobalVar<long>("lastShoutoutEpoch", true);

        List<string> queue = CPH.GetGlobalVar<List<string>>("shoutoutQueue", true);
        if (queue == null)
        {
            queue = new List<string>();
        }

        if (queue.Count == 0)
        {
            if (!CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId))
                return false;

            string timerGuidString = currentTimerId.ToString();

            if (now - lastShoutoutEpoch > 120)
            {
                // Queue is empty and cooldown has passed; reset timer to 1s so next shoutout fires immediately.
                id736.Timers.ResetTimerById(timerGuidString, 1);
            }
            else
            {
                // Cooldown still active; set timer to the remaining time until the next allowed shoutout,
                // then enable the ad watch logic action.
                int when = (int)(lastShoutoutEpoch + 120 - now);
                if (when < 1) when = 1;
                id736.Timers.ResetTimerById(timerGuidString, when);
                CPH.EnableAction("ad watch logic");
            }

            return false;
        }

        string nextUsername = queue[0];
        queue.RemoveAt(0);
        CPH.SetGlobalVar("shoutoutQueue", queue, true);
        CPH.SetGlobalVar("nextShoutoutUser", nextUsername, false);

        now = id736.Time.NowEpoch();
        CPH.SetGlobalVar("lastShoutoutEpoch", now);

        if (queue.Count == 0)
        {
            // After this shoutout the queue is empty; reset the timer to 1s so future queue-and-shoutouts happen quickly.
            id736.Timers.ResetTimerById("shoutout timer", 1, false);
            CPH.DisableAction("!so (timer)");
        }

        return true;
    }
}
