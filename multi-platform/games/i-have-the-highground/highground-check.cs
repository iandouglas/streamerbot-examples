using System;
using System.Collections.Generic;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        string currentHolder = CPH.GetGlobalVar<string>("highGroundCurrentHolder", true) ?? string.Empty;
        long lastClaimEpoch = CPH.GetGlobalVar<long?>("highGroundLastClaimEpoch", true) ?? 0;
        int cooldownSeconds = CPH.GetGlobalVar<int?>("highGroundCooldownSeconds", true) ?? 120;

        string countsJson = CPH.GetGlobalVar<string>("highGroundClaimCounts", true);
        var counts = id736.Data.FromJson<Dictionary<string, int>>(countsJson)
            ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(currentHolder))
        {
            id736.Chat.SendMessage("Nobody has claimed the high ground yet.");
            return true;
        }

        int holdCount = counts.ContainsKey(currentHolder) ? counts[currentHolder] : 0;

        long now = id736.Time.NowEpoch();
        long nextClaimEpoch = lastClaimEpoch + cooldownSeconds;
        long remainingSeconds = nextClaimEpoch > now ? nextClaimEpoch - now : 0;

        string cooldownText = remainingSeconds > 0
            ? $" Next claim opens in {FormatDuration(remainingSeconds)}."
            : " The high ground is up for grabs right now!";

        id736.Chat.SendMessage($"{currentHolder} has the high ground and has held it {holdCount} time{(holdCount == 1 ? "" : "s")}.{cooldownText}");
        return true;
    }

    private string FormatDuration(long totalSeconds)
    {
        if (totalSeconds < 60)
            return $"{totalSeconds} second{(totalSeconds == 1 ? "" : "s")}";

        long minutes = totalSeconds / 60;
        long seconds = totalSeconds % 60;

        if (seconds == 0)
            return $"{minutes} minute{(minutes == 1 ? "" : "s")}";

        return $"{minutes} minute{(minutes == 1 ? "" : "s")} {seconds} second{(seconds == 1 ? "" : "s")}";
    }
}
