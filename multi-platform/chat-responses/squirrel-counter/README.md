# Squirrel Counter

A cross-platform chat command that counts how many times the streamer or chat has been distracted by a squirrel.

## Requirements

This action uses the shared helper DLL `iandouglas736.dll`. Follow the install steps in the main [`README.md`](../../../README.md) or [`dlls-needed/README.md`](../../../dlls-needed/README.md).

## Files

| File | Purpose |
|---|---|
| `squirrel-counter.cs` | The action code. |

## Setup

1. Create a new Action in Streamer.bot.
2. Add an **Execute C# Code** sub-action.
3. Paste the contents of `squirrel-counter.cs`.
4. Set a trigger:
   - Twitch/YouTube/Kick chat command `!squirrel`
   - Channel point reward
   - Bit redeem
   - A stream deck button or any other trigger

## How it works

Each time the action runs:

1. It reads the persisted global variable `ID736SquirrelCount`.
2. It increments the counter by 1.
3. It sends a chat message to the platform that triggered it:
   - On **Twitch**: `SabaPing <user> has spotted a squirrel! We have seen <count> squirrels so far!`
   - On **YouTube** and **Kick**: `<user> has spotted a squirrel! We have seen <count> squirrels so far!`

The counter survives Streamer.bot restarts because it is stored as a persisted global variable.

## Resetting the counter

If you want to reset the count, open the Streamer.bot **Global Variables** panel and delete or set `ID736SquirrelCount` to `0`.
