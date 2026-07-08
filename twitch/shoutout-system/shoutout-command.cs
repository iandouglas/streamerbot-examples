using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Chat.SetContext(CPH);

        if (!CPH.TryGetArg("rawInput", out string target))
            return false;

        target = target.Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(target))
            return false;

        List<string> queue = CPH.GetGlobalVar<List<string>>("shoutoutQueue", true);
        if (queue == null)
            queue = new List<string>();

        if (queue.Contains(target))
            return true;

        queue.Add(target);

        long now = id736.Time.NowEpoch();
        long lastShoutoutEpoch = CPH.GetGlobalVar<long>("lastShoutoutEpoch", true);
        if (lastShoutoutEpoch > 0 && (now - lastShoutoutEpoch < 120))
        {
            string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;
            id736.Chat.SendReplyOrMessage("shoutout queued, thanks!", msgId);
        }

        CPH.SetGlobalVar("shoutoutQueue", queue, true);
        CPH.EnableAction("!so (timer)");

        // Ensure the timer context is set, then start/reset the shoutout timer to 2 seconds.
        id736.Timers.SetContext(CPH);
        id736.Timers.ResetTimerById("shoutout timer", 2);
        return true;
    }
}
