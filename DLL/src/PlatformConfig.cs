using System;
using System.Collections.Generic;
using Streamer.bot.Plugin.Interface;

namespace iandouglas736
{
    /// <summary>
    /// Helpers for reading streamer-configured platform settings from global variables.
    ///
    /// Global variables let a streamer decide:
    /// - Which platforms they stream to (default: Twitch, YouTube, Kick if connected)
    /// - Which platforms a specific command or action is allowed to use
    ///
    /// Suggested naming convention for per-action platform filters:
    ///   idLLC_enabled_platforms            = global default list (e.g. "twitch,youtube")
    ///   idLLC_<action>_enabled_platforms   = per-action override (e.g. "idLLC_higherlower_enabled_platforms")
    ///
    /// Values are comma-separated platform names: "twitch", "youtube", "kick".
    /// </summary>
    public static class PlatformConfig
    {
        private static IInlineInvokeProxy _cph;

        public static void SetContext(IInlineInvokeProxy cph)
        {
            _cph = cph ?? throw new ArgumentNullException(nameof(cph));
        }

        private static IInlineInvokeProxy CPH
        {
            get
            {
                if (_cph == null)
                    Log.Message("iandouglas736.PlatformConfig.SetContext(CPH) must be called before using platform config helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                    throw new InvalidOperationException("iandouglas736.PlatformConfig.SetContext(CPH) must be called before using platform config helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Default list of supported platforms in lower-case.
        /// </summary>
        public static readonly List<string> AllPlatforms = new List<string> { "twitch", "youtube", "kick" };

        /// <summary>
        /// Reads a comma-separated list of enabled platforms from a global variable.
        /// If the variable is missing or empty, returns all platforms.
        /// </summary>
        public static List<string> GetEnabledPlatforms(string varName = "idLLC_enabled_platforms")
        {
            string raw = CPH.GetGlobalVar<string>(varName, true);
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>(AllPlatforms);

            var result = new List<string>();
            foreach (string part in raw.Split(','))
            {
                string platform = part.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(platform) && AllPlatforms.Contains(platform))
                    result.Add(platform);
            }

            return result.Count > 0 ? result : new List<string>(AllPlatforms);
        }

        /// <summary>
        /// Reads a per-action override if it exists, otherwise falls back to the global default.
        /// Use actionName like "higherlower" to read "idLLC_higherlower_enabled_platforms".
        /// </summary>
        public static List<string> GetEnabledPlatformsForAction(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return GetEnabledPlatforms();

            string actionVar = $"idLLC_{actionName}_enabled_platforms";
            string raw = CPH.GetGlobalVar<string>(actionVar, true);
            if (string.IsNullOrWhiteSpace(raw))
                return GetEnabledPlatforms();

            return GetEnabledPlatforms(actionVar);
        }

        /// <summary>
        /// Returns true if the current action's platform is enabled for the given action.
        /// Reads platform from the userType argument.
        /// </summary>
        public static bool IsCurrentPlatformEnabled(string actionName = null)
        {
            string platform = Chat.GetCurrentPlatform();
            return IsPlatformEnabled(platform, actionName);
        }

        /// <summary>
        /// Returns true if the specified platform is enabled for the given action.
        /// </summary>
        public static bool IsPlatformEnabled(string platform, string actionName = null)
        {
            List<string> enabled = string.IsNullOrEmpty(actionName)
                ? GetEnabledPlatforms()
                : GetEnabledPlatformsForAction(actionName);

            return enabled.Contains(platform?.ToLowerInvariant() ?? "twitch");
        }

        /// <summary>
        /// Convenience: sets the global default list of enabled platforms.
        /// </summary>
        public static void SetEnabledPlatforms(string platforms, string varName = "idLLC_enabled_platforms")
        {
            CPH.SetGlobalVar(varName, platforms, true);
        }

        /// <summary>
        /// Convenience: sets the per-action list of enabled platforms.
        /// </summary>
        public static void SetEnabledPlatformsForAction(string actionName, string platforms)
        {
            if (string.IsNullOrEmpty(actionName))
                return;
            CPH.SetGlobalVar($"idLLC_{actionName}_enabled_platforms", platforms, true);
        }
    }
}
