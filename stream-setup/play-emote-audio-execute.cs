using System;
using System.Collections.Generic;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("message", out string message) || string.IsNullOrWhiteSpace(message))
            return false;
        if (!CPH.TryGetArg("userName", out string userName) || string.IsNullOrWhiteSpace(userName))
            return false;
        if (userName == "id736bot")
            return false;

        if (message.Contains("TwitchSings") || message.Contains("TheIlluminati"))
            return false;
        if (message.StartsWith("!buy"))
            return false;
        if (message.StartsWith("!sell"))
            return false;
        if (message.StartsWith("!holding"))
            return false;

        string catalogJson = CPH.GetGlobalVar<string>("ID736EmoteMediaCatalog", false);
        if (string.IsNullOrWhiteSpace(catalogJson))
            return false;

        Dictionary<string, Dictionary<string, object>> catalog;
        try
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(catalogJson);
        }
        catch (Exception ex)
        {
            CPH.LogError($"[EmoteAudio] failed to parse catalog: {ex.Message}");
            return false;
        }

        if (catalog == null || catalog.Count == 0)
            return false;

        string[] tokens = message.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (!catalog.TryGetValue(token, out Dictionary<string, object> item))
                continue;

            string filename = id736.Data.GetValue<string>(item, "Filename");
            if (string.IsNullOrWhiteSpace(filename))
                continue;

            CPH.PlaySound(filename, 0.5f, false);
            return true;
        }

        return false;
    }
}
