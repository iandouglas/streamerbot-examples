using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Chat.SetContext(CPH);
		id736.Points.SetContext(CPH);

		string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;
		string userName = id736.Chat.GetCurrentUserName();
		string platform = id736.Chat.GetCurrentPlatform();

		if (string.IsNullOrWhiteSpace(userName))
		{
			CPH.LogWarn("[StockGame] could not determine username");
			return false;
		}

		string stockPricesJson = CPH.GetGlobalVar<string>("EmoteStockGame_prices", true);
		var stockPrices = id736.Data.FromJson<Dictionary<string, Dictionary<string, int>>>(stockPricesJson)
			?? new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

		var stocks = id736.Points.GetDictionary(userName, platform, "stocks");
		int points = id736.Points.Get(userName, platform, "points");

		if (stocks == null || stocks.Count == 0)
		{
			id736.Chat.SendReplyOrMessage($"Sorry, you don't own any stock, but you do have {points} points to buy some.", msgId);
			return false;
		}

		var parts = new List<string>();
		foreach (var kvp in stocks)
		{
			string stockName = kvp.Key;
			int quantity = kvp.Value;
			if (quantity <= 0)
				continue;

			int currentPrice = stockPrices.ContainsKey(stockName) ? stockPrices[stockName]["currentPrice"] : 0;
			int value = currentPrice * quantity;
			parts.Add($"{quantity} x {stockName} (value {value}pts)");
		}

		string stockMsg = parts.Count > 0
			? $"Current stocks: {string.Join(" ... ", parts)}; you have {points} points."
			: $"You don't own any stock right now, but you have {points} points.";

		id736.Chat.SendReplyOrMessage(stockMsg, msgId);
		return true;
	}
}
