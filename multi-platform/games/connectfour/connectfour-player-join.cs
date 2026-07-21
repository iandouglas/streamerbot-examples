using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "connectfour");
    }

    private string MakeUserKey(string platform, string userId)
    {
        return $"{platform.ToLowerInvariant()}:{userId}";
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("user", out string user) || string.IsNullOrWhiteSpace(user))
            return false;
        if (!CPH.TryGetArg("userName", out string userName))
            userName = user;
        if (!CPH.TryGetArg("userId", out string userId) || string.IsNullOrWhiteSpace(userId))
            userId = userName;
        if (!CPH.TryGetArg("userType", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        object msgIdObj;
        string msgId = CPH.TryGetArg("msgId", out msgIdObj) ? msgIdObj?.ToString() : null;

        bool gameActive = CPH.GetGlobalVar<bool>("connectfour_game_active", false);
        if (!gameActive)
        {
            id736.Chat.SendReplyOrMessage("No Connect Four game is currently active. Start one with: !game connectfour [easy|normal|extreme]", msgId);
            return false;
        }

        string phase = CPH.GetGlobalVar<string>("connectfour_phase", false) ?? "";
        if (phase != "join")
        {
            id736.Chat.SendReplyOrMessage("The join window has already closed. Wait for the next game.", msgId);
            return false;
        }

        bool joinWindowOpen = CPH.GetGlobalVar<bool>("connectfour_join_window_open", false);
        if (!joinWindowOpen)
        {
            id736.Chat.SendReplyOrMessage("The join window has closed. Wait for the next game.", msgId);
            return false;
        }

        string groupName = "connectfour_players";

        if (id736.Groups.IsInGroup(userName, groupName))
        {
            id736.Chat.SendReplyOrMessage("You have already joined the game.", msgId);
            return false;
        }

        id736.Groups.AddUser(userName, platform, groupName);

        string userKey = MakeUserKey(platform, userId);

        int joinCount = CPH.GetGlobalVar<int>("connectfour_join_count", false) + 1;
        CPH.SetGlobalVar("connectfour_join_count", joinCount, false);

        string playerOrder = CPH.GetGlobalVar<string>("connectfour_player_order", false) ?? "";
        if (string.IsNullOrEmpty(playerOrder))
            CPH.SetGlobalVar("connectfour_player_order", userKey, false);
        else
            CPH.SetGlobalVar("connectfour_player_order", $"{playerOrder}|{userKey}", false);

        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("connectfour_player_names", false) ?? "{}")
            ?? new Dictionary<string, string>();
        playerNames[userKey] = userName;
        CPH.SetGlobalVar("connectfour_player_names", id736.Data.ToJson(playerNames), false);

        Log($"player-join: {userName} ({userKey}) joined, count={joinCount}");

        id736.Chat.SendReplyOrMessage($"{userName} joined! {joinCount} player(s) joined so far.", msgId);

        SendEvent("player-joined", new Dictionary<string, object>
        {
            { "userKey", userKey },
            { "displayName", userName },
            { "joinCount", joinCount }
        });

        return true;
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
    }
}