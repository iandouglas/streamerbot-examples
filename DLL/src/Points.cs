using System;
using System.Collections.Generic;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace iandouglas736
{
    /// <summary>
    /// Cross-platform currency/points helpers for Streamer.bot.
    /// Points are stored in platform-specific user variables because Streamer.bot
    /// does not provide a single global user variable across Twitch/YouTube/Kick.
    /// </summary>
    public static class Points
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
                    Log.Message("iandouglas736.Points.SetContext(CPH) must be called before using points helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                    throw new InvalidOperationException("iandouglas736.Points.SetContext(CPH) must be called before using points helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Gets a user's current balance for the given currency on the specified platform.
        /// Returns 0 if the user has no balance yet.
        /// </summary>
        public static int Get(string userName, Platform platform, string currencyName)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(currencyName))
                return 0;

            switch (platform)
            {
                case Platform.Twitch:
                    return CPH.GetTwitchUserVar<int>(userName, currencyName, true);
                case Platform.YouTube:
                    return CPH.GetYouTubeUserVar<int>(userName, currencyName, true);
                case Platform.Kick:
                    return CPH.GetKickUserVar<int>(userName, currencyName, true);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets a user's current balance for the given currency on the specified platform.
        /// </summary>
        public static int Get(string userName, string platform, string currencyName)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            return Get(userName, parsed, currencyName);
        }

        /// <summary>
        /// Sets a user's balance for the given currency on the specified platform.
        /// </summary>
        public static void Set(string userName, Platform platform, string currencyName, int value)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(currencyName))
                return;

            switch (platform)
            {
                case Platform.Twitch:
                    CPH.SetTwitchUserVar(userName, currencyName, value, true);
                    break;
                case Platform.YouTube:
                    CPH.SetYouTubeUserVar(userName, currencyName, value, true);
                    break;
                case Platform.Kick:
                    CPH.SetKickUserVar(userName, currencyName, value, true);
                    break;
            }
        }

        /// <summary>
        /// Sets a user's balance for the given currency on the specified platform.
        /// </summary>
        public static void Set(string userName, string platform, string currencyName, int value)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            Set(userName, parsed, currencyName, value);
        }

        /// <summary>
        /// Adds points to a user's balance and returns the new total.
        /// </summary>
        public static int Add(string userName, Platform platform, string currencyName, int amount)
        {
            if (amount <= 0)
                return Get(userName, platform, currencyName);

            int current = Get(userName, platform, currencyName);
            int next = current + amount;
            Set(userName, platform, currencyName, next);
            return next;
        }

        /// <summary>
        /// Adds points to a user's balance and returns the new total.
        /// </summary>
        public static int Add(string userName, string platform, string currencyName, int amount)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            return Add(userName, parsed, currencyName, amount);
        }

        /// <summary>
        /// Subtracts points from a user's balance and returns the new total.
        /// Will not go below zero unless allowNegative is true.
        /// </summary>
        public static int Subtract(string userName, Platform platform, string currencyName, int amount, bool allowNegative = false)
        {
            if (amount <= 0)
                return Get(userName, platform, currencyName);

            int current = Get(userName, platform, currencyName);
            int next = current - amount;
            if (!allowNegative && next < 0)
                next = 0;

            Set(userName, platform, currencyName, next);
            return next;
        }

        /// <summary>
        /// Subtracts points from a user's balance and returns the new total.
        /// </summary>
        public static int Subtract(string userName, string platform, string currencyName, int amount, bool allowNegative = false)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            return Subtract(userName, parsed, currencyName, amount, allowNegative);
        }

        /// <summary>
        /// Gets a user's balance, initializing it to 0 if it has never been set.
        /// Useful for commands like !points where you want to guarantee a value exists.
        /// </summary>
        public static int GetOrInit(string userName, Platform platform, string currencyName)
        {
            int current = Get(userName, platform, currencyName);
            // Streamer.bot returns 0 for missing int vars; explicitly set 0 so it is persisted.
            Set(userName, platform, currencyName, current);
            return current;
        }

        /// <summary>
        /// Gets a user's balance, initializing it to 0 if it has never been set.
        /// </summary>
        public static int GetOrInit(string userName, string platform, string currencyName)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            return GetOrInit(userName, parsed, currencyName);
        }

        /// <summary>
        /// Adds points to every user on the specified platform. Returns the number of users updated.
        /// Streamer.bot provides platform-specific bulk increment methods.
        /// </summary>
        public static int AddToAll(Platform platform, string currencyName, int amount)
        {
            if (string.IsNullOrEmpty(currencyName) || amount == 0)
                return 0;

            switch (platform)
            {
                case Platform.Twitch:
                    CPH.IncrementAllTwitchUsersVar(currencyName, amount, true);
                    return -1; // count not available from Streamer.bot API
                case Platform.YouTube:
                    CPH.IncrementAllYouTubeUsersVar(currencyName, amount, true);
                    return -1;
                case Platform.Kick:
                    CPH.IncrementAllKickUsersVar(currencyName, amount, true);
                    return -1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Adds points to every user on the specified platform.
        /// </summary>
        public static int AddToAll(string platform, string currencyName, int amount)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            return AddToAll(parsed, currencyName, amount);
        }

        /// <summary>
        /// Gets a user's persisted dictionary variable (e.g., stock portfolio) on the given platform.
        /// Returns an empty dictionary if it has never been set.
        /// </summary>
        public static Dictionary<string, int> GetDictionary(string userName, Platform platform, string varName)
        {
            string json = GetRawString(userName, platform, varName);
            return Data.FromJson<Dictionary<string, int>>(json)
                ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a user's persisted dictionary variable on the given platform.
        /// </summary>
        public static Dictionary<string, int> GetDictionary(string userName, string platform, string varName)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            return GetDictionary(userName, parsed, varName);
        }

        /// <summary>
        /// Sets a user's persisted dictionary variable (e.g., stock portfolio) on the given platform.
        /// </summary>
        public static void SetDictionary(string userName, Platform platform, string varName, Dictionary<string, int> value)
        {
            string json = Data.ToJson(value) ?? "{}";
            SetRaw(userName, platform, varName, json);
        }

        /// <summary>
        /// Sets a user's persisted dictionary variable on the given platform.
        /// </summary>
        public static void SetDictionary(string userName, string platform, string varName, Dictionary<string, int> value)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            SetDictionary(userName, parsed, varName, value);
        }

        private static string GetRawString(string userName, Platform platform, string varName)
        {
            switch (platform)
            {
                case Platform.Twitch:
                    return CPH.GetTwitchUserVar<string>(userName, varName, true);
                case Platform.YouTube:
                    return CPH.GetYouTubeUserVar<string>(userName, varName, true);
                case Platform.Kick:
                    return CPH.GetKickUserVar<string>(userName, varName, true);
                default:
                    return null;
            }
        }

        private static void SetRaw(string userName, Platform platform, string varName, object value)
        {
            switch (platform)
            {
                case Platform.Twitch:
                    CPH.SetTwitchUserVar(userName, varName, value, true);
                    break;
                case Platform.YouTube:
                    CPH.SetYouTubeUserVar(userName, varName, value, true);
                    break;
                case Platform.Kick:
                    CPH.SetKickUserVar(userName, varName, value, true);
                    break;
            }
        }
    }
}
