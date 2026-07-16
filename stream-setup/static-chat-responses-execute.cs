using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        id736.Log.Message("got here", filenamePrefix: "staticchat");

        var chatResponses = CPH.GetGlobalVar<Dictionary<string, string>>("ID736StaticChatResponses", false);
        if (chatResponses == null || chatResponses.Count == 0)
        {
            id736.Log.Message("no responses to send back", filenamePrefix: "staticchat");
            return false;
        }

        string command = args["rawInput"].ToString().ToLower().Trim().Remove(0, 1);

        if (chatResponses.ContainsKey(command))
        {
            string response = chatResponses[command];
            id736.Log.Message($"platform: {id736.Chat.GetCurrentPlatform()}", filenamePrefix: "staticchat");

            // add an emote at the start if we're on Twitch
            string platform = id736.Chat.GetCurrentPlatform();
            if (platform == "twitch")
                response = $"TwitchSings {response}";

            id736.Chat.SendMessage(response);
        }

        return true;
    }
}