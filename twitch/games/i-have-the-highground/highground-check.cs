using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        string currentHolder = CPH.GetGlobalVar<string>("highGroundCurrentHolder", true) ?? string.Empty;
        long lastClaimEpoch = CPH.GetGlobalVar<long?>("highGroundLastClaimEpoch", true) ?? 0;
        int cooldownSeconds = CPH.GetGlobalVar<int?>("highGroundCooldownSeconds", true) ?? 120;

        string countsJson = CPH.GetGlobalVar<string>("highGroundClaimCounts", true);
        var counts = string.IsNullOrWhiteSpace(countsJson)
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : JsonConvert.DeserializeObject<Dictionary<string, int>>(countsJson)
              ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(currentHolder))
        {
            CPH.SendMessage("Nobody has claimed the high ground yet.", true);
            return true;
        }

        int holdCount = counts.ContainsKey(currentHolder) ? counts[currentHolder] : 0;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long nextClaimEpoch = lastClaimEpoch + cooldownSeconds;
        long remainingSeconds = nextClaimEpoch > now ? nextClaimEpoch - now : 0;

        string cooldownText = remainingSeconds > 0
            ? $" Next claim opens in {FormatDuration(remainingSeconds)}."
            : " The high ground is up for grabs right now!";

        CPH.SendMessage($"{currentHolder} has the high ground and has held it {holdCount} time{(holdCount == 1 ? "" : "s")}.{cooldownText}", true);
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
