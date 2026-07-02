using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NLayer;
using Duration.Mine.Mp4;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("userName", out string userName) || string.IsNullOrWhiteSpace(userName))
            return false;

        if (!CPH.TryGetArg("user", out string displayName) || string.IsNullOrWhiteSpace(displayName))
            displayName = userName;

        bool doVideo = false ;

        CPH.TryGetArg<string>("videoFileWin", out string videoFileWin);
        CPH.TryGetArg<string>("videoFileLose", out string videoFileLose);
        CPH.TryGetArg<string>("OBSScene", out string OBSScene);
        CPH.TryGetArg<string>("OBSSource", out string OBSSource);
        if (videoFileWin != null && videoFileLose != null && OBSScene != null && OBSSource != null)
            doVideo = true;

        int cooldownSeconds = CPH.GetGlobalVar<int?>("highGroundCooldownSeconds", true) ?? 10;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long lastClaimEpoch = CPH.GetGlobalVar<long?>("highGroundLastClaimEpoch", true) ?? 0;
        string currentHolder = CPH.GetGlobalVar<string>("highGroundCurrentHolder", true) ?? string.Empty;

        string countsJson = CPH.GetGlobalVar<string>("highGroundClaimCounts", true);
        var counts = string.IsNullOrWhiteSpace(countsJson)
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : JsonConvert.DeserializeObject<Dictionary<string, int>>(countsJson)
              ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        bool cooldownActive = lastClaimEpoch > 0 && now < (lastClaimEpoch + cooldownSeconds);
        if (cooldownActive)
        {
            string holderText = string.IsNullOrWhiteSpace(currentHolder) ? "someone else" : currentHolder;
            CPH.SendMessage($"Sorry {displayName}, {holderText} has the high ground -- but YOU WERE THE CHOSEN ONE!", true);
            playVideo(videoFileLose, OBSScene, OBSSource);
            return true;
        }

        bool ownershipChanged = !string.Equals(currentHolder, userName, StringComparison.OrdinalIgnoreCase);

        if (!counts.ContainsKey(userName))
            counts[userName] = 0;

        if (ownershipChanged)
            counts[userName]++;

        CPH.SetGlobalVar("highGroundCurrentHolder", userName, true);
        CPH.SetGlobalVar("highGroundLastClaimEpoch", now, true);
        CPH.SetGlobalVar("highGroundClaimCounts", JsonConvert.SerializeObject(counts), true);

        int holdCount = counts[userName];
        CPH.SetGlobalVar("highGroundLatestWinner", userName, false);
        CPH.SetGlobalVar("highGroundLatestWinnerDisplay", displayName, false);
        CPH.SetGlobalVar("highGroundLatestHoldCount", holdCount, false);
        CPH.SetGlobalVar("highGroundOwnershipChanged", ownershipChanged, false);

        CPH.SendMessage($"{displayName} has the high ground! They've held the high ground {holdCount} times!", true);
        playVideo(videoFileWin, OBSScene, OBSSource);

        return true;
    }

    public void playVideo(string filename, string OBSScene, string OBSSource) {
        var media_duration = Convert.ToInt32(Mp4Duration.GetMp4Duration(filename));

        CPH.ObsSetMediaSourceFile(OBSScene, OBSSource, filename);
        CPH.ObsSetSourceVisibility(OBSScene, OBSSource, true);

        CPH.Wait(media_duration * 1000);
        CPH.ObsSetSourceVisibility(OBSScene, OBSSource, false);
        CPH.Wait(100);
    }
}
