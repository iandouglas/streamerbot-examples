using System;
using System.Collections.Generic;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        id736.Chat.SetContext(CPH);
        id736.Timers.SetContext(CPH);

        // Try the rawInput argument.
        string rawInput = "";
        if (!CPH.TryGetArg("rawInput", out rawInput) || string.IsNullOrWhiteSpace(rawInput))
        {
            id736.Chat.SendMessage("Usage: !fire <angle> <power>");
            return true;
        }

        var args = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Strip the command trigger if it is still present.
        int startIndex = 0;
        if (args.Length > 0 && args[0].Equals("!fire", StringComparison.OrdinalIgnoreCase))
            startIndex = 1;

        if (args.Length - startIndex < 2)
        {
            id736.Chat.SendMessage("Usage: !fire <angle> <power>");
            return true;
        }

        if (!int.TryParse(args[startIndex], out int angle) || !int.TryParse(args[startIndex + 1], out int power))
        {
            id736.Chat.SendMessage("Angle and power must be numbers. Example: !fire 45 75");
            return true;
        }

        angle = Clamp(angle, 0, 90);
        power = Clamp(power, 0, 100);

        if (!CPH.TryGetArg("userName", out string userName) || string.IsNullOrWhiteSpace(userName))
            userName = "Player";

        if (!CPH.TryGetArg("userType", out string platform) || string.IsNullOrWhiteSpace(platform))
            platform = "twitch";
        platform = platform.ToLowerInvariant();

        if (!CPH.TryGetArg("profileImageUrl", out string profileImageUrl))
            profileImageUrl = "";

        var entry = new Dictionary<string, object>
        {
            { "name", userName },
            { "platform", platform },
            { "angle", angle },
            { "power", power },
            { "profileImageUrl", profileImageUrl }
        };

        string queueJson = CPH.GetGlobalVar<string>("cannon_queue", false) ?? "[]";
        var queue = id736.Data.JsonToNestedList(queueJson) ?? new List<object>();
        queue.Add(entry);
        CPH.SetGlobalVar("cannon_queue", id736.Data.ToJson(queue), false);

        // Show the game in OBS and wake up the game timer.
        ShowGameSource();
        EnableGameTimer(intervalSeconds: 1);

        id736.Chat.SendMessage($"{userName} is waiting for their turn.");
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

        if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(source))
            return;

        CPH.ObsShowSource(scene, source);
        CPH.SetGlobalVar("cannon_source_shown_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
    }

    private void EnableGameTimer(int intervalSeconds)
    {
        string timerName = CPH.GetGlobalVar<string>("cannon_timer_name", false) ?? "cannon-game";
        string timerGuid = CPH.GetGlobalVar<string>("cannon_timer_guid", false) ?? "";

        // The id736.Timers helpers call CPH's *ById methods, which require a GUID.
        // If setup already looked up the GUID, use the DLL helpers directly.
        if (!string.IsNullOrWhiteSpace(timerGuid))
        {
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
            CPH.EnableTimer(timerName);
            CPH.SetGlobalVar("cannon_timer_interval", intervalSeconds, false);
        }

        CPH.SetGlobalVar("cannon_last_active", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);
        CPH.SetGlobalVar("cannon_timer_enabled", true, false);
    }
}
