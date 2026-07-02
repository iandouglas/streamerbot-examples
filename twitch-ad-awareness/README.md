# Twitch Ad Awareness

Streamer.bot has a way to notify you if ads are going to start, in one-minute intervals, from 1 minute ahead to 5 minutes ahead. This mechanism is also informed, by Twitch, how long the ads will last.

This set of actions and C# code will write a chat message to Twitch chat that ads are going to start, and how long they will last, as well as send a Twitch Chat when ads are over.

## Setup

Import the code from `import_code.txt` into Streamer.bot using the Import feature.

### "ad awareness" action

This action will be triggered by the "Twitch > Ads" source and is set to notifying chat one minute ahead of time, but you can enable one or more of these by double-clicking on the trigger and picking however many alerts you want to show up in chat.

#### Check and compile the C# code

Double-click on the "Execute Code" sub-action.

The code gets the alert data from Twitch about the ads: when they're going to start and how long they'll last. The C# code stores a few temporary values in memory in Streamer.bot and then activates a necessary timer and secondary action called "ad watch logic" that does all the work from there.

Click the "Compile" button at the bottom of the code window, and then the "Save and Compile" button to close the window. If the "Compile" button gives you an errors, try clicking the "Find Refs" button, then the "Compile" button again. If you still have trouble, come ask in my [Discord community](https://736.fyi/discord) for free help.

You can right-click on the "Twitch > Ads" trigger and pick the "Test Trigger" at the top to watch your Twitch chat to see what will happen.

### "ad water logic" action

This is the heart of the ad awareness.

This action uses and resets some timers. Line 44 is the message that prints to chat as ads are starting. Line 48 is the message that prints when the ads are over. You can customize these and write in the names of your own emotes if you want.

#### Run some C# code

Double-click on the "Execute Code" sub-action.

The message sent to Twitch Chat is on line 13 and uses the "HeyGuys" global Twitch icon as a "wave" greeting. You can customize this string however you like.

Click the "Compile" button at the bottom of the code window, and then the "Save and Compile" button to close the window. If the "Compile" button gives you an errors, try clicking the "Find Refs" button, then the "Compile" button again. If you still have trouble, come ask in my [Discord community](https://736.fyi/discord) for free help.

You can right-click on the "Twitch > Ads" trigger and pick the "Test Trigger" at the top to watch your Twitch chat to see what will happen.
