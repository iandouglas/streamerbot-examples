using System;
using System.Linq;
using System.Collections.Generic;

public class CPHInline
{
	public bool Execute()
	{
        Dictionary<string, Dictionary<string, int>> stocks = CPH.GetGlobalVar<Dictionary<string, Dictionary<string, int>>>("EmoteStockGame_prices", true);

        if (stocks == null) {
            stocks = new Dictionary<string, Dictionary<string, int>>{
                ["iandouDeadpoolHeart"] = new Dictionary<string, int>{["volatility"] = 50,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 10000},
                ["iandouLit"] = new Dictionary<string, int>{["volatility"] = 100,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 10000},
                ["iandouThxFren"] = new Dictionary<string, int>{["volatility"] = 200,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 10000},
                ["iandouHype"] = new Dictionary<string, int>{["volatility"] = 300,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 10000},
                ["iandouMildPanic"] = new Dictionary<string, int>{["volatility"] = 400,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 10000},
                ["iandouScream"] = new Dictionary<string, int>{["volatility"] = 500,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 10000},
                ["iandouGlas736"] = new Dictionary<string, int>{["volatility"] = 1000,["startPrice"] = 10000,["currentPrice"] = 10000,["maxPrice"] = 100000},

                ["DoritosChip"] = new Dictionary<string, int>{["volatility"] = 1,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 5000},
                ["Kappa"] = new Dictionary<string, int>{["volatility"] = 1,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 5000},
                ["OhMyDog"] = new Dictionary<string, int>{["volatility"] = 1,["startPrice"] = 1000,["currentPrice"] = 1000,["maxPrice"] = 5000},
            };
        }

        Random rand = new Random();

        var keys = Enumerable.ToList((IEnumerable<string>)stocks.Keys);

        foreach (var key in keys)
        {
            var obj = stocks[key];
            int price = obj["currentPrice"];
            int adjustmentAmount = rand.Next(1,obj["volatility"]);
            int coinFlip = rand.Next(0, 2);
            if (coinFlip == 1) {
                price += adjustmentAmount;
            } else {
                price -= adjustmentAmount;
                if (price < 0) {
                    price = obj["startPrice"];
                }
                if (price > obj["maxPrice"]) {
                    price = (obj["maxPrice"]+obj["startPrice"])/2;
                }
            }
            stocks[key]["currentPrice"] = price;
        }

        CPH.SetGlobalVar("EmoteStockGame_prices", stocks, true);
		return true;
	}
}