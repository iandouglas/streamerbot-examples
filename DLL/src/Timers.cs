using System;
using System.Threading.Tasks;
using Streamer.bot.Plugin.Interface;

namespace iandouglas736
{
    /// <summary>
    /// Helpers for managing Streamer.bot timers from C# code.
    /// Set the context once per action with SetContext(CPH), then use the static methods.
    /// </summary>
    public static class Timers
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
                    Log.Message("iandouglas736.Timers.SetContext(CPH) must be called before using timer helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                    throw new InvalidOperationException("iandouglas736.Timers.SetContext(CPH) must be called before using timer helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Resets a Streamer.bot timer by GUID to run at a new interval.
        /// Optionally disables and re-enables it so the new interval takes effect immediately.
        /// </summary>
        /// <param name="timerGuid">The timer GUID as a string.</param>
        /// <param name="durationInSeconds">New interval in seconds.</param>
        /// <param name="keepEnabled">If true, disables then re-enables the timer. Default is true.</param>
        public static void ResetTimerById(string timerGuid, int durationInSeconds, bool keepEnabled = true)
        {
            if (string.IsNullOrWhiteSpace(timerGuid))
                return;

            Task.Run(async () =>
            {
                await Task.Delay(200);
                CPH.SetTimerInterval(timerGuid, durationInSeconds);
                CPH.DisableTimerById(timerGuid);
                if (keepEnabled)
                    CPH.EnableTimerById(timerGuid);
            });
        }

        /// <summary>
        /// Resets a Streamer.bot timer using the timerId argument that Streamer.bot provides
        /// when a timer event triggers an action.
        /// </summary>
        public static void ResetTimer(int durationInSeconds, bool keepEnabled = true)
        {
            if (!CPH.TryGetArg("timerId", out Guid timerId))
                return;

            ResetTimerById(timerId.ToString(), durationInSeconds, keepEnabled);
        }

        /// <summary>
        /// Disables a timer by GUID.
        /// </summary>
        public static void Disable(string timerGuid)
        {
            if (!string.IsNullOrWhiteSpace(timerGuid))
                CPH.DisableTimerById(timerGuid);
        }

        /// <summary>
        /// Enables a timer by GUID.
        /// </summary>
        public static void Enable(string timerGuid)
        {
            if (!string.IsNullOrWhiteSpace(timerGuid))
                CPH.EnableTimerById(timerGuid);
        }

        /// <summary>
        /// Sets a timer's interval in seconds without disabling/enabling it.
        /// </summary>
        public static void SetInterval(string timerGuid, int durationInSeconds)
        {
            if (!string.IsNullOrWhiteSpace(timerGuid))
                CPH.SetTimerInterval(timerGuid, durationInSeconds);
        }

    }
}
