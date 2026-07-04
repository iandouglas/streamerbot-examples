# Get a funny Dad Joke in YouTube chat

This setup will retrieve a random joke from an API that Ian wrote and constantly adds to, then send it to YouTube live chat.

## Setup

1. Import the code from `import_code.txt` into Streamer.bot using the Import feature. This will create a new `!dadjoke` chat command and an associated action.
2. No authentication is needed for the joke API.
3. You can alter the command to rate-limit how frequently people can grab a joke.

## What the imported action does

The action has two main sub-actions:

1. **Fetch URL** — calls `https://dadjokes736.com/api/random` and stores the response in a variable. No JSON parsing is needed because the API returns plain text.
2. **Send Message to Channel** — sends the fetched joke to **YouTube live chat** using the YouTube `Send Message to Channel` sub-action.

> If you want to send to **Twitch** or **Kick** as well, add the corresponding platform sub-action (`Twitch > Send Message to Channel` or `Kick > Send Message`) and use the same variable as the message body.

## No compiling

This version uses only built-in Streamer.bot sub-actions — no C# code required.

## YouTube-specific notes

- Make sure you have connected your **YouTube broadcaster account** in `Settings > Platforms > YouTube`. You may also connect a **Bot Account** there if you want the jokes to come from a dedicated bot identity.
- The YouTube `Send Message to Channel` sub-action currently only supports **plain text messages** — no variable substitution in the message field. Because of this limitation, the imported action may need to use a **C# sub-action** to read the fetched joke variable and send it via `CPH.SendYouTubeMessage(message, sendFromBot)`.
  - If your import uses the built-in sub-action and the joke does not appear, switch the second sub-action to an **Execute C# Code** sub-action containing the line above.

## Pledge of Clean Humor

The only jokes in this API are jokes that I have been telling my own kids for many years. While there may be some mild crude humor about farts or poking fun at the idea of death, I have completely avoided any known-to-me racial humor or derogatory terms. There are a few 'dark' humor jokes in there but they are still teenager-appropriate.

If you find a joke that is inappropriate, please contact me and let me know why, and I will consider removing it. I won't promise removal.

## Using it

The imported code will look for a single chat trigger: `!dadjoke`. You can change the command name, add aliases, restrict permissions, or add a channel-point redeem / Super Chat trigger if you prefer.

## Copyright on use

These jokes are found all over the internet, some were written or modified by me, and attribution has never been tracked. If you are the verifiable author of one of the jokes in the API, contact me and I will alter the joke to include attribution to you which will be part of the joke content itself so it will always be displayed.

## Rate Limiting

I reserve the right to rate limit the API or web site of dadjokes736.com at any time without notice.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://736.fyi/discord) for help.
