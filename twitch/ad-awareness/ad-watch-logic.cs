using System;
using System.Threading.Tasks;

public class CPHInline
{
    int randId;
    public void ChatLog(string message, bool fromBot=true)
    {
        // CPH.SendMessage($"{DateTime.Now.ToString("HH:mm:ss")} ({randId}) {message}", fromBot);
        CPH.SendMessage($"{message}", fromBot);
    }

    public void ResetTimer(string timerName, int durationInSeconds, bool keepEnabled=true)
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
		Random rnd = new Random();
		randId = rnd.Next(1000);
        // ChatLog($"ad watch logic");
        string adTimerName = "ad awareness timer";

        long adStartEpoch = CPH.GetGlobalVar<long>("adsStartEpoch", false);
        long adFinishEpoch = CPH.GetGlobalVar<long>("adsFinishEpoch", false);
        int adsDurationInSeconds = CPH.GetGlobalVar<int>("adsDurationInSeconds", false);
        long nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CPH.SetGlobalVar("nowEpoch", nowEpoch, false);

        if (nowEpoch < adStartEpoch) {
            ResetTimer(adTimerName, (int)(adStartEpoch-nowEpoch));
        }
        else if (nowEpoch >= adStartEpoch && nowEpoch < adFinishEpoch) {
            ChatLog($"Ads have started, see you in {adsDurationInSeconds} seconds");
            ResetTimer(adTimerName, adsDurationInSeconds);
        }
        else if (nowEpoch >= adFinishEpoch) {
            ChatLog($"Ads are over KAPOW Welcome back to the stream!");
            ResetTimer(adTimerName, 1, false);
            CPH.DisableAction("ad watch logic");
            CPH.UnsetGlobalVar("adStartAtEpoch", false);
            CPH.UnsetGlobalVar("adFinishedAtEpoch", false);
            CPH.UnsetGlobalVar("adDurationInSeconds", false);
        } else {
            // ChatLog("not sure how we got here?");
        }

        // ChatLog($"ad watch logic done");
        return true;
    }
}
