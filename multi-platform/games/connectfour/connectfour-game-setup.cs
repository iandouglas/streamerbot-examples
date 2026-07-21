// connectfour-game-setup.cs
// Handles game start/stop commands and initialization

CPH.CreateCommand("!game connectfour", "!game connectfour [easy|normal|extreme] - Start a Connect Four game", async (e) =>
{
    if (e.CommandArg1 == "end")
    {
        string gameActive = CPH.GetGlobalVar("connectfour_active", "bool").ToString();
        if (gameActive == "False")
        {
            id736.Chat.SendReplyOrMessage(e, "No Connect Four game is currently active.");
            return;
        }
        
        CPH.SetGlobalVar("connectfour_active", false);
        CPH.SetGlobalVar("connectfour_difficulty", "");
        CPH.SetGlobalVar("connectfour_grid", "");
        CPH.SetGlobalVar("connectfour_turn", "");
        CPH.SetGlobalVar("connectfour_voting_results", "");
        CPH.SetGlobalVar("connectfour_voting_timer", "");
        CPH.SetGlobalVar("connectfour_player_order", "");
        
        id736.Chat.SendReplyOrMessage(e, "Connect Four game has been ended.");
        return;
    }
    
    string difficulty = e.CommandArg1;
    if (difficulty != "easy" && difficulty != "normal" && difficulty != "extreme")
    {
        id736.Chat.SendReplyOrMessage(e, "Invalid difficulty. Use: !game connectfour [easy|normal|extreme]");
        return;
    }
    
    string gameActive = CPH.GetGlobalVar("connectfour_active", "bool").ToString();
    if (gameActive == "True")
    {
        id736.Chat.SendReplyOrMessage(e, $"A Connect Four game is already active. Current difficulty: {CPH.GetGlobalVar("connectfour_difficulty", "string")}");
        return;
    }
    
    CPH.SetGlobalVar("connectfour_active", true);
    CPH.SetGlobalVar("connectfour_difficulty", difficulty);
    CPH.SetGlobalVar("connectfour_grid", "000000000000000000000000000000000000000000000000");
    CPH.SetGlobalVar("connectfour_turn", "0");
    CPH.SetGlobalVar("connectfour_voting_results", "");
    CPH.SetGlobalVar("connectfour_voting_timer", "");
    CPH.SetGlobalVar("connectfour_player_order", "");
    CPH.SetGlobalVar("connectfour_current_player_index", "0");
    CPH.SetGlobalVar("connectfour_winner", "");
    CPH.SetGlobalVar("connectfour_join_window_open", "true");
    CPH.SetGlobalVar("connectfour_voting_stage", "join");
    CPH.SetGlobalVar("connectfour_voting_column", "");
    CPH.SetGlobalVar("connectfour_voting_second_stage", "false");
    CPH.SetGlobalVar("connectfour_voting_second_timer", "");
    CPH.SetGlobalVar("connectfour_voting_second_column", "");
    
    id736.Groups.CreateGroup("connectfour_players");
    
    id736.Chat.SendReplyOrMessage(e, $"Connect Four game started with {difficulty} difficulty. Type !join to participate (60 seconds)!");
});

CPH.CreateCommand("!join", "Join a Connect Four game", async (e) =>
{
    string gameActive = CPH.GetGlobalVar("connectfour_active", "bool").ToString();
    if (gameActive != "True")
    {
        id736.Chat.SendReplyOrMessage(e, "No Connect Four game is currently active. Start one with: !game connectfour [easy|normal|extreme]");
        return;
    }
    
    string joinWindowOpen = CPH.GetGlobalVar("connectfour_join_window_open", "string");
    if (joinWindowOpen != "true")
    {
        id736.Chat.SendReplyOrMessage(e, "The join window has closed. Wait for the next game.");
        return;
    }
    
    string difficulty = CPH.GetGlobalVar("connectfour_difficulty", "string");
    string joinCount = CPH.GetGlobalVar("connectfour_join_count", "int");
    int count = string.IsNullOrEmpty(joinCount) ? 0 : int.Parse(joinCount);
    
    string platform = e.User.Platform.ToLower();
    string userId = e.User.Id;
    string userKey = $"{platform}:{userId}";
    
    if (id736.Groups.IsInGroup("connectfour_players", userKey))
    {
        id736.Chat.SendReplyOrMessage(e, "You have already joined the game.");
        return;
    }
    
    id736.Groups.AddUser("connectfour_players", userKey);
    
    count++;
    CPH.SetGlobalVar("connectfour_join_count", count.ToString());
    
    string grid = CPH.GetGlobalVar("connectfour_grid", "string");
    string playerOrder = CPH.GetGlobalVar("connectfour_player_order", "string");
    if (string.IsNullOrEmpty(playerOrder))
    {
        CPH.SetGlobalVar("connectfour_player_order", userKey);
    }
    else
    {
        CPH.SetGlobalVar("connectfour_player_order", $"{playerOrder}|{userKey}");
    }
    
    if (count == 1)
    {
        id736.Chat.SendReplyOrMessage(e, $"{e.User.DisplayName} joined! 1 player joined so far. 59 seconds remaining to join.");
    }
    else
    {
        id736.Chat.SendReplyOrMessage(e, $"{e.User.DisplayName} joined! {count} players joined so far. 59 seconds remaining to join.");
    }
});
