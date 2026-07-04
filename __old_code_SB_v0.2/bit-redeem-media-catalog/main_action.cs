using System;
using System.Runtime;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NLayer;
using Duration.Mine.Mp4;


public class CPHInline
{
    public bool Execute()
    {
        Dictionary<string, List<Dictionary<string,object>>> MediaCatalog = CPH.GetGlobalVar<Dictionary<string, List<Dictionary<string,object>>>>("ID736MediaCatalog", false);
        if (MediaCatalog == null) {
            CPH.RunAction("Load CSV data from Google Sheets");
        }
    	MediaCatalog = CPH.GetGlobalVar<Dictionary<string, List<Dictionary<string,object>>>>("ID736MediaCatalog", false);
		if (MediaCatalog == null) {
            CPH.SendMessage("sorry, something went wrong loading the media catalog, 2nd try");
            return true;
        }

		// don't change any of this stuff
		string oldScene = CPH.ObsGetCurrentScene();
		var bitsCheered = args["bits"].ToString();
		CPH.LogDebug($"bits cheered: {bitsCheered}");
		string background = "";
		string music = "";
		int timeout = Int32.MaxValue;


		if (MediaCatalog.ContainsKey(bitsCheered)) {
			List<Dictionary<string, object>> mediaList = MediaCatalog[bitsCheered];
			
			int duration = int.Parse(mediaList[0]["Duration"].ToString());

            foreach (var media in mediaList) {
            	if (media["Type"] == "txt")
            	{
            		CPH.ObsSetGdiText(media["OBSScene"].ToString(), media["OBSSource"].ToString(), media["Filename"].ToString());
				}
				else
				{
					CPH.ObsSetMediaSourceFile(media["OBSScene"].ToString(), media["OBSSource"].ToString(), media["Filename"].ToString());
				}
			}
            foreach (var media in mediaList) {
            	if (media["Type"] == "txt") continue;
				CPH.ObsShowSource(media["OBSScene"].ToString(), media["OBSSource"].ToString());
            }
            CPH.Wait(200);
            foreach (var media in mediaList) {
            	if (media["Type"] != "txt") continue;
				CPH.ObsShowSource(media["OBSScene"].ToString(), media["OBSSource"].ToString());
            }
			// pause here while media plays
			CPH.Wait((duration * 1000)+100);

			// hide the sources afterwards
            foreach (var media in mediaList) {
			    CPH.ObsHideSource(media["OBSScene"].ToString(), media["OBSSource"].ToString());
            }
			
			// put another wait here of at least 100ms so overlapping bit redeems don't mess up your scenes showing
			CPH.Wait(100);
		}
		return true;
	}
}
