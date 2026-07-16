using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
	public bool Execute()
	{
		id736.Core.LinkStreamerbot(CPH);

		string cmd = args.ContainsKey("command") ? args["command"].ToString() : "";
		string userName = id736.Chat.GetCurrentUserName();
		string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;
		string platform = id736.Chat.GetCurrentPlatform();

		if (string.IsNullOrWhiteSpace(userName))
		{
			id736.Log.Message("could not determine username", filenamePrefix: "lurk");
			return false;
		}

		string msg = null;
		var rand = new Random();

		if (cmd == "!lurk")
		{
			int roll = CPH.Between(1, 20);
			int lurkValue = CPH.Between(1, 20);
			if (roll == 20)
			{
				msg = $"{userName} has rolled a nat 20 for their stealth check and ... wait, where did they go?!";
			}
			else if (roll == 1)
			{
				msg = $"{userName} has rolled a ONE for their stealth check and ... CRITICAL FAIL ... they attach jingle bells to their armor and pull out their tambourine, while trying to tiptoe away on a sheet of bubblewrap they themselves lay out in front of their feet.";
			}
			else
			{
				msg = $"{userName} rolled {roll} and DM rolled {lurkValue}";

				if (roll > lurkValue)
				{
					msg = $"{msg}. {userName} has passed the stealth check and fades into the background.";
				}
				else
				{
					msg = $"{msg}. {userName} failed their stealth check.";
				}
			}
		}
		else if (cmd == "!unlurk")
		{
			List<string> msgs;

			if (platform == "twitch")
			{
				msgs = new List<string>() {
					$"DinoDance -- {userName} is back in chat -- horray!",
					$"SUBprise -- {userName} is back in chat -- horray!",
					$"KAPOW -- {userName} is back in chat -- horray!",
					$"OhMyDog -- {userName} is back in chat -- horray!",
				};
			}
			else
			{
				msgs = new List<string>() {
					$"Everyone!! {userName} is back in chat -- hooray!",
					$"Look who's back -- it's {userName}! Welcome back!",
					$"{userName} has returned to chat!",
					$"A wild {userName} appeared in chat! They used Charm -- it's highly effective.",
				};
			}

			msg = msgs[rand.Next(msgs.Count)];
		}

		if (!string.IsNullOrEmpty(msg))
		{
			id736.Chat.SendReplyOrMessage(msg, msgId);
		}

		return true;
	}
}
