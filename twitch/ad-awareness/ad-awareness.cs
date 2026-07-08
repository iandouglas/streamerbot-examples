using System;
using System.Globalization;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("minutes", out int minutes))
            return false;
        CPH.TryGetArg("adLength", out long adLengthSeconds);
        TryGetUtcDateTimeArg("nextAdAt", out DateTime nextAdAtUtc);

        long nowEpoch = id736.Time.NowEpoch();
        long adStartAtEpoch = new DateTimeOffset(nextAdAtUtc).ToUnixTimeSeconds();
        long adFinishedAtEpoch = new DateTimeOffset(nextAdAtUtc.AddSeconds(adLengthSeconds)).ToUnixTimeSeconds();

        CPH.SetGlobalVar("adsStartEpoch", adStartAtEpoch, false);
        CPH.SetGlobalVar("adsDurationInSeconds", adLengthSeconds, false);
        CPH.SetGlobalVar("adsFinishEpoch", adFinishedAtEpoch, false);

        string adTimerName = "ad awareness timer";
        CPH.EnableTimer(adTimerName);
        CPH.EnableAction("ad watch logic");

        return true;
    }

    private bool TryGetUtcDateTimeArg(string argName, out DateTime valueUtc)
    {
        valueUtc = DateTime.MinValue;
        if (CPH.TryGetArg(argName, out DateTime directValue))
        {
            valueUtc = directValue.Kind == DateTimeKind.Utc ? directValue : directValue.ToUniversalTime();
            return true;
        }

        if (!CPH.TryGetArg(argName, out string rawValue) || string.IsNullOrWhiteSpace(rawValue))
            return false;
        return DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out valueUtc);
    }
}
