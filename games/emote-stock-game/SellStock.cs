using System;
using System.Linq;
using System.Collections.Generic;

public class CPHInline
{
    public void SendReply(string username, string message, string msgId="", bool fromBot=true)
    {
        if (msgId != "") {
            message = $"TwitchSings {message}";
            CPH.TwitchReplyToMessage(message, msgId, fromBot);
            return;
        }
        message = $"TwitchSings @{username}: ";
        CPH.SendMessage(message, fromBot);
    }

	public bool Execute()
	{
        string msgId = "";
		CPH.TryGetArg("msgId", out msgId);
		CPH.TryGetArg("userName", out string username);

        Dictionary<string, Dictionary<string, int>> stockPrices = CPH.GetGlobalVar<Dictionary<string, Dictionary<string, int>>>("EmoteStockGame_prices", true);
        Dictionary<string, int> stocks = CPH.GetTwitchUserVar<Dictionary<string, int>>(username, "stocks", true);
        if (stocks == null) {
            stocks = new();
        }
        int points = CPH.GetTwitchUserVar<int>(username, "points", true);

        if (!CPH.TryGetArg("match[2]", out string stockName))
        {
            SendReply(username, $"Sorry I don't have a matching stock to buy.", msgId, true);
            return false;
        }

        if (!CPH.TryGetArg("match[4]", out string quantityRaw))
        {
            SendReply(username, $"Sorry, I couldn't understand how many stock you wanted to sell.", msgId, true);
            return false;
        }
        if (! int.TryParse(quantityRaw, out int stockQuantity))
        {
            SendReply(username, $"Sorry, that quantity doesn't look like a number number.", msgId, true);
            return false;
        }
        int cost = stockPrices[stockName]["currentPrice"] * stockQuantity;

        if (!stocks.ContainsKey(stockName) || stockQuantity > stocks[stockName]) {
            SendReply(username, $"Sorry, you don't have enough stock to sell that many.", msgId, true);
            return false;
        }

        stocks[stockName] -= stockQuantity ;
        points += cost;

        CPH.SetTwitchUserVar(username, "stocks", stocks, true);
        CPH.SetTwitchUserVar(username, "points", points, true);

        SendReply(username, $"TheIlluminati You sold {quantityRaw} of {stockName} for ${cost} points TheIlluminati", msgId, true);

		return true;
	}
}