# Ask a Silly Question

A cross-platform chat action that posts a random, lighthearted question to chat. Great as a channel point redeem, bit reward, or any other trigger you want.

## What it does

When the action runs, it picks a random silly question from a built-in list and posts it to chat, mentioning the user who triggered it.

- On **Twitch**, it prepends `TwitchSings` before the mention.
- On **YouTube** and **Kick**, it posts a plain `@username` mention without Twitch emotes.

## Requirements

This action uses the shared helper DLL `iandouglas736.dll`. Follow the install steps in the main [`README.md`](../../../README.md) or [`dlls-needed/README.md`](../../../dlls-needed/README.md).

## How to set it up

1. Create a new Action in Streamer.bot.
2. Add an **Execute C# Code** sub-action.
3. Paste the contents of `silly-questions.cs` into the code editor.
4. Set a trigger:
   - **Channel point reward:** `Twitch → Channel Rewards → Reward Redemption`
   - **Bits:** `Twitch → Bits → Cheered`
   - **Chat command:** any command trigger (e.g. `!sillyquestion`)
   - **Timed action, keyboard shortcut, or anything else**

The action works for Twitch, YouTube, and Kick chat because it uses `iandouglas736.Chat` helpers.

## Customizing the questions

Open `silly-questions.cs` and edit the `_questions` list near the top of the file. Add, remove, or change entries however you like. Each entry should be a quoted string.

## Files

- `silly-questions.cs` — the action code.
