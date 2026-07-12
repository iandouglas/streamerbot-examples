using System;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace iandouglas736
{
    /// <summary>
    /// Cross-platform chat helpers for Streamer.bot.
    /// Set the context once per action with SetContext(CPH), then call the static methods.
    /// </summary>
    public static class Chat
    {
        private static IInlineInvokeProxy _cph;

        /// <summary>
        /// Sets the Streamer.bot CPH proxy for this action. Call once at the top of Execute().
        /// </summary>
        public static void SetContext(IInlineInvokeProxy cph)
        {
            _cph = cph ?? throw new ArgumentNullException(nameof(cph));
        }

        private static IInlineInvokeProxy CPH
        {
            get
            {
                if (_cph == null)
                    throw new InvalidOperationException("iandouglas736.Chat.SetContext(CPH) must be called before using chat helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Sends a chat message to the platform that triggered the current action.
        /// Falls back to Twitch if the platform cannot be determined.
        /// </summary>
        public static void SendMessage(string message, bool fromBot = true)
        {
            string platform = GetCurrentPlatform();
            SendMessageTo(platform, message, fromBot);
        }

        /// <summary>
        /// Sends a chat message to a specific platform.
        /// </summary>
        public static void SendMessageTo(string platform, string message, bool fromBot = true)
        {
            if (string.IsNullOrEmpty(message))
                return;

            switch (platform?.ToLowerInvariant())
            {
                case "youtube":
                    try
                    {
                        CPH.LogDebug($"[static] sending '{message}' to youtube");
                        CPH.SendYouTubeMessage(message, fromBot);
                        CPH.LogDebug($"[static] sent '{message}' to youtube");
                    }
                    catch (Exception e)
                    {
                        CPH.LogDebug($"[static] exception: {e.ToString()}");
                        CPH.LogError(e.ToString());
                    }

                    break;
                case "kick":
                    CPH.SendKickMessage(message, fromBot);
                    break;
                default:
                    CPH.SendMessage(message, fromBot);
                    break;
            }
        }

        /// <summary>
        /// Sends a chat message to a specific platform.
        /// </summary>
        public static void SendMessageTo(Platform platform, string message, bool fromBot = true)
        {
            SendMessageTo(platform.ToString().ToLowerInvariant(), message, fromBot);
        }

        /// <summary>
        /// Sends a Twitch reply if a msgId is available, otherwise sends a normal message.
        /// On YouTube/Kick, a normal message is sent because those platforms do not support replies in the same way.
        /// </summary>
        public static void SendReplyOrMessage(string message, string msgId = null, bool fromBot = true)
        {
            string platform = GetCurrentPlatform();

            if (platform == "twitch" && !string.IsNullOrEmpty(msgId))
            {
                CPH.TwitchReplyToMessage(message, msgId, fromBot);
            }
            else
            {
                SendMessage(message, fromBot);
            }
        }

        /// <summary>
        /// Reads the current action's platform from the userType argument.
        /// </summary>
        public static string GetCurrentPlatform()
        {
            if (CPH.TryGetArg("userType", out string platform))
                return platform?.ToLowerInvariant() ?? "twitch";
            return "twitch";
        }

        /// <summary>
        /// Reads the current action's platform as a Streamer.bot Platform enum.
        /// </summary>
        public static Platform GetCurrentPlatformEnum()
        {
            string platform = GetCurrentPlatform();
            if (Enum.TryParse(platform, true, out Platform parsed))
                return parsed;
            return Platform.Twitch;
        }

        /// <summary>
        /// Reads the current action's username (login) from the userName argument.
        /// Falls back to the user argument if userName is not present.
        /// </summary>
        public static string GetCurrentUserName()
        {
            if (CPH.TryGetArg("userName", out string userName) && !string.IsNullOrEmpty(userName))
                return userName;
            if (CPH.TryGetArg("user", out string user) && !string.IsNullOrEmpty(user))
                return user;
            return null;
        }
    }
}
