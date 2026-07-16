using System;
using System.Collections.Generic;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        string userName = id736.Chat.GetCurrentUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            id736.Log.Message("could not determine username", filenamePrefix: "highground");
            return false;
        }

        string displayName = userName;
        CPH.TryGetArg("user", out displayName);
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = userName;

        bool doVideo = false;
        CPH.TryGetArg<string>("videoFileWin", out string videoFileWin);
        CPH.TryGetArg<string>("videoFileLose", out string videoFileLose);
        CPH.TryGetArg<string>("OBSScene", out string OBSScene);
        CPH.TryGetArg<string>("OBSSource", out string OBSSource);
        if (!string.IsNullOrWhiteSpace(videoFileWin) && !string.IsNullOrWhiteSpace(videoFileLose) &&
            !string.IsNullOrWhiteSpace(OBSScene) && !string.IsNullOrWhiteSpace(OBSSource))
            doVideo = true;

        int cooldownSeconds = CPH.GetGlobalVar<int?>("highGroundCooldownSeconds", true) ?? 120;

        long now = id736.Time.NowEpoch();
        long lastClaimEpoch = CPH.GetGlobalVar<long?>("highGroundLastClaimEpoch", true) ?? 0;
        string currentHolder = CPH.GetGlobalVar<string>("highGroundCurrentHolder", true) ?? string.Empty;

        string countsJson = CPH.GetGlobalVar<string>("highGroundClaimCounts", true);
        var counts = id736.Data.FromJson<Dictionary<string, int>>(countsJson)
            ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        bool cooldownActive = lastClaimEpoch > 0 && now < (lastClaimEpoch + cooldownSeconds);
        if (cooldownActive)
        {
            string holderText = string.IsNullOrWhiteSpace(currentHolder) ? "someone else" : currentHolder;
            id736.Chat.SendMessage($"Sorry {displayName}, {holderText} has the high ground -- but YOU WERE THE CHOSEN ONE!");
            if (doVideo) PlayVideo(videoFileLose, OBSScene, OBSSource);
            return true;
        }

        bool ownershipChanged = !string.Equals(currentHolder, userName, StringComparison.OrdinalIgnoreCase);

        if (!counts.ContainsKey(userName))
            counts[userName] = 0;

        if (ownershipChanged)
            counts[userName]++;

        CPH.SetGlobalVar("highGroundCurrentHolder", userName, true);
        CPH.SetGlobalVar("highGroundLastClaimEpoch", now, true);
        CPH.SetGlobalVar("highGroundClaimCounts", id736.Data.ToJson(counts), true);

        int holdCount = counts[userName];
        CPH.SetGlobalVar("highGroundLatestWinner", userName, false);
        CPH.SetGlobalVar("highGroundLatestWinnerDisplay", displayName, false);
        CPH.SetGlobalVar("highGroundLatestHoldCount", holdCount, false);
        CPH.SetGlobalVar("highGroundOwnershipChanged", ownershipChanged, false);

        id736.Chat.SendMessage($"{displayName} has the high ground! They've held the high ground {holdCount} times!");
        if (doVideo) PlayVideo(videoFileWin, OBSScene, OBSSource);

        return true;
    }

    public void PlayVideo(string filename, string OBSScene, string OBSSource)
    {
        double? seconds = id736.Media.LengthInSeconds(filename);
        if (!seconds.HasValue)
        {
            id736.Log.Message($"Could not determine duration for {filename}", filenamePrefix: "highground");
            return;
        }

        int media_duration = Convert.ToInt32(seconds.Value);

        CPH.ObsSetMediaSourceFile(OBSScene, OBSSource, filename);
        CPH.ObsSetSourceVisibility(OBSScene, OBSSource, true);

        CPH.Wait(media_duration * 1000);
        CPH.ObsSetSourceVisibility(OBSScene, OBSSource, false);
        CPH.Wait(100);
    }
}
