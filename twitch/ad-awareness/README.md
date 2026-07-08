# Twitch Ad Awareness

Streamer.bot can notify you about upcoming Twitch ads at one-minute intervals before they start, and also tells you how long the ads will last.

This set of actions and C# code will write chat messages when ads are about to start, when they start, and when they finish. It uses the shared helper DLL `iandouglas736.dll` for time helpers and cross-platform chat sending.

## Requirements

Install the shared helper DLL following the main [`README.md`](../../README.md) or [`dlls-needed/README.md`](../../dlls-needed/README.md).

## Files

| File | Purpose |
|---|---|
| `ad-awareness.cs` | Triggered by the `Twitch → Ads` event. Stores ad timing and enables the watch timer. |
| `ad-watch-logic.cs` | Runs on the timer. Sends the "ads starting" and "ads over" messages and resets the timer. |

## Setup

1. Create the actions manually in Streamer.bot.
2. Open the `ad awareness` action and paste `ad-awareness.cs` into the **Execute C# Code** sub-action.
3. Open the `ad watch logic` action and paste `ad-watch-logic.cs` into its **Execute C# Code** sub-action.
4. Click **Compile** on each.
5. Trigger the `Twitch → Ads` event to test (right-click the trigger and choose **Test Trigger**).

## Customizing messages

Edit these lines in `ad-watch-logic.cs`:

- Line sending `"Ads have started, see you in ..."` — message shown when ads begin.
- Line sending `"Ads are over KAPOW Welcome back to the stream!"` — message shown when ads end.

You can swap `KAPOW` for any Twitch emote you prefer. On YouTube/Kick, the text will appear without the emote.

## How it works

- `ad-awareness.cs` reads the ad notification from Twitch, computes start and finish epoch times, and stores them in non-persisted global variables.
- It enables the `ad awareness timer` and the `ad watch logic` action.
- `ad-watch-logic.cs` runs repeatedly on that timer. It checks the current time against the stored epochs and:
  - Schedules the timer to fire right when ads start.
  - Sends the "ads have started" message when ads begin.
  - Sends the "ads are over" message when ads end, then disables itself.

## Support

If you need help, join [my Discord community](https://736.fyi/discord) and I'll provide free support.
