using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        if (!CPH.TryGetArg("obsScene", out string obsScene) || string.IsNullOrWhiteSpace(obsScene))
            return true;

        if (!CPH.TryGetArg("obsSource", out string obsSource) || string.IsNullOrWhiteSpace(obsSource))
            return true;

        if (!CPH.TryGetArg("timerGuid", out string timerGuid) || string.IsNullOrWhiteSpace(timerGuid))
            return true;

        // Store config in non-persistent global variables.
        CPH.SetGlobalVar("cannon_obs_scene", obsScene, false);
        CPH.SetGlobalVar("cannon_obs_source", obsSource, false);
        CPH.SetGlobalVar("cannon_timer_guid", timerGuid, false);

        // Reset game state in non-persistent global variables.
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
        CPH.SetGlobalVar("cannon_queue", "[]", false);
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_firing_started", 0L, false);
        CPH.SetGlobalVar("cannon_next_fire_at", 0L, false);
        CPH.SetGlobalVar("cannon_current_shot_id", 0, false);
        CPH.SetGlobalVar("cannon_timer_enabled", false, false);
        CPH.SetGlobalVar("cannon_timer_interval", 1, false);
        CPH.SetGlobalVar("cannon_last_active", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        CPH.SetGlobalVar("cannon_last_wind_update", 0L, false);
        CPH.SetGlobalVar("cannon_wind", new Random().NextDouble() * 40.0 - 20.0, false);
        CPH.SetGlobalVar("cannon_side", new Random().Next(2) == 0 ? "left" : "right", false);
        CPH.SetGlobalVar("cannon_source_shown_at", 0L, false);

        // Hide the OBS source on setup so the game starts hidden.
        CPH.ObsHideSource(obsScene, obsSource);

        return true;
    }
}
