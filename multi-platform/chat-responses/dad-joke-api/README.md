# Dad Joke API

A cross-platform chat command that fetches a random dad joke from `https://dadjokes736.com/api/random` and relays it to all connected platforms.

## Requirements

This action uses the shared helper DLL `iandouglas736.dll`. Follow the install steps in the main [`README.md`](../../../README.md) or [`dlls-needed/README.md`](../../../dlls-needed/README.md).

The C# code in `dad-joke.cs` uses a namespace alias so the helper calls are short:

```csharp
using id736 = iandouglas736;

id736.Chat.SendMessage("Hello, chat!");
id736.Chat.SendMessageTo("youtube", "Hello, YouTube!");
```

## Files

| File | Purpose |
|---|---|
| `dad-joke.cs` | The C# action code. |

## How it works

When someone runs `!dadjoke` on any platform:

1. The action fetches a random joke from the API.
2. It sends a relay message to the **other** platforms saying who asked for the joke:
   - Example: `iandouglas from youtube wants a dad joke!`
3. It sends the actual joke to **all** platforms.

On the source platform, the relay step is skipped, so YouTube chat only sees the dad joke itself.

## Setup

1. Create a new Action in Streamer.bot.
2. Add an **Execute C# Code** sub-action.
3. Paste the contents of `dad-joke.cs`.
4. Set a trigger:
   - Twitch/YouTube/Kick chat command `!dadjoke`
   - Channel point reward
   - Bit redeem
   - Timed action
   - Anything else you like

## Pledge of Clean Humor

The only jokes in this API are jokes that I have been telling my own kids for many years. While there may be some mild crude humor about farts or poking fun at the idea of death, I have completely avoided any known-to-me racial humor or derogatory terms. There are a few 'dark' humor jokes in there but they are still teenager-appropriate.

If you find a joke that is inappropriate, please contact me and let me know why, and I will consider removing it. I won't promise removal.

## Copyright on use

These jokes are found all over the internet, some were written or modified by me, and attribution has never been tracked. If you are the verifiable author of one of the jokes in the API, contact me and I will alter the joke to include attribution to you which will be part of the joke content itself so it will always be displayed.

## Rate Limiting

I reserve the right to rate limit the API or web site of dadjokes736.com at any time without notice.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://736.fyi/discord) for help.
