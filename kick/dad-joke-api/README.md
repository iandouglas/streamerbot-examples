# Get a funny Dad Joke in Kick chat

This setup will retrieve a random joke from an API that Ian wrote and constantly adds to, then send it to Kick chat.

## Setup

1. Import the code from `import_code.txt` into Streamer.bot using the Import feature. This will create a new `!dadjoke` chat command and an associated action.
2. No authentication is needed for the joke API.
3. You can alter the command to rate-limit how frequently people can grab a joke.

## What the imported action does

The action has two main sub-actions:

1. **Fetch URL** — calls `https://dadjokes736.com/api/random` and stores the response in a variable. No JSON parsing is needed because the API returns plain text.
2. **Send Message** — sends the fetched joke to **Kick chat** using the Kick `Send Message` sub-action.

> If you want to send to **Twitch** or **YouTube** as well, add the corresponding platform sub-action (`Twitch > Send Message to Channel` or `YouTube > Send Message to Channel`) and use the same variable as the message body.

## No compiling

This version uses only built-in Streamer.bot sub-actions — no C# code required.

## Kick-specific notes

- Make sure you have connected your **Kick broadcaster account** in `Settings > Platforms > Kick`.
- The Kick `Send Message` sub-action lets you choose whether to send as the **Kick Bot** account or the **Kick Broadcaster** account. Pick whichever account you want the jokes to come from.
- If you select **Send using bot account**, enable **Fallback to Broadcaster** if you want messages to still go out when the bot account is not logged in.

## Pledge of Clean Humor

The only jokes in this API are jokes that I have been telling my own kids for many years. While there may be some mild crude humor about farts or poking fun at the idea of death, I have completely avoided any known-to-me racial humor or derogatory terms. There are a few 'dark' humor jokes in there but they are still teenager-appropriate.

If you find a joke that is inappropriate, please contact me and let me know why, and I will consider removing it. I won't promise removal.

## Using it

The imported code will look for a single chat trigger: `!dadjoke`. You can change the command name, add aliases, restrict permissions, or add a channel-point redeem trigger if you prefer.

## Copyright on use

These jokes are found all over the internet, some were written or modified by me, and attribution has never been tracked. If you are the verifiable author of one of the jokes in the API, contact me and I will alter the joke to include attribution to you which will be part of the joke content itself so it will always be displayed.

## Rate Limiting

I reserve the right to rate limit the API or web site of dadjokes736.com at any time without notice.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://736.fyi/discord) for help.
