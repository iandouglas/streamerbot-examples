var gameActive = CPH.GetGlobalVar("connectfour_game_active", false);
if (!gameActive)
{
    return;
}

var currentPlayer = CPH.GetGlobalVar("connectfour_current_player", "");
if (!string.IsNullOrEmpty(currentPlayer))
{
    id736.Chat.SendReplyOrMessage("connectfour-player-join", args, "Game has already started. Wait for the current round to finish.");
    return;
}

var playerGroup = "connectfour_players";
var platformUserKey = $"{args.Platform}:{args.UserId}";
var userId = args.UserId.ToString();

if (id736.Groups.IsInGroup(playerGroup, platformUserKey))
{
    id736.Chat.SendReplyOrMessage("connectfour-player-join", args, $"@{args.UserName}, you're already in the game!");
    return;
}

id736.Groups.AddUser(playerGroup, platformUserKey, userId, args.Platform);

var playerList = id736.Groups.GetGroupUsers(playerGroup);
if (playerList.Count == 1)
{
    id736.Chat.SendReplyOrMessage("connectfour-player-join", args, $"@{args.UserName} joined! Waiting for more players... 60 seconds to join.");
    return;
}

var playerNames = string.Join(", ", playerList.Select(p => $"@{p.Name}").Take(10));
if (playerList.Count > 10)
{
    id736.Chat.SendReplyOrMessage("connectfour-player-join", args, $"{playerNames}, and {playerList.Count - 10} more joined! 60 seconds to join.");
}
else
{
    id736.Chat.SendReplyOrMessage("connectfour-player-join", args, $"{playerNames} joined! 60 seconds to join.");
}
