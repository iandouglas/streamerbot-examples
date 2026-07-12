# !static-commands

A cross-platform Streamer.bot chat command that lists every static response command currently loaded from [`stream-setup/static-chat-responses-_loadData.cs`](../../../stream-setup/static-chat-responses-_loadData.cs).

This action reads the global variable `ID736StaticChatResponses` and echoes the command names back to chat so viewers know what `!command` responses are available.

## What it needs

1. The shared helper DLL `iandouglas736.dll` must be referenced in Streamer.bot. See [`dlls-needed/README.md`](../../../dlls-needed/README.md) for setup instructions.
2. The static responses loader must have run at least once so `ID736StaticChatResponses` exists. That loader is normally triggered by your stream-start action.

## Streamer.bot setup

1. Create a new Command, e.g. `!static`.
   - **Mode:** `Single`
   - **Sources:** enable the platforms you want (Twitch, YouTube, Kick, etc.)
2. Create a new Action and set the command as its trigger.
3. Add **Core → C# → Execute C# Code**.
4. Paste the contents of [`static-commands.cs`](static-commands.cs).
5. Click **Compile**. If it fails, confirm `iandouglas736.dll` is referenced.

## How it behaves

- If no static responses are loaded, it replies:
  `No static commands are loaded right now.`
- If responses exist, it sends one or more chat messages like:
  `Static commands: !discord, !email, !github, !linkedin, ...`
- The output is split into multiple messages if it would exceed Twitch's chat length limit.
- It works on Twitch, YouTube, and Kick through the `id736.Chat` helper.

## Customization

- Change the command trigger in Streamer.bot to whatever you prefer (`!commands`, `!static`, etc.).
- To change the prefix text or length limit, edit the `prefix` and `maxMessageLength` values in `static-commands.cs`.
