using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;

public class MyCsvRecord
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Info { get; set; } = "";
    public string PhysicalItem { get; set; } = "";
}

public class CPHInline
{
	public bool Execute()
	{
		string sheetsURL = args["GoogleSheetURL"].ToString();
		string csvURL = sheetsURL.Replace("/edit", "/gviz/tq?tqx=out:csv&sheet=Sheet1");
		
		Dictionary<string, List<Dictionary<string,string>>> PrizeCatalog = new Dictionary<string, List<Dictionary<string,string>>>();
		
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
            record.Name = record.Name.Trim();
            record.Type = record.Type.Trim();
            record.Info = record.Info.Trim();
            record.PhysicalItem = record.PhysicalItem.Trim().ToLower();

			if (record.Name == "" || record.Type == "" || record.Info == "" || record.PhysicalItem == "") 
            {
                CPH.LogInfo($"skipping invalid record: {record.Name},{record.Type},{record.Info},{record.PhysicalItem}");
				continue;
			}

            Dictionary<string,string> item = new Dictionary<string,string>(){
                {"Name", record.Name},
                {"Type", record.Type},
                {"Info", record.Info},
                {"PhysicalItem", record.PhysicalItem}
            };


            if (!PrizeCatalog.ContainsKey(record.Type))
            {
                var list = new List<Dictionary<string,string>>();
                PrizeCatalog.Add(record.Type, list);
            }
			PrizeCatalog[record.Type].Add(item);
		}

        // store in global non-persisted memory
        CPH.SetGlobalVar("ID736PrizeCatalog", PrizeCatalog, false);
		return true;
	}
}
