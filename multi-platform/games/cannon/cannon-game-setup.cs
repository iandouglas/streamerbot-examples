using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Timers.SetContext(CPH);
        CPH.LogDebug("[cannon-setup] Started.");

        if (!CPH.TryGetArg("obsScene", out string obsScene) || string.IsNullOrWhiteSpace(obsScene))
        {
            CPH.LogDebug("[cannon-setup] ERROR: obsScene action argument is missing.");
            return true;
        }

        if (!CPH.TryGetArg("obsSource", out string obsSource) || string.IsNullOrWhiteSpace(obsSource))
        {
            CPH.LogDebug("[cannon-setup] ERROR: obsSource action argument is missing.");
            return true;
        }

        if (!CPH.TryGetArg("timerName", out string timerName) || string.IsNullOrWhiteSpace(timerName))
            timerName = "cannon-game";

        if (!CPH.TryGetArg("timerGuid", out string timerGuid) || string.IsNullOrWhiteSpace(timerGuid))
        {
            CPH.LogDebug("[cannon-setup] ERROR: timerGuid action argument is missing. Copy the Timer ID from the cannon-game timer in Streamer.bot and paste it as a 'timerGuid' action argument.");
            return true;
        }

        CPH.LogDebug($"[cannon-setup] Config: scene={obsScene}, source={obsSource}, timerName={timerName}, timerGuid={timerGuid}");

        // Store config in non-persistent global variables.
        CPH.SetGlobalVar("cannon_obs_scene", obsScene, false);
        CPH.SetGlobalVar("cannon_obs_source", obsSource, false);
        CPH.SetGlobalVar("cannon_timer_name", timerName, false);
        CPH.SetGlobalVar("cannon_timer_guid", timerGuid, false);

        // Reset game state in non-persistent global variables.
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
        CPH.SetGlobalVar("cannon_queue", "[]", false);
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_timer_enabled", false, false);
        CPH.SetGlobalVar("cannon_timer_interval", 2, false);
        CPH.SetGlobalVar("cannon_last_active", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        CPH.SetGlobalVar("cannon_last_wind_update", 0L, false);
        CPH.SetGlobalVar("cannon_wind", new Random().NextDouble() * 40.0 - 20.0, false);
        CPH.SetGlobalVar("cannon_side", new Random().Next(2) == 0 ? "left" : "right", false);

        CPH.LogDebug("[cannon-setup] Game state reset.");

        // Hide the OBS source on setup so the game starts hidden.
        CPH.ObsHideSource(obsScene, obsSource);
        CPH.LogDebug("[cannon-setup] OBS source hidden. Setup finished.");

        return true;
    }
}
