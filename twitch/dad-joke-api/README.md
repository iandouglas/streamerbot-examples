# Get a funny joke from Ian's dad-a-base of Dad Jokes

This setup will retrieve a random joke from an API that Ian wrote and constantly adds to.

## Setup

Import the code from `import_code.txt` into Streamer.bot using the Import feature. This will create a new command called `!dadjoke` and associated action for that command to retrieve a joke and write it to Twitch chat. No authentication needed. You can alter the command to rate-limit how frequently people can grab a joke.

## What the imported action does

The action has two main sub-actions:

1. **Fetch URL** — calls `https://dadjokes736.com/api/random` and stores the response in a variable. No JSON parsing is needed because the API returns plain text.
2. **Send Message to Channel** — sends the fetched joke to **Twitch chat** using the Twitch `Send Message to Channel` sub-action.

> If you want to send to **Kick** or **YouTube** as well, add the corresponding platform sub-action (`Kick > Send Message` or `YouTube > Send Message to Channel`) and use the same variable as the message body.

## No compiling

The new version doesn't use any C# code, and uses built-in Streamer.bot controls to fetch a dad joke and to write it to Twitch Chat. You can expand this to send to other platforms by adding other sub-actions.

## Twitch-specific notes

- Make sure you have connected your **Twitch broadcaster account** in `Settings > Platforms > Twitch`.
- The Twitch `Send Message to Channel` sub-action lets you choose whether to send as the **Twitch Bot** account or the **Twitch Broadcaster** account. Pick whichever account you want the jokes to come from.

## Pledge of Clean Humor

The only jokes in this API are jokes that I have been telling my own kids for many years. While there may be some mild crude humor about farts or poking fun at the idea of death, I have completely avoided any known-to-me racial humor or derogatory terms. There are a few 'dark' humor jokes in there but they are still teenager-appropriate.

If you find a joke that is inappropriate, please contact me and let me know why, and I will consider removing it. I won't promise removal.

## Using it

The imported code will look for a single triggers: `!dadjoke` as a chat command. You can alter the Streamer.bot command to add other permissions or restrictions, or add/replace the trigger on the action to use a channel point redeem or other means for your users to access the jokes.

## Copyright on use

These jokes are found all over the internet, some were written or modified by me, and attribution has never been tracked. If you are the verifiable author of one of the jokes in the API, contact me and I will alter the joke to include attribution to you which will be part of the joke content itself so it will always be displayed.

## Rate Limiting

I reserve the right to rate limit the API or web site of dadjokes736.com at any time without notice.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://736.fyi/discord) for help.
