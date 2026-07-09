using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    private const double MinWindChange = 0.1;
    private const double MaxWindChange = 1.5;
    private const double WindMin = -20.0;
    private const double WindMax = 20.0;
    private const long WindUpdateMs = 7000;
    private const long InactivityHideMs = 60 * 1000; // 60 seconds
    private static readonly Random _random = new Random();

    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Timers.SetContext(CPH);

        CPH.LogDebug("[cannon-tick] Started.");

        // Capture this timer's GUID on the first run. The timer must be enabled by name
        // at least once (via CPH.EnableTimer) so it fires and we can learn its GUID.
        string timerGuid = CPH.GetGlobalVar<string>("cannon_timer_guid", false) ?? "";
        if (string.IsNullOrWhiteSpace(timerGuid) && CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId))
        {
            timerGuid = currentTimerId.ToString();
            CPH.SetGlobalVar("cannon_timer_guid", timerGuid, false);
            CPH.LogDebug($"[cannon-tick] Stored timer GUID: {timerGuid}");
        }

        // Only run when the timer has been enabled by a player joining.
        bool timerEnabled = CPH.GetGlobalVar<bool>("cannon_timer_enabled", false);
        int desiredInterval = CPH.GetGlobalVar<int>("cannon_timer_interval", false);
        CPH.LogDebug($"[cannon-tick] enabled={timerEnabled}, desiredInterval={desiredInterval}s, guid={timerGuid}");

        if (!timerEnabled)
        {
            CPH.LogDebug("[cannon-tick] Timer not enabled yet. Disabling self and exiting.");
            if (!string.IsNullOrWhiteSpace(timerGuid))
                id736.Timers.Disable(timerGuid);
            return true;
        }

        // Apply any requested interval change.
        if (desiredInterval > 0 && !string.IsNullOrWhiteSpace(timerGuid))
        {
            CPH.LogDebug($"[cannon-tick] Resetting timer '{timerGuid}' to {desiredInterval}s via DLL.");
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, desiredInterval, keepEnabled: true);
            CPH.SetGlobalVar("cannon_timer_interval", 0, false);
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // --- setup ---
        bool setupSent = CPH.GetGlobalVar<bool>("cannon_setup_sent", false);
        CPH.LogDebug($"[cannon-tick] cannon_setup_sent={setupSent}");
        if (!setupSent)
        {
            CPH.LogDebug("[cannon-tick] Sending setup event.");
            SendSetup();
            CPH.SetGlobalVar("cannon_setup_sent", true, false);
            CPH.SetGlobalVar("cannon_last_wind_update", now, false);
            CPH.SetGlobalVar("cannon_last_active", now, false);
            CPH.SetGlobalVar("cannon_wind", _random.NextDouble() * 40.0 - 20.0, false);
            CPH.SetGlobalVar("cannon_side", _random.Next(2) == 0 ? "left" : "right", false);
            CPH.SetGlobalVar("cannon_firing", false, false);
        }

        // --- wind update every 7 seconds ---
        long lastWindUpdate = CPH.GetGlobalVar<long>("cannon_last_wind_update", false);
        if (now - lastWindUpdate >= WindUpdateMs)
        {
            double wind = CPH.GetGlobalVar<double>("cannon_wind", false);
            double change = _random.NextDouble() * (MaxWindChange - MinWindChange) + MinWindChange;
            if (_random.Next(2) == 0) change = -change;
            wind = Clamp(wind + change, WindMin, WindMax);
            CPH.SetGlobalVar("cannon_wind", wind, false);
            CPH.SetGlobalVar("cannon_last_wind_update", now, false);

            CPH.LogDebug($"[cannon-tick] Wind updated to {wind:F1}.");
            SendEvent("wind", new Dictionary<string, object> { { "wind", wind } });
        }

        // --- queue management ---
        string queueJson = CPH.GetGlobalVar<string>("cannon_queue", false) ?? "[]";
        var queue = id736.Data.JsonToNestedList(queueJson) ?? new List<object>();
        CPH.LogDebug($"[cannon-tick] Queue count={queue.Count}, queueJson={queueJson}");

        if (queue.Count > 0)
        {
            // Tell the browser the current queue order.
            CPH.LogDebug("[cannon-tick] Broadcasting queue.");
            SendEvent("queue", new Dictionary<string, object> { { "players", queue } });

            // Fire the first player if the browser is not currently busy.
            bool firing = CPH.GetGlobalVar<bool>("cannon_firing", false);
            CPH.LogDebug($"[cannon-tick] cannon_firing={firing}");
            if (!firing)
            {
                var player = queue[0] as Dictionary<string, object>;
                queue.RemoveAt(0);
                CPH.SetGlobalVar("cannon_queue", id736.Data.ToJson(queue), false);
                CPH.SetGlobalVar("cannon_firing", true, false);

                CPH.LogDebug($"[cannon-tick] Firing player: {id736.Data.ToJson(player)}");
                SendEvent("fire", new Dictionary<string, object> { { "player", player } });
            }
        }

        // --- auto-hide after inactivity ---
        long lastActive = CPH.GetGlobalVar<long>("cannon_last_active", false);
        long inactiveMs = now - lastActive;
        CPH.LogDebug($"[cannon-tick] lastActiveMs={inactiveMs}, InactivityHideMs={InactivityHideMs}");
        if (queue.Count == 0 && inactiveMs >= InactivityHideMs)
        {
            CPH.LogDebug("[cannon-tick] Inactivity threshold reached. Hiding game.");
            HideGameSource();
            DisableTimer(timerGuid);
        }

        CPH.LogDebug("[cannon-tick] Finished.");
        return true;
    }

    private void SendSetup()
    {
        string side = CPH.GetGlobalVar<string>("cannon_side", false) ?? "left";
        double wind = CPH.GetGlobalVar<double>("cannon_wind", false);
        var targetX = 960 + _random.Next(-600, 600);

        CPH.LogDebug($"[cannon-tick] SendSetup side={side}, wind={wind:F1}, targetX={targetX}");

        // Default audio paths are relative to index.html so local file:// loads work.
        string audioFuse = "assets/sounds/fuse.mp3";
        string audioFire = "assets/sounds/cannon-fire.mp3";
        string audioImpact = "assets/sounds/land-clang.mp3";

        if (CPH.TryGetArg("audioFuse", out string fuseArg) && !string.IsNullOrWhiteSpace(fuseArg))
            audioFuse = fuseArg;
        if (CPH.TryGetArg("audioFire", out string fireArg) && !string.IsNullOrWhiteSpace(fireArg))
            audioFire = fireArg;
        if (CPH.TryGetArg("audioImpact", out string impactArg) && !string.IsNullOrWhiteSpace(impactArg))
            audioImpact = impactArg;

        var payload = new Dictionary<string, object>
        {
            { "cannonSide", side },
            { "cannonAngle", 45 },
            { "targetX", targetX },
            { "targetY", 980 },
            { "wind", wind },
            { "audio", new Dictionary<string, object>
                {
                    { "fuse", audioFuse },
                    { "fire", audioFire },
                    { "impact", audioImpact }
                }
            }
        };

        SendEvent("setup", payload);
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        // Streamer.bot automatically wraps the broadcast JSON in a General.Custom
        // envelope before sending it to websocket clients. We only need to send
        // the raw payload, and the browser reads it from payload.data.
        data["event"] = eventName;
        string json = id736.Data.ToJson(data);
        CPH.LogDebug($"[cannon-tick] Broadcasting event {eventName}: {json}");
        CPH.WebsocketBroadcastJson(json);
    }

    private void HideGameSource()
    {
        string scene = CPH.GetGlobalVar<string>("cannon_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("cannon_obs_source", false) ?? "";
        CPH.LogDebug($"[cannon-tick] HideGameSource scene={scene}, source={source}");
        if (!string.IsNullOrWhiteSpace(scene) && !string.IsNullOrWhiteSpace(source))
        {
            CPH.ObsHideSource(scene, source);
        }
    }

    private void DisableTimer(string timerGuid)
    {
        CPH.LogDebug($"[cannon-tick] DisableTimer guid={timerGuid}");
        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Disable(timerGuid);
        }

        CPH.SetGlobalVar("cannon_timer_enabled", false, false);
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
        CPH.LogDebug("[cannon-tick] Timer disabled and setup flag reset.");
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
