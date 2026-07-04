using System;
using System.Linq;
using System.Collections.Generic;

public class CPHInline
{
    public void SendMessage(string username, string message, string msgId, bool fromBot=true)
    {
        if(msgId != "")
            CPH.TwitchReplyToMessage($"TwitchSings {message}", msgId, fromBot, true);
        else
            CPH.SendMessage($"TwitchSings @{username}: {message}", fromBot);
    }
	public bool Execute()
	{
        string msgId ;
        CPH.TryGetArg("msgId", out msgId);
		CPH.TryGetArg("userName", out string username);

        Dictionary<string, Dictionary<string, int>> stockPrices = CPH.GetGlobalVar<Dictionary<string, Dictionary<string, int>>>("EmoteStockGame_prices", true);
        Dictionary<string, int> stocks = CPH.GetTwitchUserVar<Dictionary<string, int>>(username, "stocks", true);
        int points = CPH.GetTwitchUserVar<int>(username, "points", true);

        if (stocks == null) {
            string msg = $"Sorry, you don't own any stock, but you do have {points} points to buy some.";
            SendMessage(username, msg, msgId, true);
            return false;
        }

        var keys = Enumerable.ToList((IEnumerable<string>)stocks.Keys);

        string stockMsg = "Current stocks: ";
        foreach (var key in keys)
        {
            if (stocks[key] > 0)
                stockMsg += $" ... {stocks[key]} x {key} (value ${stockPrices[key]["currentPrice"] * stocks[key]})";
        }
        stockMsg += $"; you have {points} points.";

        SendMessage(username, stockMsg, msgId, false);
		return true;
	}
}