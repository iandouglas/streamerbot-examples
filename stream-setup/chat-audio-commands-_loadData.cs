using System;
using System.Collections.Generic;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        string sheetsURL = args["GoogleSheetURL"].ToString();

        var catalog = id736.GoogleSheets.ReadFile(sheetsURL, "ChatAudioCommands", id736.SheetDuplicateKeyMode.LastEntryWins);

        var cleaned = new Dictionary<string, Dictionary<string, object>>();
        foreach (var kvp in catalog)
        {
            if (!(kvp.Value is Dictionary<string, object> row))
                continue;

            string command = GetString(row, "ChatCommand");
            string audioPath = GetString(row, "AudioPathOrFolder");

            if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(audioPath))
            {
                CPH.LogInfo($"[ChatAudio] skipping invalid record: {command},{audioPath}");
                continue;
            }

            // Normalize to lowercase, strip leading ! for matching later
            string normalizedCommand = command.Trim().ToLowerInvariant().TrimStart('!');

            var safeRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row)
            {
                safeRow[cell.Key] = MakeSerializable(cell.Value);
            }

            safeRow["AudioPathOrFolder"] = audioPath.Replace("\\", "/").TrimEnd('/');
            safeRow["Command"] = normalizedCommand;

            // Optional volume, default 0.5
            if (!safeRow.ContainsKey("Volume") || string.IsNullOrWhiteSpace(GetString(safeRow, "Volume")))
                safeRow["Volume"] = 0.5;
            else
                safeRow["Volume"] = Convert.ToDouble(safeRow["Volume"]);

            cleaned[normalizedCommand] = safeRow;
        }

        // Store as JSON to avoid cross-AppDomain serialization issues with JToken/JObject.
        string json = JsonConvert.SerializeObject(cleaned);
        CPH.SetGlobalVar("ID736ChatAudioCommands", json, false);
        return true;
    }

    private string GetString(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out object value) && value != null)
            return value.ToString().Trim();
        return null;
    }

    private object MakeSerializable(object value)
    {
        if (value == null)
            return null;

        if (value is string || value is bool || value is int || value is long || value is double || value is float || value is decimal || value is DateTime)
            return value;

        if (value is Dictionary<string, object> dict)
        {
            var safe = new Dictionary<string, object>();
            foreach (var kvp in dict)
                safe[kvp.Key] = MakeSerializable(kvp.Value);
            return safe;
        }

        if (value is List<object> list)
        {
            var safe = new List<object>();
            foreach (var item in list)
                safe.Add(MakeSerializable(item));
            return safe;
        }

        return value.ToString();
    }
}
