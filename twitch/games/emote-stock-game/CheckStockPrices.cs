using System;
using System.Linq;
using System.Collections.Generic;

public class CPHInline
{
	public bool Execute()
	{
        Dictionary<string, Dictionary<string, int>> stocks = CPH.GetGlobalVar<Dictionary<string, Dictionary<string, int>>>("EmoteStockGame_prices", true);

        var keys = Enumerable.ToList((IEnumerable<string>)stocks.Keys);

        string priceMsg = "TwitchSings Current stocks: ";
        foreach (var key in keys)
        {
            priceMsg += $"{key} : ${stocks[key]["currentPrice"]} ... ";
        }

        priceMsg += "to see your stock portfolio use !holdings";
        CPH.SendMessage(priceMsg, false);
		return true;
	}
}