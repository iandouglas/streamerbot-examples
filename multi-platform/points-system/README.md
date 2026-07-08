# Cross-Platform Points System

A simple points/currency system that works across **Twitch, YouTube, and Kick**. Points are stored in platform-specific user variables because Streamer.bot does not share user state between platforms.

## Requirements

These actions use the shared helper DLL `iandouglas736.dll`. Follow the install steps in the main [`README.md`](../../README.md) or [`dlls-needed/README.md`](../../dlls-needed/README.md).

## Files

| File | Purpose |
|---|---|
| `check-points.cs` | Chat command `!points` — shows the user's current balance. |
| `first-words.cs` | Welcomes a user on their first chat message and gives them starting points. |
| `give-points.cs` | Chat command `!give @user 100` — gives points to another user. |
| `give-points-logic.cs` | Shared logic used by the other actions. Adds/subtracts points and posts the result. |
| `timer-give-points.cs` | Timed action that gives points to everyone while the stream is live. |

## How to set up

1. Create a new Action for each file above.
2. Add an **Execute C# Code** sub-action and paste the contents of the `.cs` file.
3. Wire each action to the right trigger:
   - `check-points.cs` — Twitch/YouTube/Kick chat command `!points`.
   - `first-words.cs` — Twitch/YouTube/Kick **First Words** event.
   - `give-points.cs` — Twitch/YouTube/Kick chat command with a Regex like `^!give @?(\S+)\s+(-?\d+)$`.
   - `give-points-logic.cs` — no trigger; run by other actions.
   - `timer-give-points.cs` — a Timed Action. Set `%pointsToAdd%` and `%platform%` arguments, or edit the defaults in the file.

## Cross-platform notes

- Each platform has its own user variable namespace in Streamer.bot, so a Twitch viewer's points are separate from the same user's YouTube or Kick points.
- The currency name is `"points"` by default. You can change it in each file if you want a custom name.
- The `give-points-logic.cs` action needs a `platform` argument to know which platform's user variables to update. The trigger actions pass it automatically.

## Trigger examples

### `!points` command

Create a command named `points` and set its trigger to run `check-points.cs`.

### `!give` command

Create a command with:

- **Mode:** `Regex`
- **Regex:** `^!give @?(\S+)\s+(-?\d+)$`

This matches commands like `!give @someone 100` or `!give someone -50`. The action uses `match[2]` for the recipient and `match[4]` for the amount.

### First Words event

Set the trigger to **Twitch/YouTube/Kick → Chat → First Words**. Set a `%pointsToAdd%` argument before the C# sub-action if you want a custom welcome bonus (default is `100`).

### Timed points

Create a **Timed Action** that runs `timer-give-points.cs` every few minutes. It only gives points when `CPH.ObsIsStreaming()` is true. Pass `%pointsToAdd%` and `%platform%` arguments, or edit the defaults.
