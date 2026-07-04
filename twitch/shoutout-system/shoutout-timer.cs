using System;
using System.Collections.Generic;
// using Newtonsoft.Json;
using System.Threading.Tasks;

public class CPHInline
{
    public bool Execute()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastShoutoutEpoch = CPH.GetGlobalVar<long>("lastShoutoutEpoch", true);

        if (lastShoutoutEpoch != null && (now - lastShoutoutEpoch < 120)) {
            int when = (int)(now-lastShoutoutEpoch+120);
            CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId);
            string timerGuidString = currentTimerId.ToString();
            Task.Run(async () =>
            {
                await Task.Delay(200);
                CPH.SetTimerInterval(timerGuidString, when);
                CPH.DisableTimerById(timerGuidString);
                CPH.EnableTimerById(timerGuidString);
            });

            return false; // too soon
        }

        List<string> queue = CPH.GetGlobalVar<List<string>>("shoutoutQueue", true);
        if (queue == null)
        {
            queue = new();
        }

        if (queue.Count == 0)
        {
            CPH.DisableTimer("shoutout timer");
            return false;
        }

        string nextUsername = queue[0];
        queue.RemoveAt(0);
        CPH.SetGlobalVar("shoutoutQueue", queue, true);
        CPH.SetGlobalVar("nextShoutoutUser", nextUsername, false);
        if (queue.Count == 0)
        {
            CPH.SetTimerInterval("shoutout timer", 1); // reset timer for next time
            CPH.DisableTimer("shoutout timer"); // shut off the timer
            CPH.DisableAction("!so (timer)"); // disable myself
        }
        CPH.SetTimerInterval("shoutout timer", 120);

        now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CPH.SetGlobalVar("lastShoutoutEpoch", now);

        return true;
    }
}