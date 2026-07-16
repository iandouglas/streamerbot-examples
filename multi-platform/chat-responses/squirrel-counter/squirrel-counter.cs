using System;
using id736 = iandouglas736;

public class CPHInline
{
    private const string CounterVarName = "ID736SquirrelCount";

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        string userName = id736.Chat.GetCurrentUserName();
        if (string.IsNullOrWhiteSpace(userName))
        {
            id736.Log.Message("could not determine username", filenamePrefix: "squirrel");
            return false;
        }

        // Increment the persisted global counter.
        int count = CPH.GetGlobalVar<int>(CounterVarName, true);
        count++;
        CPH.SetGlobalVar(CounterVarName, count, true);

        string platform = id736.Chat.GetCurrentPlatform();
        string twitchEmote = platform == "twitch" ? "SabaPing " : "";
        string message = $"{twitchEmote}{userName} has spotted a squirrel! We have seen {count} squirrel{(count == 1 ? "" : "s")} so far!";

        id736.Chat.SendMessage(message);
        return true;
    }
}
