using System;
using System.Collections.Generic;
using System.IO;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        string sheetsURL = args["GoogleSheetURL"].ToString();

        var catalog = id736.GoogleSheets.ReadFile(sheetsURL, "Sheet1", id736.SheetDuplicateKeyMode.LastEntryWins);

        var cleaned = new Dictionary<string, Dictionary<string, object>>();
        foreach (var kvp in catalog)
        {
            if (!(kvp.Value is Dictionary<string, object> row))
                continue;

            string emoteName = GetString(row, "EmoteName");
            string filename = GetString(row, "Filename");
            string type = GetString(row, "Type");

            if (string.IsNullOrWhiteSpace(emoteName) || string.IsNullOrWhiteSpace(filename))
            {
                id736.Log.Message($"skipping invalid record: {emoteName},{filename},{GetString(row, "OBSScene")},{GetString(row, "OBSSource")},{GetString(row, "Duration")}", filenamePrefix: "emoteaudio");
                continue;
            }

            if (type == "txt")
            {
                id736.Log.Message($"skipping text entry: {emoteName}", filenamePrefix: "emoteaudio");
                continue;
            }

            var safeRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row)
            {
                safeRow[cell.Key] = MakeSerializable(cell.Value);
            }

            safeRow["Filename"] = filename.Replace("\\", "/");
            safeRow["FileExtension"] = Path.GetExtension(filename).ToLowerInvariant();

            // Resolve missing Duration using Media helper
            if (!safeRow.ContainsKey("Duration") || GetString(safeRow, "Duration") == "")
            {
                double? seconds = id736.Media.LengthInSeconds(filename);
                if (seconds.HasValue)
                {
                    safeRow["Duration"] = Convert.ToInt32(seconds.Value);
                }
                else
                {
                    id736.Log.Message($"could not determine duration, defaulting to 5s: {filename}", filenamePrefix: "emoteaudio");
                    safeRow["Duration"] = 5;
                }
            }
            else
            {
                // ensure Duration is stored as an int
                safeRow["Duration"] = Convert.ToInt32(safeRow["Duration"]);
            }

            cleaned[emoteName] = safeRow;
        }

        // Store as a JSON string to avoid cross-AppDomain serialization issues
        // with Dictionary<string, object> graphs that may contain JToken/JObject.
        string json = JsonConvert.SerializeObject(cleaned);
        CPH.SetGlobalVar("ID736EmoteMediaCatalog", json, false);
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

        // Primitives and strings are fine as-is.
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

        // Fall back to a string representation for anything else (e.g., JToken).
        return value.ToString();
    }
}
