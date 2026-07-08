using System;
using System.Net;
using id736 = iandouglas736;

public class CPHInline
{
    private static readonly string[] _platformOrder = { "twitch", "youtube", "kick" };

    public bool Execute()
    {
        id736.Chat.SetContext(CPH);

        string userName = id736.Chat.GetCurrentUserName();
        string sourcePlatform = id736.Chat.GetCurrentPlatform();

        if (string.IsNullOrWhiteSpace(userName))
        {
            CPH.LogWarn("[DadJoke] could not determine username");
            return false;
        }

        string joke = FetchRandomJoke();
        if (string.IsNullOrWhiteSpace(joke))
        {
            id736.Chat.SendMessage("Sorry, the dad joke machine is out of order right now.");
            return false;
        }

        // Relay a "someone wants a dad joke" message to all OTHER platforms.
        foreach (string targetPlatform in _platformOrder)
        {
            if (targetPlatform == sourcePlatform)
                continue;

            string relay = $"{userName} from {sourcePlatform} wants a dad joke!";
            id736.Chat.SendMessageTo(targetPlatform, relay);
        }

        // Send the actual joke to ALL platforms (including the source).
        foreach (string targetPlatform in _platformOrder)
        {
            id736.Chat.SendMessageTo(targetPlatform, joke);
        }

        return true;
    }

    private string FetchRandomJoke()
    {
        string response = "";
        try
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("Accept", "text/plain");
                response = client.DownloadString("https://dadjokes736.com/").Trim();
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"[DadJoke] failed to fetch joke: {ex.Message}");
            response = "no joke -- literally ... ¯\\_(ツ)_/¯";
        }
        return response;
    }
}
