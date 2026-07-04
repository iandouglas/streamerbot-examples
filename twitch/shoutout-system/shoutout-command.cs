using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("rawInput", out string target))
            return false;

        target = target.Trim().TrimStart('@');
        if (string.IsNullOrWhiteSpace(target))
            return false;

        List<string> queue = CPH.GetGlobalVar<List<string>>("shoutoutQueue", true);
        if (queue == null) {
            queue = new();
        }

        if (queue.Contains(target))
            return true;

        queue.Add(target);

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastShoutoutEpoch = CPH.GetGlobalVar<long>("lastShoutoutEpoch", true);
        if (lastShoutoutEpoch != null && (now - lastShoutoutEpoch < 120)) {
            string msgId = "";
	    	CPH.TryGetArg("msgId", out msgId);
            CPH.TwitchReplyToMessage("shoutout queued, thanks!", msgId, true);
        }

        CPH.SetGlobalVar("shoutoutQueue", queue, true);
        CPH.SetTimerInterval("shoutout timer", 2);
        CPH.EnableTimer("shoutout timer");
        return true;
    }
}