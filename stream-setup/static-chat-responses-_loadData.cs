using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        string sheetsURL = args["GoogleSheetURL"].ToString();

        var catalog = id736.GoogleSheets.ReadFile(sheetsURL, "Sheet1", id736.SheetDuplicateKeyMode.LastEntryWins);

        var responses = new Dictionary<string, string>();
        foreach (var kvp in catalog)
        {
            if (!(kvp.Value is Dictionary<string, object> row))
                continue;

            string command = GetString(row, "ChatCommand");
            string response = GetString(row, "ChatResponse");

            if (!string.IsNullOrWhiteSpace(command) && !string.IsNullOrWhiteSpace(response))
                responses[command] = response;
        }

        CPH.SetGlobalVar("ID736StaticChatResponses", responses, false);
        return true;
    }

    private string GetString(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out object value) && value != null)
            return value.ToString().Trim();
        return null;
    }
}
