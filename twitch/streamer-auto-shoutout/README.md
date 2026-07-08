# auto shoutout for other streamers on Twitch

This is used with my [shoutout-system](../shoutout-system) code.


## Set up a Streamer.bot group

Go into Streamer.bot, Settings -> Groups

Add a new group called "streamers"

Add any users to your group that you know are streamers on Twitch that have been in your chat.


## Build your action

Create a new Action.

The action Trigger will be "Twitch -> Chat -> First Words"

Add a subaction to "Execute C# Code" and paste in the "streamer-auto-shoutout.cs" code and make sure it compiles. It doesn't require any additional DLL libraries. Click "Save and Compile" or "Ok" to close that window.


## How it works

When you reset First Words (usually happens from Streamer.bot every 12 hours but you can also set this as a subaction when your stream goes live) any first-time chats that happen in your stream will be compared against the username in your "streamers" group. If the name matches, the code will issue a `!so username` automatically.

There is logic in the code that will check both their "user name" and their "display name" as they may appear differently in chat and what they actually registered as their user/display names on Twitch, so one way or another it should give them a proper shoutout.
