# Ian's Stream Setup

When my stream starts, I have a handful of actions that need to run that load in my Google Sheets data, prepare audio files, reset some timers, load my game configurations etc. so I wanted to document those here.

## Before you start

These actions rely on a shared helper DLL called `iandouglas736.dll`. You **must** install it in Streamer.bot or the C# code will not compile.

### What you need

1. Copy the DLL files from [`../dlls-needed`](../dlls-needed) into a folder on your Streamer.bot PC. A common choice is your **Streamer.bot installation folder** or a `DLLs` subfolder inside it. All referenced DLLs must live in the same folder. Common paths:
   - `C:\Program Files\Streamer.bot\`
   - `C:\Program Files\Streamer.bot\DLLs\`
   - A portable folder like `D:\streamerbot\Streamer.bot\DLLs\`
2. In Streamer.bot, open **Settings → C# Compile Settings**.
3. In the **Common References** area, right-click → **Add Reference**.
4. Select at least `iandouglas736.dll`.
5. If you use audio or video rewards, also select:
   - `NLayer.dll` (for `.mp3` duration)
   - `Duration.Mine.Mp4.dll` (for `.mp4` duration)
6. Click **OK** and restart Streamer.bot if needed.

If you split the DLLs across different folders, you may see `FileNotFoundException` at runtime because Streamer.bot resolves assemblies relative to where the main reference is located.

See [`dlls-needed/README.md`](../dlls-needed/README.md) for full details, including how to update the DLL later.

### Compiling the C# code

Every `.cs` file in this folder is meant to be pasted into an **Execute C# Code** sub-action inside a Streamer.bot action.

1. Create a new Action in Streamer.bot.
2. Add any required sub-actions first (like setting arguments).
3. Add **Core → C# → Execute C# Code**.
4. Open the `.cs` file, copy all the text, and paste it into the code window.
5. Click **Compile**. If it fails, make sure the DLL references above are installed.

If you get stuck, come ask for help in [my Discord community](https://736.fyi/discord).

---

## File naming convention

Each feature has two parts:

- **`-loadCSV.cs`** — the loader that reads a Google Sheet tab and stores the data in a global variable.
- **`-execute.cs`** — the action that runs at stream time and uses that data.

For example, `change-background-loadCSV.cs` loads the backgrounds catalog, and `change-background-execute.cs` actually changes the OBS background.

---

## Data Loads from CSV files

These loaders read **public** Google Sheets. The sheet must be shared so anyone with the link can view it. The first row is used as column headers.

### change-background-loadCSV.cs

This loads CSV data from a Google Sheet that allows my viewers to change the animated background on my stream.

**Required sub-action argument:** `GoogleSheetURL`

**Expected columns:** `Filename`, `OBSScene`, `OBSSource`

The loader stores the result in a global variable called `ID736BackgroundsCatalog`.


### channel-point-redeems-loadCSV.cs

This loads CSV data from a Google Sheet for Twitch channel point redeems and media to play when triggered.

**Required sub-action argument:** `GoogleSheetURL`

**Expected columns:** `ChannelRewardName`, `Filename`, `Type`, `OBSScene`, `OBSSource`

The loader stores the result in a global variable called `ID736ChanPtsMediaCatalog`.


### play-emote-audio-loadCSV.cs

This loads CSV data from a Google Sheet for Twitch emotes to play audio sounds.

**Required sub-action argument:** `GoogleSheetURL`

**Expected columns:** `EmoteName`, `Filename`, `Type`, `OBSScene`, `OBSSource`, `Duration`

The loader stores the result in a global variable called `ID736EmoteMediaCatalog`. If `Duration` is blank for `.mp3` or `.mp4` files, it is inferred automatically.


### chat-audio-commands-loadCSV.cs

This loads CSV data from a Google Sheet for chat commands that play audio files — either a specific file or a random file from a folder.

**Required sub-action argument:** `GoogleSheetURL`

**Expected columns:** `ChatCommand`, `AudioPathOrFolder`, `Volume`

The loader stores the result in a global variable called `ID736ChatAudioCommands`.

For each row:

- `ChatCommand` is the chat command, e.g. `!boo`. It is case-insensitive.
- `AudioPathOrFolder` is either:
  - A full path to a single audio file (e.g. `.mp3`, `.wav`). That exact file will play.
  - A folder path. A random audio file from that folder will play.
- `Volume` is optional. Default is `0.5`. Values range from `0.0` to `1.0`.

Example rows:

| ChatCommand | AudioPathOrFolder | Volume |
|---|---|---|
| `!boo` | `//DXP4800PLUS-NAS/ian-misc/livestream_videos/assets/Audio/boo` | `0.5` |
| `!specific` | `//DXP4800PLUS-NAS/ian-misc/livestream_videos/assets/Audio/boo/one-file.mp3` | `0.7` |


### static-chat-responses-loadCSV.cs

This loads CSV data from a Google Sheet for static responses for commands like `!email` or `!linkedin`.

**Required sub-action argument:** `GoogleSheetURL`

**Expected columns:** `ChatCommand`, `ChatResponse`

The loader stores the result in a global variable called `ID736StaticChatResponses`.

---

## Stream Actions

### change-background-execute.cs

This uses a Twitch channel point redeem to change my background to one of several MP4 videos of animations like circuit path traces or hexagons moving around or binary matrixes etc.

To use this, you must set up two subactions to set arguments called `OBSScene` and `OBSSource` where you'll list the OBS scene and source where you want to load the background image. Once you have those defined in subactions, you can add an "Execute C# Code" subaction and paste in the C# code from this file.

The logic in the code will randomly choose a background MP4 file that is not already playing.


### channel-point-redeems-execute.cs

This is for Twitch only.

Add a new Action, and its Trigger will be "Twitch -> Channel Rewards -> Reward Redemption" and leave the reward name as "Any".

Add a subaction for "Execute C# Code" and paste the contents of this file.

Just like the static chat responses, the name of the channel reward in your CSV data must EXACTLY match the full string of how you define your channel point rewards on Twitch (or within Streamer.Bot if you define/manage them there, Platforms -> Twitch -> Channel Point Rewards). For example, building a channel reward called "Change Background" must be the exact string you put in your Google Sheet, otherwise the channel point redemption will not work.


### play-emote-audio-execute.cs

This is the code that actually watches incoming messages to see if an emote in the message matches any of the emotes from my CSV data, and plays an audio file from the CSV data. If more than one audio-capable emote is seen in a message, only the first emote has its audio file played.

There is some logic in here to avoid playing audio for the "emote-stock-game".


### chat-audio-commands-execute.cs

This is the handler for the chat audio commands loaded by `chat-audio-commands-loadCSV.cs`. It works cross-platform (Twitch, YouTube, Kick) because it uses `rawInput` from the chat event, not platform-specific arguments.

To use this, build a Streamer.bot command with:

- **Mode:** `Regex`
- **Regex:** `^!([a-zA-Z0-9_-]*)$`

That regex matches any `!command` containing letters, numbers, underscores, or dashes. Start a new Action, set this command as the Trigger, then add an **Execute C# Code** sub-action and paste the code from this file.

When a command is matched, the handler looks it up in `ID736ChatAudioCommands`. If `AudioPathOrFolder` is a file, it plays that file. If it is a folder, it picks a random audio file from the folder and plays it.


### static-chat-responses-execute.cs

This code will watch any incoming message based on a "regular expression" (aka Regex) command to see if something matches, and will replay a stored static response from the CSV data loaded above.

To use this, build a Streamer.bot command that has a "mode" of "Regex" and set the Regex string to `^!([a-zA-Z0-9_-]*)$` This regex will allow you to match any !command that contains uppercase or lowercase letters, numbers, or a dash.

It's important to note that the !command needs to match the case-sensitivity exactly as you write it in the CSV data from Google Sheets. If you add `!Command` (uppercase C) to your Google Sheet, a chat command of `!command` (lowercase C) will NOT match.

Start a new Action, set this command as the Trigger, then add an "Execute C# Code" subaction and paste in the code from this file.
