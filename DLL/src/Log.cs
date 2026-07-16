using System;
using System.IO;
using Streamer.bot.Plugin.Interface;

namespace iandouglas736
{
    /// <summary>
    /// Writes per-prefix daily log files to a disk directory.
    /// 
    /// The path and filename prefix can be configured once via Streamer.bot global
    /// variables (id736LogPath, id736DefaultFilenamePrefix) and read automatically
    /// by every Log.Message() call, or passed explicitly per call.
    /// 
    /// Requires SetContext(CPH) — call Core.LinkStreamerbot(CPH) at the top of each action.
    /// </summary>
    public static class Log
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
                    throw new InvalidOperationException("iandouglas736.Log.SetContext(CPH) must be called before using Log.Message(). Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Writes a timestamped line to a daily log file.
        /// 
        /// The path and filename prefix are resolved in order:
        ///   1. Explicit parameters (if provided)
        ///   2. Streamer.bot global variables (id736LogPath, id736DefaultFilenamePrefix)
        /// 
        /// If path or prefix cannot be resolved, logs an error via CPH.LogError and returns.
        /// 
        /// File name format: {prefix}-{yyyyMMdd}.txt
        /// Line format: [{timestamp}] {message}
        /// </summary>
        /// <param name="message">The text to log (required).</param>
        /// <param name="filenamePrefix">Prefix for the log file name. If null, reads from CPH global var "id736DefaultFilenamePrefix".</param>
        /// <param name="path">Directory path for log files. If null, reads from CPH global var "id736LogPath".</param>
        public static void Message(string message, string filenamePrefix = null, string path = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            string resolvedPath = path;
            if (string.IsNullOrWhiteSpace(resolvedPath))
                resolvedPath = CPH.GetGlobalVar<string>("id736LogPath", true);

            string resolvedPrefix = filenamePrefix;
            if (string.IsNullOrWhiteSpace(resolvedPrefix))
                resolvedPrefix = CPH.GetGlobalVar<string>("id736DefaultFilenamePrefix", true);

            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                CPH.LogError("[iandouglas736.Log] Cannot write log: id736LogPath global variable is not set. Configure it in your Streamer.bot startup action: CPH.SetGlobalVar(\"id736LogPath\", \"S:/logs\", true)");
                return;
            }

            if (string.IsNullOrWhiteSpace(resolvedPrefix))
            {
                CPH.LogError("[iandouglas736.Log] Cannot write log: id736DefaultFilenamePrefix global variable is not set. Configure it in your Streamer.bot startup action: CPH.SetGlobalVar(\"id736DefaultFilenamePrefix\", \"iandouglas736\", true)");
                return;
            }

            try
            {
                var fileName = $"{resolvedPrefix}-{DateTime.Now:yyyyMMdd}.txt";
                var fullPath = Path.Combine(resolvedPath, fileName);

                Directory.CreateDirectory(resolvedPath);

                System.IO.File.AppendAllText(
                    fullPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH\\:mm\\:ss.ffff}] {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                CPH.LogError($"[iandouglas736.Log] Failed to write log file: {ex.Message}");
            }
        }
    }
}