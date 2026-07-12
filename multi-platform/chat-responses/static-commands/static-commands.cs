using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Chat.SetContext(CPH);

        string msgId = args.ContainsKey("msgId") ? args["msgId"].ToString() : null;

        var chatResponses = CPH.GetGlobalVar<Dictionary<string, string>>("ID736StaticChatResponses", false);
        if (chatResponses == null || chatResponses.Count == 0)
        {
            id736.Chat.SendReplyOrMessage("No static commands are loaded right now.", msgId);
            return true;
        }

        List<string> commands = chatResponses.Keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.StartsWith("!") ? k : "!" + k)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (commands.Count == 0)
        {
            id736.Chat.SendReplyOrMessage("No static commands are available right now.", msgId);
            return true;
        }

        string prefix = "Static commands: ";
        int maxMessageLength = 480;

        var sb = new StringBuilder();
        sb.Append(prefix);

        for (int i = 0; i < commands.Count; i++)
        {
            string next = commands[i];
            if (i < commands.Count - 1)
                next += ", ";

            if (sb.Length + next.Length > maxMessageLength)
            {
                id736.Chat.SendMessage(sb.ToString().TrimEnd(',', ' '));
                sb.Clear();
                sb.Append(prefix);
            }

            sb.Append(next);
        }

        if (sb.Length > prefix.Length)
        {
            id736.Chat.SendMessage(sb.ToString().TrimEnd(',', ' '));
        }

        return true;
    }
}
