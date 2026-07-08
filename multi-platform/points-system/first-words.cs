using System;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Chat.SetContext(CPH);
		id736.Points.SetContext(CPH);

		string userName = id736.Chat.GetCurrentUserName();
		string platform = id736.Chat.GetCurrentPlatform();
		string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;

		if (string.IsNullOrWhiteSpace(userName))
		{
			CPH.LogWarn("[FirstWords] could not determine username");
			return false;
		}

		// Ensure the user has a points balance (starts at 0).
		id736.Points.GetOrInit(userName, platform, "points");

		int pointsToAdd = 0;
		if (!CPH.TryGetArg("pointsToAdd", out pointsToAdd))
			pointsToAdd = 100;

		string platformEmote = platform == "twitch" ? "TwitchSings " : "";
		string welcomeMessage = $"{platformEmote}Welcome to the stream, have some points!";
		id736.Chat.SendReplyOrMessage(welcomeMessage, msgId);

		// Hand off to the shared give-points logic action.
		CPH.SetArgument("whoDunnit", "id736bot");
		CPH.SetArgument("recipient", userName);
		CPH.SetArgument("pointsToAdd", pointsToAdd);
		CPH.SetArgument("platform", platform);
		CPH.RunAction("give points logic", true);

		return true;
	}
}
