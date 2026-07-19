using System;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Core.LinkStreamerbot(CPH);

		string currencyVariableName = "points";

		string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;

		string whoDunnit = "id736bot";
		CPH.TryGetArg("whoDunnit", out whoDunnit);

		string recipient = "everyone";
		CPH.TryGetArg("recipient", out recipient);

		string platform = "twitch";
		CPH.TryGetArg("platform", out platform);

		int pointsToAdd = 0;
		CPH.TryGetArg("pointsToAdd", out pointsToAdd);

		string cleanQuantity = pointsToAdd.ToString()
			.Trim()
			.Replace('—', '-')  // Em-dash
			.Replace('–', '-')  // En-dash
			.Replace('−', '-'); // Unicode minus sign

		if (!int.TryParse(cleanQuantity, out pointsToAdd))
		{
			id736.Chat.SendReplyOrMessage($"Sorry, that quantity doesn't look like a number.", msgId);
			return false;
		}

		if (!string.IsNullOrWhiteSpace(recipient) && recipient.ToLowerInvariant() != "everyone")
		{
			int oldPoints = id736.Points.Get(recipient, platform, currencyVariableName);
			int finalPoints = oldPoints + pointsToAdd;

			if (pointsToAdd < 0 && Math.Abs(pointsToAdd) > oldPoints)
				finalPoints = 0;

			id736.Points.Set(recipient, platform, currencyVariableName, finalPoints);

			string giveOrTake = pointsToAdd < 0 ? "took away" : "gave";
			string toOrFrom = pointsToAdd < 0 ? "from" : "to";
			int displayAmount = Math.Abs(pointsToAdd);

			string twitchEmote = id736.Chat.GetCurrentPlatform() == "twitch" ? " TheIlluminati" : "";
			string message = $"{whoDunnit} {giveOrTake} {displayAmount} {currencyVariableName} {toOrFrom} @{recipient}, they now have {finalPoints} {currencyVariableName}{twitchEmote}";
			id736.Chat.SendReplyOrMessage(message, msgId);
		}
		else
		{
			id736.Points.AddToAll(platform, currencyVariableName, pointsToAdd);

			string twitchEmote = id736.Chat.GetCurrentPlatform() == "twitch" ? " TheIlluminati" : "";
			string message = $"everyone was given {pointsToAdd} {currencyVariableName}{twitchEmote}";
			id736.Chat.SendReplyOrMessage(message, msgId);
		}

		return true;
	}
}
