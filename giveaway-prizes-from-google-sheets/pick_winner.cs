using System;
using System.Collections.Generic;

public class CPHInline
{
	public bool Execute()
	{
		Dictionary<string, List<Dictionary<string,string>>> PrizeCatalog = CPH.GetGlobalVar<Dictionary<string, List<Dictionary<string,string>>>>("ID736PrizeCatalog", false);
        if (PrizeCatalog == null)
        {
            CPH.RunAction("Load Giveaway CSV data from Google Sheets");
        }
    	PrizeCatalog = CPH.GetGlobalVar<Dictionary<string, List<Dictionary<string,string>>>>("ID736PrizeCatalog", false);
		if (PrizeCatalog == null)
		{
            CPH.SendMessage("sorry, something went wrong loading the giveaway prize catalog, 2nd try");
            return true;
        }
		
		List<string> usernames = new List<string>();

		foreach(KeyValuePair<string,object> kvp in args)
		{ 
			if(kvp.Key.Contains("randomUserName"))
			{
				string tmpUsername = kvp.Value.ToString();
				if (args["excludeUsersFromGroup"].ToString() == "" || (args["excludeUsersFromGroup"].ToString() != "" && !CPH.UserInGroup(tmpUsername, args["excludeUsersFromGroup"].ToString())))
				{
					usernames.Add(tmpUsername);
				}
			}
		}

		int seed = Environment.TickCount * new Guid().GetHashCode();
		Random rnd = new Random(seed);
		
		int index = rnd.Next(usernames.Count);
		string username = usernames[index];

		List<string> keys = new List<string>(PrizeCatalog.Keys);
		
		// reset random number generator
		seed = Environment.TickCount * new Guid().GetHashCode();
		rnd = new Random(seed);
		index = rnd.Next(keys.Count);
		string giftType = keys[index];
		
		
		List<Dictionary<string,string>> prizes = PrizeCatalog[giftType];

		// reset random number generator
		seed = Environment.TickCount * new Guid().GetHashCode();
		rnd = new Random(seed);
		
		index = rnd.Next(prizes.Count);
		Dictionary<string,string> prize = prizes[index];
		
		string extraContent = "";
		if (prize["PhysicalItem"] == "true" && args["showInfoFieldInChat"].ToString() == "True" && args["hideInfoForDigitalItem"] != "True")
		{
			extraContent = prize["Info"];
		}
		
		string message = $"Congratulations {username}, you have been chosen as a giveaway recipient! You have been given a {giftType}: {prize["Name"]} {extraContent}";
		
		CPH.SendMessage(message);
		if (prize["PhysicalItem"] == "true")
		{
			CPH.SendMessage($"{username}, since this is a physical item to send, please DM me your shipping information.");
		}
		
		if (args["discordWebhookURL"].ToString() != "YOUR DISCORD WEBHOOK HERE")
		{
			CPH.DiscordPostTextToWebhook(args["discordWebhookURL"].ToString(), message);
		}
		return true;
	}
}
