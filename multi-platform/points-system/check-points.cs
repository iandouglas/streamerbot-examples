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
			CPH.LogWarn("[Points] could not determine username");
			return false;
		}

		int balance = id736.Points.GetOrInit(userName, platform, "points");

		string message = $"@{userName} has {balance} points";
		id736.Chat.SendReplyOrMessage(message, msgId);
		return true;
	}
}
