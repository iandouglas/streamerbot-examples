using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Timers.SetContext(CPH);
        CPH.LogDebug("[cannon-fire] Started.");

        // Try the rawInput argument.
        string rawInput = "";
        if (!CPH.TryGetArg("rawInput", out rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            CPH.LogDebug("[cannon-fire] rawInput argument missing or empty.");
            id736.Chat.SendMessage("Usage: !fire \u003cangle\u003e \u003cpower\u003e");
            return true;
        }

        CPH.LogDebug($"[cannon-fire] rawInput={rawInput}");

        var args = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Strip the command trigger if it is still present.
        int startIndex = 0;
        if (args.Length > 0 && args[0].Equals("!fire", StringComparison.OrdinalIgnoreCase))
            startIndex = 1;

        if (args.Length - startIndex < 2)
        {
            CPH.LogDebug("[cannon-fire] Not enough arguments after trigger.");
            id736.Chat.SendMessage("Usage: !fire \u003cangle\u003e \u003cpower\u003e");
            return true;
        }

        if (!int.TryParse(args[startIndex], out int angle) || !int.TryParse(args[startIndex + 1], out int power))
        {
            CPH.LogDebug("[cannon-fire] Failed to parse angle or power as integers.");
            id736.Chat.SendMessage("Angle and power must be numbers. Example: !fire 45 75");
            return true;
        }

        angle = Clamp(angle, 0, 90);
        power = Clamp(power, 0, 100);
        CPH.LogDebug($"[cannon-fire] Parsed angle={angle}, power={power}");

        if (!CPH.TryGetArg("userName", out string userName) || string.IsNullOrWhiteSpace(userName))
            userName = "Player";

        if (!CPH.TryGetArg("userType", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        if (!CPH.TryGetArg("profileImageUrl", out string profileImageUrl))
            profileImageUrl = "";

        CPH.LogDebug($"[cannon-fire] userName={userName}, platform={platform}");

        var entry = new Dictionary<string, object>
        {
            { "name", userName },
            { "platform", platform },
            { "angle", angle },
            { "power", power },
            { "profileImageUrl", profileImageUrl }
        };

        string queueJson = CPH.GetGlobalVar<string>("cannon_queue", false) ?? "[]";
        CPH.LogDebug($"[cannon-fire] Existing queue: {queueJson}");
        var queue = id736.Data.JsonToNestedList(queueJson) ?? new List<object>();
        queue.Add(entry);
        CPH.SetGlobalVar("cannon_queue", id736.Data.ToJson(queue), false);
        CPH.LogDebug("[cannon-fire] Added player to queue.");

        // Defensive reset: if a previous shot's firing flag got stuck (e.g. the
        // browser never reported the landing), clearing it here lets the next
        // queued player fire after the current animation. The timer will see the
        // flag is false and start the new shot on its next tick.
        bool firing = CPH.GetGlobalVar<bool>("cannon_firing", false);
        if (firing)
        {
            CPH.LogDebug("[cannon-fire] cannon_firing was true on queue add; resetting as a safety net.");
            CPH.SetGlobalVar("cannon_firing", false, false);
            CPH.SetGlobalVar("cannon_firing_started", 0L, false);
        }

        // Show the game in OBS and wake up the game timer.
        ShowGameSource();
        EnableGameTimer(intervalSeconds: 2);

        id736.Chat.SendMessage($"{userName} is locked and loaded! (angle {angle}, power {power})");
        CPH.LogDebug("[cannon-fire] Finished.");
        return true;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private void ShowGameSource()
    {
        string scene = CPH.GetGlobalVar<string>("cannon_obs_scene", false) ?? "";
        string source = CPH.GetGlobalVar<string>("cannon_obs_source", false) ?? "";

        CPH.LogDebug($"[cannon-fire] ShowGameSource scene={scene}, source={source}");

        if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(source))
        {
            CPH.LogDebug("[cannon-fire] Cannot show source: missing scene or source config.");
            return;
        }

        CPH.LogDebug($"[cannon-fire] Calling ObsShowSource({scene}, {source}).");
        CPH.ObsShowSource(scene, source);
    }

    private void EnableGameTimer(int intervalSeconds)
    {
        string timerName = CPH.GetGlobalVar<string>("cannon_timer_name", false) ?? "cannon-game";
        string timerGuid = CPH.GetGlobalVar<string>("cannon_timer_guid", false) ?? "";
        CPH.LogDebug($"[cannon-fire] EnableGameTimer name={timerName}, guid={timerGuid}");

        // The id736.Timers helpers call CPH's *ById methods, which require a GUID.
        // If setup already looked up the GUID, use the DLL helpers directly.
        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
            CPH.LogDebug($"[cannon-fire] Enabling/resetting timer '{timerGuid}' to {intervalSeconds}s via DLL.");
            id736.Timers.Enable(timerGuid);
            id736.Timers.ResetTimerById(timerGuid, intervalSeconds, keepEnabled: true);
            // Interval is already applied; tell tick not to reset again.
            CPH.SetGlobalVar("cannon_timer_interval", 0, false);
        }
        else if (!string.IsNullOrWhiteSpace(timerName))
        {
            // No GUID captured yet. Use CPH's name-based EnableTimer so the timer fires
            // once; cannon-game-tick will then capture the GUID from the timerId argument
            // and apply the requested interval itself.
            CPH.LogDebug($"[cannon-fire] No GUID yet; enabling timer by name '{timerName}' via CPH.EnableTimer.");
            CPH.EnableTimer(timerName);
            CPH.SetGlobalVar("cannon_timer_interval", intervalSeconds, false);
        }
        else
        {
            CPH.LogDebug("[cannon-fire] No timer name or GUID configured; cannot enable timer.");
        }

        CPH.SetGlobalVar("cannon_last_active", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        CPH.SetGlobalVar("cannon_timer_enabled", true, false);
        CPH.LogDebug("[cannon-fire] Timer enabled flag set and activity timestamp updated.");
    }
}
