using System;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Core.LinkStreamerbot(CPH);

		string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;
		string platform = id736.Chat.GetCurrentPlatform();
		string givingUsername = id736.Chat.GetCurrentUserName();

		if (string.IsNullOrWhiteSpace(givingUsername))
			givingUsername = "iandouglas736";

		if (!CPH.TryGetArg("match[2]", out string recvUsername))
		{
			id736.Chat.SendReplyOrMessage($"@{givingUsername}: Sorry I don't understand who to give points to.", msgId);
			return false;
		}

		string quantityRaw = "";
		if (!CPH.TryGetArg("match[4]", out quantityRaw))
		{
			id736.Chat.SendReplyOrMessage($"@{givingUsername}: Sorry, I couldn't understand how many points you were trying to give.", msgId);
			return false;
		}

		string cleanQuantity = quantityRaw?
			.Trim()
			.Replace('—', '-')  // Em-dash
			.Replace('–', '-')  // En-dash
			.Replace('−', '-'); // Unicode minus sign

		if (!int.TryParse(cleanQuantity, out int newPoints))
		{
			id736.Chat.SendReplyOrMessage($"@{givingUsername}: Sorry, that quantity doesn't look like a number.", msgId);
			return false;
		}

		CPH.SetArgument("whoDunnit", givingUsername);
		CPH.SetArgument("recipient", recvUsername);
		CPH.SetArgument("pointsToAdd", newPoints);
		CPH.SetArgument("platform", platform);
		CPH.RunAction("give points logic", true);

		return true;
	}
}
