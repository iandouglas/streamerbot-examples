using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Core.LinkStreamerbot(CPH);

		string stockPricesJson = CPH.GetGlobalVar<string>("EmoteStockGame_prices", true);
		var stocks = id736.Data.FromJson<Dictionary<string, Dictionary<string, int>>>(stockPricesJson)
			?? new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

		if (stocks.Count == 0)
		{
			id736.Chat.SendMessage("No stocks are available right now. The market hasn't opened yet!");
			return false;
		}

		string platform = id736.Chat.GetCurrentPlatform();
		string emote = platform == "twitch" ? "TwitchSings " : "";

		var priceParts = stocks
			.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
			.Select(kvp => $"{kvp.Key} : {kvp.Value["currentPrice"]}pts")
			.ToList();

		string message = $"{emote}Current stocks: {string.Join(" ... ", priceParts)} -- use !holdings to see your portfolio";
		id736.Chat.SendMessage(message, false);
		return true;
	}
}
