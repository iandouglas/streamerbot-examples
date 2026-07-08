using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        CPH.LogDebug("got here");

        id736.Chat.SetContext(CPH);

        var chatResponses = CPH.GetGlobalVar<Dictionary<string, string>>("ID736StaticChatResponses", false);
        if (chatResponses == null || chatResponses.Count == 0)
        {
            CPH.LogDebug("no responses to send back");
            return false;
        }

        string command = args["rawInput"].ToString().ToLower().Trim().Remove(0, 1);

        if (chatResponses.ContainsKey(command))
        {
            string response = chatResponses[command];
            CPH.LogDebug($"platform: {id736.Chat.GetCurrentPlatform()}");

            // add an emote at the start if we're on Twitch
            string platform = id736.Chat.GetCurrentPlatform();
            if (platform == "twitch")
                response = $"TwitchSings {response}";

            id736.Chat.SendMessage(response);
        }

        return true;
    }
}