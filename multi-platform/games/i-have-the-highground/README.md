# "I have the high ground!"

Inspired by an idea by [Amish Ace on Twitch](https://twitch.tv/amish_ace), and the Star Wars movie, "Star Wars: Episode III – Revenge of the Sith" with the fight between Obi Wan Kenobi and Anakin Skywalker.

Your chat can use bits, channel points, or any other trigger you like to try to claim the "high ground".

## Requirements

These actions use the shared helper DLL `iandouglas736.dll`. Follow the install steps in the main [`README.md`](../../../README.md) or [`dlls-needed/README.md`](../../../dlls-needed/README.md).

## Files

| File | Purpose |
|---|---|
| `highground-logic.cs` | Runs when someone attempts to claim the high ground. Handles cooldown, win/lose videos, and chat messages. |
| `highground-check.cs` | Chat command `!highground` — shows who currently holds the high ground and how long until the next claim. |
| `hg-leaders.cs` | Chat command `!hgleaders` — shows the top 3 high-ground holders. |

## Setup

1. Create three actions in Streamer.bot:
   - `highground logic` — no trigger, called by your bit/channel point actions.
   - `!highground` — chat command trigger, runs `highground-check.cs`.
   - `!hgleaders` — chat command trigger, runs `hg-leaders.cs`.

2. Create your bit/channel point action(s). They must set these arguments before running `highground logic`:
   - `videoFileWin` — disk path to the win video.
   - `videoFileLose` — disk path to the lose video.
   - `OBSScene` — OBS scene containing the media source.
   - `OBSSource` — OBS media source to play the video on.

3. Set the global variable `highGroundCooldownSeconds` to the cooldown length in seconds. The default is `120`.

## Cross-platform notes

- The actions now use `iandouglas736.Chat` to send messages, so they work on Twitch, YouTube, and Kick.
- Chat messages are sent to the platform that triggered the action.

## How it works

- The first successful claim starts a cooldown.
- During the cooldown, any new claim fails and plays the lose video.
- After the cooldown expires, anyone can claim the high ground again.
- A user's claim count only increments when they take the high ground from someone else (or claim it for the first time).

## Support

If you need help, join [my Discord community](https://736.fyi/discord) and I'll provide free support.
