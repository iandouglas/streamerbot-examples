using System;
using System.Collections.Generic;
using System.IO;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg<string>("rewardName", out string rewardName))
            return false;

        CPH.LogDebug($"channel reward redeemed: {rewardName}");

        string catalogJson = CPH.GetGlobalVar<string>("ID736ChanPtsMediaCatalog", false);
        if (string.IsNullOrWhiteSpace(catalogJson))
            return false;

        Dictionary<string, Dictionary<string, object>> catalog;
        try
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(catalogJson);
        }
        catch (Exception ex)
        {
            CPH.LogError($"[ChannelRewards] failed to parse catalog: {ex.Message}");
            return false;
        }

        if (catalog == null || catalog.Count == 0)
            return false;

        if (!catalog.TryGetValue(rewardName, out Dictionary<string, object> item))
            return true;

        string filename = id736.Data.GetValue<string>(item, "Filename");
        string type = id736.Data.GetValue<string>(item, "Type");
        string OBSScene = id736.Data.GetValue<string>(item, "OBSScene");
        string OBSSource = id736.Data.GetValue<string>(item, "OBSSource");

        if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(type))
            return true;

        double? seconds = id736.Media.LengthInSeconds(filename);
        if (!seconds.HasValue)
        {
            CPH.LogInfo($"[ChannelRewards] Could not determine duration for {filename}");
            return true;
        }
        int media_duration = Convert.ToInt32(seconds.Value);

        switch (type.ToLowerInvariant())
        {
            case "audio":
                CPH.PlaySound(filename, 0.5f, false);
                CPH.Wait(media_duration * 1000);
                CPH.Wait(100);
                break;

            case "video":
                if (string.IsNullOrWhiteSpace(OBSScene) || string.IsNullOrWhiteSpace(OBSSource))
                {
                    CPH.LogInfo($"[ChannelRewards] Missing OBS scene/source for video reward: {rewardName}");
                    return true;
                }

                CPH.ObsSetMediaSourceFile(OBSScene, OBSSource, filename);
                CPH.ObsSetSourceVisibility(OBSScene, OBSSource, true);
                CPH.Wait(media_duration * 1000);
                CPH.ObsSetSourceVisibility(OBSScene, OBSSource, false);
                CPH.Wait(100);
                break;

            default:
                CPH.LogInfo($"[ChannelRewards] Unknown reward type '{type}' for {rewardName}");
                return false;
        }

        return true;
    }
}
