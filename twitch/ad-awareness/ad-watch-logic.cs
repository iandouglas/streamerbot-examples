using System;
using System.Threading.Tasks;
using id736 = iandouglas736;

public class CPHInline
{
    public void ResetTimer(string timerName, int durationInSeconds, bool keepEnabled = true)
    {
        CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId);
        string timerGuidString = currentTimerId.ToString();
        Task.Run(async () =>
        {
            await Task.Delay(200);
            CPH.SetTimerInterval(timerGuidString, durationInSeconds);
            CPH.DisableTimerById(timerGuidString);
            if (keepEnabled)
                CPH.EnableTimerById(timerGuidString);
        });
    }

    public bool Execute()
    {
        id736.Chat.SetContext(CPH);

        string adTimerName = "ad awareness timer";

        long adStartEpoch = CPH.GetGlobalVar<long>("adsStartEpoch", false);
        long adFinishEpoch = CPH.GetGlobalVar<long>("adsFinishEpoch", false);
        int adsDurationInSeconds = CPH.GetGlobalVar<int>("adsDurationInSeconds", false);
        long nowEpoch = id736.Time.NowEpoch();
        CPH.SetGlobalVar("nowEpoch", nowEpoch, false);

        if (nowEpoch < adStartEpoch)
        {
            ResetTimer(adTimerName, (int)(adStartEpoch - nowEpoch));
        }
        else if (nowEpoch >= adStartEpoch && nowEpoch < adFinishEpoch)
        {
            id736.Chat.SendMessage($"Ads have started, see you in {adsDurationInSeconds} seconds");
            ResetTimer(adTimerName, adsDurationInSeconds);
        }
        else if (nowEpoch >= adFinishEpoch)
        {
            id736.Chat.SendMessage($"Ads are over KAPOW Welcome back to the stream!");
            ResetTimer(adTimerName, 1, false);
            CPH.DisableAction("ad watch logic");
            CPH.UnsetGlobalVar("adStartAtEpoch", false);
            CPH.UnsetGlobalVar("adFinishedAtEpoch", false);
            CPH.UnsetGlobalVar("adDurationInSeconds", false);
        }

        return true;
    }
}
