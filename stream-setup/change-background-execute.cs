using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;
using Newtonsoft.Json;

public class CPHInline
{
    private static readonly Random _random = new Random();

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        // get OBS scene/source from subactions
        if (!CPH.TryGetArg<string>("OBSScene", out string OBSScene))
            return false;
        if (!CPH.TryGetArg<string>("OBSSource", out string OBSSource))
            return false;

        // get backgrounds
        string catalogJson = CPH.GetGlobalVar<string>("ID736BackgroundsCatalog", false);
        if (string.IsNullOrWhiteSpace(catalogJson))
            return false;

        Dictionary<string, Dictionary<string, object>> catalog;
        try
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(catalogJson);
        }
        catch (Exception ex)
        {
            id736.Log.Message($"failed to parse catalog: {ex.Message}", filenamePrefix: "backgrounds");
            return false;
        }

        if (catalog == null || catalog.Count == 0)
            return false;

        // get current media file on that source
        string filename = "";
        string response = CPH.ObsSendRaw("GetInputSettings", $"{{\"inputName\": \"{OBSSource}\"}}", 0);
        try
        {
            var obsData = id736.Data.JsonToNestedDictionary(response);
            string filePath = id736.Data.GetValue<string>(obsData, "inputSettings.local_file");
            if (!string.IsNullOrEmpty(filePath))
            {
                // Set the file path into a local Streamer.bot variable %obsMediaFile%
                CPH.SetArgument("obsMediaFile", filePath);
                // Optional: Extract just the raw filename from the path
                string fileName = System.IO.Path.GetFileName(filePath);
                CPH.SetArgument("obsMediaFileName", fileName);
                // Log the output to your Streamer.bot Action Log
                id736.Log.Message($"Successfully found media file: {filePath}", filenamePrefix: "backgrounds");
                filename = filePath;
            }
            else
            {
                id736.Log.Message($"Could not find a file attached to source: '{OBSSource}'", filenamePrefix: "backgrounds");
            }
        }
        catch (Exception ex)
        {
            id736.Log.Message($"Failed to parse OBS response. Error: {ex.Message}", filenamePrefix: "backgrounds");
            return false;
        }

        string choice = filename;
        while (choice == filename)
        {
            int randomIndex = _random.Next(catalog.Count);
            string randomKey = catalog.Keys.ElementAt(randomIndex);
            choice = randomKey;
        }

        // resolve the chosen file from the catalog row
        string targetFile = choice;
        if (catalog.TryGetValue(choice, out Dictionary<string, object> row))
        {
            string rowFile = id736.Data.GetValue<string>(row, "Filename");
            if (!string.IsNullOrWhiteSpace(rowFile))
                targetFile = rowFile;
        }

        // set the scene/source
        id736.Log.Message($"filename: {targetFile}", filenamePrefix: "backgrounds");
        CPH.ObsSetMediaSourceFile(OBSScene, OBSSource, targetFile);
        CPH.ObsSetSourceVisibility(OBSScene, OBSSource, true);
        CPH.ObsSetSourceVisibility(OBSScene, "background", false);

        return true;
    }
}
