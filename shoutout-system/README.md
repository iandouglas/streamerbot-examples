# Shoutout System

This is a two-piece system to allow a chat command of "!so username" which will queue up as many users as you want to shout out and set timers to send shouwout commands to your Twitch chat every two minutes to give the shoutout to everyone in the order they were queued.

The first piece is triggered by a `!so` command, which runs the matching `!so` action. This code simply adds the username to the shoutout queue and activates the timer system and enables the `!so (timer)` action. This second piece does the work of checking timers and resetting an internal timer to handle all of the shoutouts.


## the intial !so action

This intecepts the `!so username` trigger and adds the username to a queue stored in persistent memory across streams. If the `!so (timer)` action is disabled (more on that later), this will enable the other action to run.

### Run some C# code

Double-click on the "Execute Code" sub-action.

Click the "Compile" button at the bottom of the code window, and then the "Save and Compile" button to close the window. If the "Compile" button gives you an errors, try clicking the "Find Refs" button, then the "Compile" button again. If you still have trouble, come ask in my [Discord community](https://736.fyi/discord) for free help.

You can right-click on the "Twitch > Ads" trigger and pick the "Test Trigger" at the top to watch your Twitch chat to see what will happen.

## the !so timer action

This action is disabled automatically when there are no shoutouts to run.

When a `!so username` command is seen in chat, this action gets enabled. It has awareness of whether it's been 120 seconds since the last shoutout so everything will be queued up properly. If there are no more usernames are queued, the timer and this action are disabled.

The other sub-actions include the announcement message that you want to send as the announcement with the /shoutout command that gets sent by Streamer.bot.

### Run some C# code

Double-click on the "Execute Code" sub-action.

Click the "Compile" button at the bottom of the code window, and then the "Save and Compile" button to close the window. If the "Compile" button gives you an errors, try clicking the "Find Refs" button, then the "Compile" button again. If you still have trouble, come ask in my [Discord community](https://736.fyi/discord) for free help.

You can right-click on the "Twitch > Ads" trigger and pick the "Test Trigger" at the top to watch your Twitch chat to see what will happen.

## Customization

If you want to do extra things like activate OBS scenes or sources, feel free to contact me for customization help.