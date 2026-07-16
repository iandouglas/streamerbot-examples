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
            return true;

        // Send delayed chat announcements from the last shot (hit/sunk messages)
        string pendingHitMsg = CPH.GetGlobalVar<string>("battleship_pending_chat_hit", false) ?? "";
        Log($"bomber-complete: pending_hit='{pendingHitMsg}'");
        if (!string.IsNullOrWhiteSpace(pendingHitMsg))
        {
            id736.Chat.SendMessage($"A boat is hit! All players who helped aim receive {pendingHitMsg}!");
            CPH.SetGlobalVar("battleship_pending_chat_hit", "", false);
        }
        string pendingSunkMsg = CPH.GetGlobalVar<string>("battleship_pending_chat_sunk", false) ?? "";
        Log($"bomber-complete: pending_sunk='{pendingSunkMsg}'");
        if (!string.IsNullOrWhiteSpace(pendingSunkMsg))
        {
            id736.Chat.SendMessage($"🎯 {pendingSunkMsg}");
            CPH.SetGlobalVar("battleship_pending_chat_sunk", "", false);
        }
        string pendingMineMsg = CPH.GetGlobalVar<string>("battleship_pending_chat_mine", false) ?? "";
        Log($"bomber-complete: pending_mine='{pendingMineMsg}'");
        if (!string.IsNullOrWhiteSpace(pendingMineMsg))
        {
            id736.Chat.SendMessage(pendingMineMsg);
            CPH.SetGlobalVar("battleship_pending_chat_mine", "", false);
        }

        // Check for pending game end (win or lost)
        string pendingEnd = CPH.GetGlobalVar<string>("battleship_pending_game_end", false) ?? "";
        if (!string.IsNullOrWhiteSpace(pendingEnd))
        {
            CPH.SetGlobalVar("battleship_pending_game_end", "", false);
            Log($"bomber-complete: triggering game end: {pendingEnd}");
            DoGameEnd(pendingEnd);
            return true;
        }

        // Muted players are cleared in the tick action after the muted round ends

        // Start the next round
        StartRound();
        return true;
    }

    private void StartRound()
    {
        if (!CPH.GetGlobalVar<bool>("battleship_game_active", false))
            return;

        int round = CPH.GetGlobalVar<int>("battleship_round", false) + 1;
        CPH.SetGlobalVar("battleship_round", round, false);
        CPH.SetGlobalVar("battleship_phase", "collect", false);
        CPH.SetGlobalVar("battleship_collecting", true, false);
        CPH.SetGlobalVar("battleship_coords", "[]", false);

        int roundSeconds = CPH.GetGlobalVar<int>("battleship_round_seconds", false);
        if (roundSeconds < 1) roundSeconds = 30;

        long endsAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + roundSeconds * 1000L;
        CPH.SetGlobalVar("battleship_round_ends_at", endsAt, false);

        // Get muted players (should be empty at this point, but send for state recovery)
        var mutedPlayers = id736.Data.FromJson<List<Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_muted_players", false) ?? "[]") ?? new List<Dictionary<string, object>>();

        var mutedPayload = mutedPlayers.Select(m => new Dictionary<string, object>
        {
            { "user", m["user"] },
            { "platform", m["platform"] }
        }).ToList();

        SendEvent("round-start", new Dictionary<string, object>
        {
            { "round", round },
            { "endsAt", endsAt },
            { "duration", roundSeconds },
            { "mutedPlayers", mutedPayload }
        });

        // Set the timer to fire at round end
        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, roundSeconds, keepEnabled: true);
        }

        Log($"round-start: round {round}, {roundSeconds}s");
        id736.Chat.SendMessage($"Round {round}! Enter a coordinate (e.g. B5) — you have {roundSeconds} seconds!");
    }

    private void DoGameEnd(string result)
    {
        CPH.SetGlobalVar("battleship_game_active", false, false);
        CPH.SetGlobalVar("battleship_collecting", false, false);
        CPH.SetGlobalVar("battleship_phase", "ended", false);

        // Disable coordinate command and join action
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        string coordCommandName = mode == "extreme" ? "battleship-extreme" : "battleship-normal";
        DisableCommand(coordCommandName);
        CPH.DisableAction("battleship-player-join");
        CPH.DisableAction("battleship-coord");

        // Disable timer
        string timerGuid = CPH.GetGlobalVar<string>("battleship_timer_guid", false) ?? "";
        if (!string.IsNullOrWhiteSpace(timerGuid))
            id736.Timers.Disable(timerGuid);

        // Award flawless bonus on win (normal/extreme only — easy mode has no mines so no bonus)
        if (result == "win")
        {
            string endMode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
            if (endMode != "easy")
                AwardFlawlessBonus();
        }

        // Get full board state for reveal
        string shipsJson = CPH.GetGlobalVar<string>("battleship_ships", false) ?? "[]";
        string minesJson = CPH.GetGlobalVar<string>("battleship_mines", false) ?? "[]";
        var ships = id736.Data.FromJson<List<Dictionary<string, object>>>(shipsJson) ?? new List<Dictionary<string, object>>();
        var mines = id736.Data.FromJson<List<Dictionary<string, object>>>(minesJson) ?? new List<Dictionary<string, object>>();

        // Send game-end event
        SendEvent("game-end", new Dictionary<string, object>
        {
            { "result", result },
            { "ships", ships },
            { "mines", mines }
        });

        // Announce
        string endMsg;
        if (result == "win") endMsg = "All ships sunk! Victory! Thanks for playing Battleship!";
        else if (result == "lost") endMsg = "All mines hit! The fleet is lost! Game over.";
        else endMsg = "Battleship game ended.";
        id736.Chat.SendMessage(endMsg);

        // Hide OBS source after a delay
        string obsScene = CPH.GetGlobalVar<string>("battleship_obs_scene", false) ?? "";
        string obsSource = CPH.GetGlobalVar<string>("battleship_obs_source", false) ?? "";
        CPH.Wait(15000);
        if (!string.IsNullOrWhiteSpace(obsScene) && !string.IsNullOrWhiteSpace(obsSource))
            CPH.ObsHideSource(obsScene, obsSource);

        // Cleanup
        id736.Groups.Clear(CPH.GetGlobalVar<string>("battleship_group", false) ?? "battleship-players");
        Log($"game-end: {result}");
        ClearGlobalVars();
    }

    private void AwardFlawlessBonus()
    {
        string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
        int bonus = mode == "extreme"
            ? CPH.GetGlobalVar<int>("battleship_flawless_bonus_extreme", false)
            : CPH.GetGlobalVar<int>("battleship_flawless_bonus_normal", false);
        if (bonus <= 0) return;

        int minRounds = CPH.GetGlobalVar<int>("battleship_min_rounds_for_bonus", false);
        if (minRounds < 1) minRounds = 5;

        var stats = id736.Data.FromJson<Dictionary<string, Dictionary<string, object>>>(
            CPH.GetGlobalVar<string>("battleship_player_stats", false) ?? "{}") ?? new Dictionary<string, Dictionary<string, object>>();
        var playerNames = id736.Data.FromJson<Dictionary<string, string>>(
            CPH.GetGlobalVar<string>("battleship_player_names", false) ?? "{}") ?? new Dictionary<string, string>();

        string pointsName = CPH.GetGlobalVar<string>("battleship_points_name", false) ?? "points";
        int awarded = 0;

        foreach (var kvp in stats)
        {
            int roundsPlayed = kvp.Value.TryGetValue("roundsPlayed", out object rp) ? Convert.ToInt32(rp) : 0;
            if (roundsPlayed < minRounds) continue;

            string userKey = kvp.Key;
            string displayName = playerNames.TryGetValue(userKey, out string name) ? name : userKey;
            var parts = userKey.Split(':');
            string platform = parts.Length > 0 ? parts[0] : "twitch";

            try
            {
                if (Enum.TryParse(platform, true, out Platform plat))
                {
                    id736.Points.Add(displayName, plat, pointsName, bonus);
                    awarded++;
                    Log($"flawless bonus: {displayName}@{platform} +{bonus}");
                }
            }
            catch (Exception ex)
            {
                Log($"flawless bonus error for {displayName}: {ex.Message}");
            }
        }

        id736.Chat.SendMessage($"Flawless victory bonus! {bonus} {pointsName} awarded to {awarded} players who played {minRounds}+ rounds!");
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
                    Log($"disabled command: {commandName}");
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