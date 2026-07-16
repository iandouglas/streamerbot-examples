using System;
using System.Collections.Generic;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        // Try rawInput first (chat message), fall back to message if needed.
        string rawInput = null;
        if (!CPH.TryGetArg("rawInput", out rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            if (!CPH.TryGetArg("message", out rawInput) || string.IsNullOrWhiteSpace(rawInput))
            {
                id736.Log.Message("no rawInput or message argument found", filenamePrefix: "chataudio");
                return false;
            }
        }

        string command = rawInput.Trim().ToLowerInvariant().TrimStart('!');
        if (string.IsNullOrWhiteSpace(command))
            return false;

        id736.Log.Message($"received command: {command}", filenamePrefix: "chataudio");

        string catalogJson = CPH.GetGlobalVar<string>("ID736ChatAudioCommands", false);
        if (string.IsNullOrWhiteSpace(catalogJson))
        {
            id736.Log.Message("catalog variable ID736ChatAudioCommands is empty", filenamePrefix: "chataudio");
            return false;
        }

        Dictionary<string, Dictionary<string, object>> catalog;
        try
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(catalogJson);
        }
        catch (Exception ex)
        {
            id736.Log.Message($"failed to parse catalog: {ex.Message}", filenamePrefix: "chataudio");
            return false;
        }

        if (catalog == null || catalog.Count == 0)
        {
            id736.Log.Message("parsed catalog is null or empty", filenamePrefix: "chataudio");
            return false;
        }

        if (!catalog.TryGetValue(command, out Dictionary<string, object> row))
        {
            id736.Log.Message($"command not found in catalog: {command}", filenamePrefix: "chataudio");
            return false;
        }

        string pathOrFolder = id736.Data.GetValue<string>(row, "AudioPathOrFolder");
        if (string.IsNullOrWhiteSpace(pathOrFolder))
        {
            id736.Log.Message($"missing AudioPathOrFolder for command: {command}", filenamePrefix: "chataudio");
            return false;
        }

        // Normalize volume to 0.0 - 1.0 range. Sheet may contain 0-100 percentages.
        double volume = 0.5;
        object volObj = id736.Data.GetValue(row, "Volume");
        if (volObj != null)
        {
            try
            {
                volume = Convert.ToDouble(volObj);
                if (volume > 1.0)
                    volume = volume / 100.0;
                if (volume < 0.0)
                    volume = 0.0;
                if (volume > 1.0)
                    volume = 1.0;
            }
            catch { }
        }

        id736.Log.Message($"resolving media for {pathOrFolder}", filenamePrefix: "chataudio");
        string fileToPlay = id736.Media.ResolveMediaFile(pathOrFolder);
        if (string.IsNullOrWhiteSpace(fileToPlay))
        {
            id736.Log.Message($"no media file found for command {command} at {pathOrFolder}", filenamePrefix: "chataudio");
            return false;
        }

        id736.Log.Message($"command {command} playing {fileToPlay} at volume {volume}", filenamePrefix: "chataudio");
        CPH.PlaySound(fileToPlay, (float)volume, false);
        return true;
    }
}
