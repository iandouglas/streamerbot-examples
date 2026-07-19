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
    private const long SourceWarmupMs = 3000;
    private const long FiringTimeoutMs = 10000;
    private const long BetweenShotsMs = 3000;
    private static readonly Random _random = new Random();

    public bool Execute()
    {
        id736.Core.LinkStreamerbot(CPH);

        // Capture this timer's GUID on the first run. The timer must be enabled by name
        // at least once (via CPH.EnableTimer) so it fires and we can learn its GUID.
        string timerGuid = CPH.GetGlobalVar<string>("cannon_timer_guid", false) ?? "";
        if (string.IsNullOrWhiteSpace(timerGuid) && CPH.TryGetArg<Guid>("timerId", out Guid currentTimerId))
        {
            timerGuid = currentTimerId.ToString();
            CPH.SetGlobalVar("cannon_timer_guid", timerGuid, false);
        }

        // Only run when the timer has been enabled by a player joining.
        bool timerEnabled = CPH.GetGlobalVar<bool>("cannon_timer_enabled", false);
        int desiredInterval = CPH.GetGlobalVar<int>("cannon_timer_interval", false);

        if (!timerEnabled)
        {
            if (!string.IsNullOrWhiteSpace(timerGuid))
                id736.Timers.Disable(timerGuid);
            return true;
        }

        // Apply any requested interval change.
        if (desiredInterval > 0 && !string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, desiredInterval, keepEnabled: true);
            CPH.SetGlobalVar("cannon_timer_interval", 0, false);
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long nextFireAt = CPH.GetGlobalVar<long>("cannon_next_fire_at", false);

        // --- setup ---
        bool setupSent = CPH.GetGlobalVar<bool>("cannon_setup_sent", false);
        if (!setupSent)
        {
            SendSetup();
            CPH.SetGlobalVar("cannon_setup_sent", true, false);
            CPH.SetGlobalVar("cannon_last_wind_update", now, false);
            CPH.SetGlobalVar("cannon_last_active", now, false);
            CPH.SetGlobalVar("cannon_wind", _random.NextDouble() * 40.0 - 20.0, false);
            CPH.SetGlobalVar("cannon_side", _random.Next(2) == 0 ? "left" : "right", false);
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

            SendEvent("wind", new Dictionary<string, object> { { "wind", wind } });
        }

        // --- queue management ---
        // If the browser reloaded recently, clear any stale queue so old names don't reappear.
        long browserLoadedAt = CPH.GetGlobalVar<long>("cannon_browser_loaded_at", false);
        long msSinceBrowserLoad = now - browserLoadedAt;
        if (browserLoadedAt > 0 && msSinceBrowserLoad >= 0 && msSinceBrowserLoad < 2000)
        {
            CPH.SetGlobalVar("cannon_queue", "[]", false);
            CPH.SetGlobalVar("cannon_browser_loaded_at", 0L, false);
        }

        string queueJson = CPH.GetGlobalVar<string>("cannon_queue", false) ?? "[]";
        var queue = id736.Data.JsonToNestedList(queueJson) ?? new List<object>();

        // Server-side safety: if the browser never reported the shot ending, reset
        // the firing flag after a long timeout so the queue can continue. Also recover
        // from a stuck flag that has no start timestamp.
        bool firing = CPH.GetGlobalVar<bool>("cannon_firing", false);
        long firingStarted = CPH.GetGlobalVar<long>("cannon_firing_started", false);
        bool firingTimedOut = firing && firingStarted > 0 && (now - firingStarted) > FiringTimeoutMs;
        bool firingStuckNoTimestamp = firing && firingStarted == 0;
        if (firingTimedOut || firingStuckNoTimestamp)
        {
            id736.Chat.SendMessageToAllPlatforms("hmm, the cannon seems jammed, let's move to next player");
            firing = false;
            CPH.SetGlobalVar("cannon_firing", false, false);
            CPH.SetGlobalVar("cannon_firing_started", 0L, false);
            CPH.SetGlobalVar("cannon_current_shot_id", 0, false);
            CPH.SetGlobalVar("cannon_next_fire_at", now + BetweenShotsMs, false);
            nextFireAt = now + BetweenShotsMs;
        }

        if (queue.Count > 0)
        {
            // Fire the first player if the browser is not currently busy.
            if (!firing && now >= nextFireAt)
            {
                // If the OBS source was just shown, give the browser a moment to load
                // and connect before firing. This prevents the first shot from being
                // lost when the source was previously hidden.
                long sourceShownAt = CPH.GetGlobalVar<long>("cannon_source_shown_at", false);
                long msSinceShown = now - sourceShownAt;
                if (sourceShownAt > 0 && msSinceShown < SourceWarmupMs)
                {
                    // Wait for the source to finish loading/appearing.
                }
                else
                {
                    var player = queue[0] as Dictionary<string, object>;
                    queue.RemoveAt(0);
                    CPH.SetGlobalVar("cannon_queue", id736.Data.ToJson(queue), false);
                    CPH.SetGlobalVar("cannon_firing", true, false);
                    CPH.SetGlobalVar("cannon_firing_started", now, false);
                    int shotId = CPH.GetGlobalVar<int>("cannon_current_shot_id", false) + 1;
                    CPH.SetGlobalVar("cannon_current_shot_id", shotId, false);
                    CPH.SetGlobalVar("cannon_next_fire_at", 0L, false);
                    CPH.SetGlobalVar("cannon_source_shown_at", 0L, false);

                    if (player != null && player.TryGetValue("name", out object nameObj) && nameObj != null)
                    {
                        id736.Chat.SendMessageToAllPlatforms($"{nameObj} climbs into the cannon...");
                    }

                    SendEvent("fire", new Dictionary<string, object>
                    {
                        { "player", player },
                        { "shotId", shotId }
                    });
                }
            }
        }

        // --- auto-hide after inactivity ---
        long lastActive = CPH.GetGlobalVar<long>("cannon_last_active", false);
        long inactiveMs = now - lastActive;
        if (queue.Count == 0 && !firing && inactiveMs >= InactivityHideMs)
        {
            HideGameSource();
            DisableTimer(timerGuid);
        }

        return true;
    }

    private void SendSetup()
    {
        string side = CPH.GetGlobalVar<string>("cannon_side", false) ?? "left";
        double wind = CPH.GetGlobalVar<double>("cannon_wind", false);
        var targetX = 960 + _random.Next(-600, 600);

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
            { "targetY", 1062 },
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
        CPH.WebsocketBroadcastJson(json);
    }

    private void HideGameSource()
    {
        string scene = CPH.GetGlobalVar<string>("cannon_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("cannon_obs_source", false) ?? "";
        if (!string.IsNullOrWhiteSpace(scene) && !string.IsNullOrWhiteSpace(source))
        {
            CPH.ObsHideSource(scene, source);
        }
    }

    private void DisableTimer(string timerGuid)
    {
        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            id736.Timers.Disable(timerGuid);
        }

        CPH.SetGlobalVar("cannon_timer_enabled", false, false);
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
