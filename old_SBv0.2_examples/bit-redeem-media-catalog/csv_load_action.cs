using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using Duration.Mine.Mp4;

public class MyCsvRecord
{
    public string Bits { get; set; } = "0";
    public string Filename { get; set; } = "";
    public string Type { get; set; } = "";
    public string OBSScene { get; set; } = "";
    public string OBSSource { get; set; } = "";
    public string? Duration { get; set; } = "10000000";
}

public class CPHInline
{
	public bool Execute()
	{
		string sheetsURL = args["GoogleSheetURL"].ToString();
		string csvURL = sheetsURL.Replace("/edit", "/gviz/tq?tqx=out:csv&sheet=Sheet1");
		
		Dictionary<int, List<Dictionary<string,object>>> MediaCatalog = new Dictionary<int, List<Dictionary<string,object>>>();
		
        string basePath = "";

		List<MyCsvRecord> records = new List<MyCsvRecord>();
		using (HttpClient client = new HttpClient())
        {
            Stream stream = client.GetStreamAsync(csvURL).Result;
            using (StreamReader reader = new StreamReader(stream))
            try {
            	
				using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
				{
					csv.Read();
					csv.ReadHeader();
					while (csv.Read())
					{
						var record = csv.GetRecord<MyCsvRecord>();
                        if (record.Bits == "-1") {
                            basePath = record.Filename.Replace("\\", "/") + "/";
                            basePath = basePath.Replace("//", "/");
                            continue;
                        }
                        if (record.Type != "txt") {
							record.Filename = record.Filename.Replace("\\", "/");
						}
						records.Add(record);
					}
				}
			
			}
			catch (CsvHelperException ex)
			{
                CPH.LogInfo("we caught a csv exception");
				CPH.LogInfo(ex.Message);
                CPH.LogInfo(ex.StackTrace);
                CPH.LogInfo(ex.InnerException.Message);
			}
		}


		foreach (var record in records)
		{
			int TWITCH_BITS = int.Parse(record.Bits);
            record.Filename = record.Filename.Trim();
            record.OBSScene = record.OBSScene.Trim();
            record.OBSSource = record.OBSSource.Trim();

			if (TWITCH_BITS <= 0 || record.Filename == "" || record.OBSScene == "" || record.OBSSource == "") 
            {
                CPH.LogInfo($"skipping invalid record: {record.Bits},{record.Filename},{record.OBSScene},{record.OBSSource},{record.Duration}");
				continue;
			}

            Dictionary<string,object> item = new Dictionary<string,object>(){
                {"Filename", record.Filename.Replace("\\", "/")},
                {"FileExtension", Path.GetExtension(record.Filename).ToLower()},
                {"Type", record.Type},
                {"Duration", record.Duration},
                {"OBSScene", record.OBSScene},
                {"OBSSource", record.OBSSource}
            };
            if (record.Type != "txt")
            {
                item["Filename"] = basePath + item["Filename"];
            }

            if (record.Duration == "" && item["FileExtension"].ToString() == ".mp3") 
            {
            	try {
					var mp3 = new NLayer.MpegFile(item["Filename"].ToString());
					item["Duration"] = Convert.ToInt32(mp3.Duration.TotalSeconds);
				}
				catch (System.IO.FileNotFoundException ex)
				{
					CPH.LogInfo($"could not file, skipping entry: {item["Filename"]}");
					continue;
				}
            }
            else if (record.Duration == "" && item["FileExtension"].ToString() == ".mp4") 
            {
            	try
            	{
					item["Duration"] = Convert.ToInt32(Mp4Duration.GetMp4Duration(item["Filename"].ToString()));
				}
				catch (System.IO.FileNotFoundException ex)
				{
					CPH.LogInfo($"could not file, skipping entry: {item["Filename"]}");
					continue;
				}
            } 
            else
            {
            	if(record.Duration == "")
            	{
					record.Duration = "5";
            	}
                item["Duration"] = int.Parse(record.Duration);
            }

            if (!MediaCatalog.ContainsKey(TWITCH_BITS))
            {
                var list = new List<Dictionary<string,object>>();
                list.Add(item);
                MediaCatalog.Add(TWITCH_BITS, list);
            }
            else
            {
                if (record.Duration == "")
                {
                    // set new min duration if this filename has a shorter duration
                    int minDuration = 1000000;
                    foreach (var mediaFile in MediaCatalog[TWITCH_BITS])
                    {
                        if (int.Parse(mediaFile["Duration"].ToString()) < minDuration)
                        {
                            minDuration = int.Parse(mediaFile["Duration"].ToString());
                        }
                    }
                    
                    if (int.Parse(item["Duration"].ToString()) > minDuration)
                    {
                        item["Duration"] = minDuration.ToString();
                    }
                    else 
                    {
                        foreach (var mediaFile in MediaCatalog[TWITCH_BITS])
                        {
                            mediaFile["Duration"] = int.Parse(item["Duration"].ToString());
                        }
                    }
                }
                MediaCatalog[TWITCH_BITS].Add(item);
            }
		}

        // store in global non-persisted memory
        CPH.SetGlobalVar("ID736MediaCatalog", MediaCatalog, false);
		return true;
	}
}
