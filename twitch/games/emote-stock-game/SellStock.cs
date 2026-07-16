using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Core.LinkStreamerbot(CPH);

		string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;
		string userName = id736.Chat.GetCurrentUserName();
		string platform = id736.Chat.GetCurrentPlatform();

		if (string.IsNullOrWhiteSpace(userName))
		{
			id736.Log.Message("could not determine username", filenamePrefix: "stockgame");
			return false;
		}

		string stockPricesJson = CPH.GetGlobalVar<string>("EmoteStockGame_prices", true);
		var stockPrices = id736.Data.FromJson<Dictionary<string, Dictionary<string, int>>>(stockPricesJson)
			?? new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

		var stocks = id736.Points.GetDictionary(userName, platform, "stocks");
		int points = id736.Points.Get(userName, platform, "points");

		if (!CPH.TryGetArg("match[2]", out string stockName))
		{
			id736.Chat.SendReplyOrMessage("Sorry, I don't have a matching stock to sell.", msgId);
			return false;
		}

		if (!stockPrices.ContainsKey(stockName))
		{
			id736.Chat.SendReplyOrMessage($"Sorry, '{stockName}' is not a valid stock.", msgId);
			return false;
		}

		if (!CPH.TryGetArg("match[4]", out string quantityRaw))
		{
			id736.Chat.SendReplyOrMessage("Sorry, I couldn't understand how many stock you wanted to sell.", msgId);
			return false;
		}

		if (!int.TryParse(quantityRaw, out int stockQuantity) || stockQuantity <= 0)
		{
			id736.Chat.SendReplyOrMessage("Sorry, that quantity doesn't look like a valid number.", msgId);
			return false;
		}

		if (!stocks.ContainsKey(stockName) || stockQuantity > stocks[stockName])
		{
			id736.Chat.SendReplyOrMessage($"Sorry, you don't have enough {stockName} stock to sell {stockQuantity} shares.", msgId);
			return false;
		}

		int cost = stockPrices[stockName]["currentPrice"] * stockQuantity;

		stocks[stockName] -= stockQuantity;
		points += cost;

		id736.Points.SetDictionary(userName, platform, "stocks", stocks);
		id736.Points.Set(userName, platform, "points", points);

		string emote = platform == "twitch" ? "TheIlluminati" : "";
		id736.Chat.SendReplyOrMessage($"{emote}You sold {stockQuantity} shares of {stockName} for ${cost} points. You now have {points} points. {emote}", msgId);

		return true;
	}
}
