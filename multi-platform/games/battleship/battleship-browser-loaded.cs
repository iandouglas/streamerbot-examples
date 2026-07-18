using System;
using System.Collections.Generic;
using System.Linq;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        try
        {
            id736.Core.LinkStreamerbot(CPH);
            id736.Log.Message("Browser loaded/reloaded", filenamePrefix: "battleship");

            CPH.SetGlobalVar("battleship_browser_loaded_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);

            // If a game is active, resend the setup event so the browser can reconstruct state
            if (CPH.GetGlobalVar<bool>("battleship_game_active", false))
            {
                id736.Log.Message("Game is active, resending setup event to browser", filenamePrefix: "battleship");

                string mode = CPH.GetGlobalVar<string>("battleship_mode", false) ?? "normal";
                int gridSize = CPH.GetGlobalVar<int>("battleship_grid_size", false);
                if (gridSize < 1) gridSize = 10;
                string phase = CPH.GetGlobalVar<string>("battleship_phase", false) ?? "setup";

                string shipsJson = CPH.GetGlobalVar<string>("battleship_ships", false) ?? "[]";
                string minesJson = CPH.GetGlobalVar<string>("battleship_mines", false) ?? "[]";
                var ships = id736.Data.FromJson<List<Dictionary<string, object>>>(shipsJson) ?? new List<Dictionary<string, object>>();
                var mines = id736.Data.FromJson<List<Dictionary<string, object>>>(minesJson) ?? new List<Dictionary<string, object>>();

                var payload = new Dictionary<string, object>
                {
                    { "event", "setup" },
                    { "mode", mode },
                    { "gridSize", gridSize },
                    { "round", CPH.GetGlobalVar<int>("battleship_round", false) },
                    { "phase", phase },
                    { "joinEndsAt", CPH.GetGlobalVar<long>("battleship_join_ends_at", false) },
                    { "endsAt", CPH.GetGlobalVar<long>("battleship_round_ends_at", false) },
                    { "roundSeed", CPH.GetGlobalVar<int>("battleship_round_seed", false) },
                    { "ships", ships },
                    { "mines", mines },
                    { "playAudio", false }
                };
                string json = id736.Data.ToJson(payload);
                CPH.WebsocketBroadcastJson(json);
                id736.Log.Message($"Setup event resent ({json.Length} bytes)", filenamePrefix: "battleship");
            }
            else
            {
                id736.Log.Message("No active game, browser loaded but not sending setup", filenamePrefix: "battleship");
            }
        }
        catch (Exception ex)
        {
            CPH.LogError($"[battleship-browser-loaded] Error: {ex.Message}");
        }

        return true;
    }
}
