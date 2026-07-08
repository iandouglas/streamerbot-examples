using System;
using System.Collections.Generic;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        // Try rawInput first (chat message), fall back to message if needed.
        string rawInput = null;
        if (!CPH.TryGetArg("rawInput", out rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            if (!CPH.TryGetArg("message", out rawInput) || string.IsNullOrWhiteSpace(rawInput))
            {
                CPH.LogDebug("[ChatAudio] no rawInput or message argument found");
                return false;
            }
        }

        string command = rawInput.Trim().ToLowerInvariant().TrimStart('!');
        if (string.IsNullOrWhiteSpace(command))
            return false;

        CPH.LogDebug($"[ChatAudio] received command: {command}");

        string catalogJson = CPH.GetGlobalVar<string>("ID736ChatAudioCommands", false);
        if (string.IsNullOrWhiteSpace(catalogJson))
        {
            CPH.LogWarn("[ChatAudio] catalog variable ID736ChatAudioCommands is empty");
            return false;
        }

        Dictionary<string, Dictionary<string, object>> catalog;
        try
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(catalogJson);
        }
        catch (Exception ex)
        {
            CPH.LogError($"[ChatAudio] failed to parse catalog: {ex.Message}");
            return false;
        }

        if (catalog == null || catalog.Count == 0)
        {
            CPH.LogWarn("[ChatAudio] parsed catalog is null or empty");
            return false;
        }

        if (!catalog.TryGetValue(command, out Dictionary<string, object> row))
        {
            CPH.LogDebug($"[ChatAudio] command not found in catalog: {command}");
            return false;
        }

        string pathOrFolder = id736.Data.GetValue<string>(row, "AudioPathOrFolder");
        if (string.IsNullOrWhiteSpace(pathOrFolder))
        {
            CPH.LogWarn($"[ChatAudio] missing AudioPathOrFolder for command: {command}");
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

        CPH.LogDebug($"[ChatAudio] resolving media for {pathOrFolder}");
        string fileToPlay = id736.Media.ResolveMediaFile(pathOrFolder);
        if (string.IsNullOrWhiteSpace(fileToPlay))
        {
            CPH.LogWarn($"[ChatAudio] no media file found for command {command} at {pathOrFolder}");
            return false;
        }

        CPH.LogInfo($"[ChatAudio] command {command} playing {fileToPlay} at volume {volume}");
        CPH.PlaySound(fileToPlay, (float)volume, false);
        return true;
    }
}
