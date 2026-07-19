using System;
using Streamer.bot.Plugin.Interface;
using Streamer.bot.Plugin.Interface.Enums;

namespace iandouglas736
{
    /// <summary>
    /// Cross-platform user group helpers for Streamer.bot.
    /// Groups in Streamer.bot are global containers, but membership is stored per platform.
    /// These helpers let you treat a user as a single participant across Twitch, YouTube, and Kick.
    /// </summary>
    public static class Groups
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
                    Log.Message("iandouglas736.Groups.SetContext(CPH) must be called before using group helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                    throw new InvalidOperationException("iandouglas736.Groups.SetContext(CPH) must be called before using group helpers. Use Core.LinkStreamerbot(CPH) to set context on all helpers.");
                return _cph;
            }
        }

        /// <summary>
        /// Ensures the named group exists. Safe to call repeatedly.
        /// </summary>
        public static void EnsureGroup(string groupName)
        {
            if (!CPH.GroupExists(groupName))
                CPH.AddGroup(groupName);
        }

        /// <summary>
        /// Checks whether a user is in a group on any connected platform.
        /// </summary>
        public static bool IsInGroup(string userName, string groupName)
        {
            return CPH.UserInGroup(userName, Platform.Twitch, groupName)
                || CPH.UserInGroup(userName, Platform.YouTube, groupName)
                || CPH.UserInGroup(userName, Platform.Kick, groupName);
        }

        /// <summary>
        /// Checks whether a user is in a group on a specific platform.
        /// </summary>
        public static bool IsInGroup(string userName, Platform platform, string groupName)
        {
            return CPH.UserInGroup(userName, platform, groupName);
        }

        /// <summary>
        /// Adds a user to a group on a specific platform and ensures the group exists.
        /// </summary>
        public static void AddUser(string userName, Platform platform, string groupName)
        {
            EnsureGroup(groupName);
            CPH.AddUserToGroup(userName, platform, groupName);
        }

        /// <summary>
        /// Adds a user to a group on a specific platform and ensures the group exists.
        /// </summary>
        public static void AddUser(string userName, string platform, string groupName)
        {
            if (!Enum.TryParse(platform, true, out Platform parsed))
                parsed = Platform.Twitch;
            AddUser(userName, parsed, groupName);
        }

        /// <summary>
        /// Removes a user from a group on all platforms.
        /// </summary>
        public static void RemoveUser(string userName, string groupName)
        {
            CPH.RemoveUserFromGroup(userName, Platform.Twitch, groupName);
            CPH.RemoveUserFromGroup(userName, Platform.YouTube, groupName);
            CPH.RemoveUserFromGroup(userName, Platform.Kick, groupName);
        }

        /// <summary>
        /// Removes all users from a group.
        /// </summary>
        public static void Clear(string groupName)
        {
            CPH.ClearUsersFromGroup(groupName);
        }

        /// <summary>
        /// Counts users in a group. Because Streamer.bot stores membership per platform,
        /// the same username on multiple platforms counts as multiple entries.
        /// </summary>
        public static int Count(string groupName)
        {
            var users = CPH.UsersInGroup(groupName);
            if (users == null)
                return 0;

            int count = 0;
            foreach (var _ in users)
                count++;
            return count;
        }
    }
}
