using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
    private void Log(string msg)
    {
        id736.Log.Message(msg, filenamePrefix: "battleship");
    }

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
        {
            id736.Chat.SendMessage("No Battleship game is currently running.");
            return true;
        }

        Log("game-end: streamer ended game early");
        DoGameEnd("ended");
        return true;
    }

    private void DoGameEnd(string result)
    {
        CPH.SetGlobalVar("battleship_game_active", false, false);
        CPH.SetGlobalVar("battleship_collecting", false, false);
        CPH.SetGlobalVar("battleship_phase", "ended", false);

        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        string coordCommandName = mode == "extreme" ? "battleship-extreme" : "battleship-normal";
        DisableCommand(coordCommandName);
        CPH.DisableAction("battleship-player-join");
        CPH.DisableAction("battleship-coord");

        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
            id736.Timers.Disable(timerGuid);

        string shipsJson = CPH.GetGlobalVar<string>("battleship_ships", false) ?? "[]";
        string minesJson = CPH.GetGlobalVar<string>("battleship_mines", false) ?? "[]";
        var ships = id736.Data.FromJson<List<Dictionary<string, object>>>(shipsJson) ?? new List<Dictionary<string, object>>();
        var mines = id736.Data.FromJson<List<Dictionary<string, object>>>(minesJson) ?? new List<Dictionary<string, object>>();

        SendEvent("game-end", new Dictionary<string, object>
        {
            { "result", result },
            { "ships", ships },
            { "mines", mines }
        });

        id736.Chat.SendMessage("Battleship game ended by the streamer. The fleet returns to base.");

        string obsScene = CPH.GetGlobalVar<string>("battleship_obs_scene", false) ?? "";
        string obsSource = CPH.GetGlobalVar<string>("battleship_obs_source", false) ?? "";
        CPH.Wait(15000);
        if (!string.IsNullOrWhiteSpace(obsScene) && !string.IsNullOrWhiteSpace(obsSource))
            CPH.ObsHideSource(obsScene, obsSource);

        id736.Groups.Clear(CPH.GetGlobalVar<string>("battleship_group", false) ?? "battleship-players");
        Log($"game-end: {result}");
        ClearGlobalVars();
    }

    private void DisableCommand(string commandName)
    {
        var commands = CPH.GetCommands();
        if (commands == null) return;
        foreach (var cmd in commands)
        {
            if (cmd == null) continue;
            var type = cmd.GetType();
            string name = type.GetProperty("Name")?.GetValue(cmd, null)?.ToString() ?? "";
            if (name == commandName)
            {
                string id = type.GetProperty("Id")?.GetValue(cmd, null)?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(id))
                {
                    CPH.DisableCommand(id);
                    return;
                }
            }
        }
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.WebsocketBroadcastJson(json);
    }

    private void ClearGlobalVars()
    {
        string[] vars = {
            "battleship_game_active", "battleship_mode", "battleship_phase",
            "battleship_group", "battleship_round", "battleship_collecting",
            "battleship_coords", "battleship_shots", "battleship_muted_players",
            "battleship_player_stats", "battleship_player_names",
            "battleship_mines_hit", "battleship_ships_sunk",
            "battleship_grid_size", "battleship_round_seconds",
            "battleship_inter_round_seconds", "battleship_points_name",
            "battleship_hit_points_easy", "battleship_hit_points_normal",
            "battleship_hit_points_extreme", "battleship_mine_penalty",
            "battleship_flawless_bonus_normal", "battleship_flawless_bonus_extreme",
            "battleship_min_rounds_for_bonus", "battleship_normal_mines",
            "battleship_extreme_multiplier", "battleship_ships", "battleship_mines",
            "battleship_obs_scene", "battleship_obs_source", "battleship_timer_guid",
            "battleship_round_ends_at", "battleship_pending_game_end",
            "battleship_pending_chat_hit", "battleship_pending_chat_sunk", "battleship_pending_chat_mine"
        };
        foreach (string v in vars)
            CPH.UnsetGlobalVar(v, false);
    }
}