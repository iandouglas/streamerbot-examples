using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
	private const string PricesVarName = "EmoteStockGame_prices";

	public bool Execute()
	{
		string json = CPH.GetGlobalVar<string>(PricesVarName, true);
		var stocks = id736.Data.FromJson<Dictionary<string, Dictionary<string, int>>>(json)
			?? BuildDefaultStocks();

		var rand = new Random();

		foreach (var key in stocks.Keys.ToList())
		{
			var obj = stocks[key];
			int price = obj["currentPrice"];
			int adjustmentAmount = rand.Next(1, obj["volatility"]);
			int coinFlip = rand.Next(0, 2);

			if (coinFlip == 1)
			{
				price += adjustmentAmount;
			}
			else
			{
				price -= adjustmentAmount;
				if (price < 0)
					price = obj["startPrice"];
			}

			if (price > obj["maxPrice"])
				price = (obj["maxPrice"] + obj["startPrice"]) / 2;

			obj["currentPrice"] = price;
		}

		CPH.SetGlobalVar(PricesVarName, id736.Data.ToJson(stocks), true);
		return true;
	}

	private Dictionary<string, Dictionary<string, int>> BuildDefaultStocks()
	{
		return new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
		{
			["iandouDeadpoolHeart"] = new Dictionary<string, int> { ["volatility"] = 50, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 10000 },
			["iandouLit"] = new Dictionary<string, int> { ["volatility"] = 100, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 10000 },
			["iandouThxFren"] = new Dictionary<string, int> { ["volatility"] = 200, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 10000 },
			["iandouHype"] = new Dictionary<string, int> { ["volatility"] = 300, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 10000 },
			["iandouMildPanic"] = new Dictionary<string, int> { ["volatility"] = 400, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 10000 },
			["iandouScream"] = new Dictionary<string, int> { ["volatility"] = 500, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 10000 },
			["iandouGlas736"] = new Dictionary<string, int> { ["volatility"] = 1000, ["startPrice"] = 10000, ["currentPrice"] = 10000, ["maxPrice"] = 100000 },

			["DoritosChip"] = new Dictionary<string, int> { ["volatility"] = 1, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 5000 },
			["Kappa"] = new Dictionary<string, int> { ["volatility"] = 1, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 5000 },
			["OhMyDog"] = new Dictionary<string, int> { ["volatility"] = 1, ["startPrice"] = 1000, ["currentPrice"] = 1000, ["maxPrice"] = 5000 },
		};
	}
}
