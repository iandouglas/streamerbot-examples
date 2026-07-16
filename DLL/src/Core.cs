using Streamer.bot.Plugin.Interface;

namespace iandouglas736
{
    /// <summary>
    /// Entry point for configuring the iandouglas736 DLL with Streamer.bot.
    /// Call Core.LinkStreamerbot(CPH) at the top of every Execute() method to
    /// set context on all helper classes at once.
    /// 
    /// Also verifies that logging global variables (id736LogPath, id736DefaultFilenamePrefix)
    /// are configured — logs an error via CPH.LogError if they are missing.
    /// </summary>
    public static class Core
    {
        /// <summary>
        /// Sets CPH context on all iandouglas736 helper classes and verifies
        /// logging configuration. Call this once at the top of each Execute() method.
        /// </summary>
        public static void LinkStreamerbot(IInlineInvokeProxy cph)
        {
            if (cph == null)
                throw new System.ArgumentNullException(nameof(cph));

            Chat.SetContext(cph);
            Groups.SetContext(cph);
            Points.SetContext(cph);
            PlatformConfig.SetContext(cph);
            Timers.SetContext(cph);
            Log.SetContext(cph);
            Media.SetContext(cph);

            VerifyLogConfig(cph);
        }

        private static void VerifyLogConfig(IInlineInvokeProxy cph)
        {
            string logPath = cph.GetGlobalVar<string>("id736LogPath", true);
            if (string.IsNullOrWhiteSpace(logPath))
            {
                cph.LogError("[iandouglas736] id736LogPath global variable is not set. Logging will not work. Add a startup action with: CPH.SetGlobalVar(\"id736LogPath\", \"S:/logs\", true)");
            }

            string defaultPrefix = cph.GetGlobalVar<string>("id736DefaultFilenamePrefix", true);
            if (string.IsNullOrWhiteSpace(defaultPrefix))
            {
                cph.LogError("[iandouglas736] id736DefaultFilenamePrefix global variable is not set. Logging will not work. Add a startup action with: CPH.SetGlobalVar(\"id736DefaultFilenamePrefix\", \"iandouglas736\", true)");
            }
        }
    }
}