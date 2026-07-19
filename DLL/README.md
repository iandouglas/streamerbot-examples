# iandouglas736 Helper DLL

(the readme is mostly AI generated and maintained)

## Note from Ian:

I made this DLL as a "quality of life" way for me to maintain my open-source contributions for Streamer.bot. The source code for the [compiled iandouglas736.DLL](../dlls-needed/) is in this path so you can have full view of all of the code in the DLL, that I'm not doing anything sneaky or shady with your data etc..

I love the idea of open-source, and offer this DLL to use however/wherever you like to make your C# work in Streamer.bot a little easier with a handful of helper methods that I use the most.

The DLL will be needed for most of my open-source extensions here in this repository, and it will rely on the other DLL files distributed in the [DLLs Needed](../dlls-needed/) folder for doing things like figuring out how long MP3 or MP4 files are to play in your streaming software.

---

# Documentation

## What is this DLL file for?

A small C# helper library for [Streamer.bot](https://streamer.bot) actions. It gives streamers a consistent, cross-platform API for common tasks like sending chat messages, managing user groups, and awarding points across **Twitch, YouTube, and Kick**.

---

## What it does

The DLL provides static helper classes under the `iandouglas736` namespace:

| Class | Purpose |
|---|---|
| `iandouglas736.Core` | One-call setup: `Core.LinkStreamerbot(CPH)` sets context on all helpers |
| `iandouglas736.Log` | Write timestamped messages to per-prefix daily log files |
| `iandouglas736.Chat` | Send chat messages / replies to the correct platform |
| `iandouglas736.Groups` | Add/check/clear Streamer.bot user groups across platforms |
| `iandouglas736.Points` | Get/set/add points in platform-specific user variables |
| `iandouglas736.PlatformConfig` | Read global variables to enable/disable platforms per action |
| `iandouglas736.Timers` | Enable/disable/reset Streamer.bot timers |
| `iandouglas736.Media` | Get audio/video file durations; play MP4 in OBS, play MP3 via Streamer.bot |
| `iandouglas736.Data` | Convert JSON into nested dictionaries with inferred types |
| `iandouglas736.GoogleSheets` | Read public Google Sheets as nested dictionaries |
| `iandouglas736.Time` | Unix epoch helpers and human-readable relative times |

---

## Installation

1. Download or build `iandouglas736.dll`.
2. Copy the DLL into your Streamer.bot installation directory.
3. Restart Streamer.bot.

Streamer.bot will load the DLL automatically on startup.

---

## Usage in an action

At the top of every `Execute C# Code` sub-action, set the context:

```csharp
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        // this needs to be near the top of every Execute() method
        // you add in Streamer.bot so my DLL knows how to talk to
        // Streamer.bot, the streaming platforms, etc.
        id736.Core.LinkStreamerbot(CPH);

        id7363.Chat.SendMessage("Hello from all platforms!");
        id736.Log.Message("Action executed", filenamePrefix: "mygame");

        return true;
    }
}
```

`id736.Core.LinkStreamerbot(CPH)` sets context on all helpers that need it. You need to call it once per action that runs C# code.

### Logging setup (required)

Before `Log.Message()` works, configure two persistent global variables in a Streamer.bot **startup action** (triggered on application start):

```csharp
CPH.SetGlobalVar("id736LogPath", "S:/logs", true);
CPH.SetGlobalVar("id736DefaultFilenamePrefix", "iandouglas736", true);
```

If these are not set, `Core.LinkStreamerbot(CPH)` will log errors via `CPH.LogError` on every action, and `Log.Message()` will log errors and skip writing.

---

## Examples

### Send a chat message

```csharp
Chat.SendMessage("Hello chat!");
Chat.SendMessage("Broadcast", false); // send from broadcaster account
```

### Send a reply on Twitch, normal message elsewhere

```csharp
if (!CPH.TryGetArg("msgId", out string msgId)) msgId = null;
Chat.SendReplyOrMessage("Thanks for playing!", msgId);
```

### Add a user to a game group

```csharp
Platform platform = Chat.GetCurrentPlatformEnum();
string userName = Chat.GetCurrentUserName();
Groups.AddUser(userName, platform, "my-game-group");
```

### Check if already joined

```csharp
if (Groups.IsInGroup(userName, "my-game-group"))
{
    Chat.SendMessage("You're already in the game!");
    return true;
}
```

### Award points

```csharp
Platform platform = Chat.GetCurrentPlatformEnum();
int total = Points.Add(userName, platform, "points", 100);
Chat.SendMessage($"{userName} now has {total} points!");
```

### Limit a command to specific platforms

Set a global variable in Streamer.bot:

| Name | Value |
|---|---|
| `iandouglas736_enabled_platforms` | `twitch,youtube` |
| `iandouglas736_higherlower_enabled_platforms` | `twitch` |

Then in code:

```csharp
if (!PlatformConfig.IsCurrentPlatformEnabled("higherlower"))
    return true;
```

### Get a media file's duration

```csharp
double? seconds = Media.LengthInSeconds("C:/Alerts/intro.mp4");
if (seconds.HasValue)
    Chat.SendMessage($"That clip is {seconds.Value:F1} seconds long.");
```

`Media.LengthInSeconds` tries these providers in order:

1. **NLayer** for `.mp3` and **Duration.Mine.Mp4** for `.mp4` if those DLLs are available
2. `ffprobe` if FFmpeg is on your PATH
3. Windows Shell properties for common media files
4. Returns `null` if no provider can read the file

You can also force a specific provider:

```csharp
double? mp3 = Media.LengthInSeconds(filename, Media.NLayerMp3Provider);
double? mp4 = Media.LengthInSeconds(filename, Media.DurationMineMp4Provider);
```

To use the optimized MP3/MP4 path, place the required DLLs in your Streamer.bot directory and add them as Common References alongside `iandouglas736.dll`.

| File type | Required DLL |
|---|---|
| `.mp3` | `NLayer.dll` |
| `.mp4` | `Duration.Mine.Mp4.dll` |

`NLayer.NAudioSupport.dll` is only needed if you also use NAudio's `Mp3FileReader` with NLayer. The `iandouglas736.Media` helper uses `NLayer.MpegFile` directly, so `NLayer.dll` alone is enough for MP3 duration detection.

### Convert JSON to a nested dictionary

```csharp
string json = CPH.GetGlobalVar<string>("my_api_response", false);
var data = Data.JsonToNestedDictionary(json);

string name = Data.GetValue<string>(data, "user.display_name");
int followers = Data.GetValue<int>(data, "user.followers_count");
```

Paths use dots for nested objects and numbers for array indexes:

```csharp
var firstItem = Data.GetValue(data, "items.0.name");
```

### Read a public Google Sheet

```csharp
var catalog = GoogleSheets.ReadFile(
    "https://docs.google.com/spreadsheets/d/XXXX/edit?usp=sharing",
    sheetName: "Sheet1",
    duplicateMode: SheetDuplicateKeyMode.BuildList
);

foreach (var kvp in catalog)
{
    string filename = kvp.Key;
    if (kvp.Value is List<Dictionary<string, object>> list)
    {
        // multiple rows shared the same key
    }
    else if (kvp.Value is Dictionary<string, object> single)
    {
        // only one row for this key
        string scene = Data.GetValue<string>(single, "OBSScene");
    }
}
```

In `BuildList` mode, a repeated key becomes a `List<Dictionary<string, object>>`. In `LastEntryWins` mode (the default), later rows overwrite earlier ones.

Cell values are inferred: `"42"` -> `long`, `"3.14"` -> `double`, `"true"` -> `bool`, empty cells -> `null`.

### Work with epoch times

```csharp
long now = Time.NowEpoch();
long adStart = CPH.GetGlobalVar<long>("adsStartEpoch", false);

string relative = Time.CompareEpoch(now, adStart);
// "in 2 minutes" if now is before adStart
// "3 minutes ago" if now is after adStart

bool exactlySame = Time.EpochsEqual(now, adStart);
bool closeEnough = Time.EpochsSimilar(now, adStart, driftSeconds: 2);

string untilAds = Time.Until(adStart);
string sinceAds = Time.Ago(adStart);
```

---

## Building from source

### Requirements

- **Windows:** [.NET SDK](https://dotnet.microsoft.com/download) and the [.NET Framework 4.8.1 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net481).
- **Linux / macOS:** [.NET SDK](https://dotnet.microsoft.com/download) and [Mono](https://www.mono-project.com/download/stable/) (provides the .NET Framework 4.8.1 reference assemblies and compiler support).
- A copy of **Streamer.bot** installed somewhere on your system.
- The path to `Streamer.bot.Plugin.Interface.dll`.

### Before building

The project file expects Streamer.bot to live at:

```text
C:/Program Files/Streamer.bot
```

If yours is different, either:

- Edit `iandouglas736.csproj` and change `<StreamerBotPath>`.
- Or pass it on the command line with `/p:StreamerBotPath=...` (Windows) or `-p:StreamerBotPath=...` (cross-platform).

### Windows

Open a PowerShell prompt in the `DLL/src` folder:

```powershell
dotnet build iandouglas736.csproj -c Release
```

Or, if you need to override the Streamer.bot path:

```powershell
dotnet build iandouglas736.csproj -c Release -p:StreamerBotPath="D:/StreamerBot"
```

The output DLL will be at:

```text
DLL/src/bin/Release/net481/iandouglas736.dll
```

Copy it to your Streamer.bot directory.

### Linux

Install the .NET SDK for your distribution, then run:

```bash
cd DLL/src
dotnet build iandouglas736.csproj -c Release -p:StreamerBotPath=/path/to/streamer.bot
```

> Note: .NET Framework 4.8.1 builds require the Windows targeting pack or [Mono](https://www.mono-project.com/download/stable/) on Linux. If `dotnet build` cannot target `net481`, install Mono and use:

```bash
msbuild iandouglas736.csproj /p:Configuration=Release /p:StreamerBotPath=/path/to/streamer.bot
```

Or target .NET Standard 2.0 / .NET 6/8 instead by editing `TargetFramework` in the `.csproj` file. Streamer.bot itself runs on .NET Framework, so referencing its interface from a .NET Standard library usually works as long as the public API surface is compatible.

### macOS

Install the .NET SDK or Mono, then run the same commands as Linux:

```bash
cd DLL/src
dotnet build iandouglas736.csproj -c Release -p:StreamerBotPath=/Applications/Streamer.bot.app/Contents/MacOS
```

If `net481` is unavailable, use Mono:

```bash
msbuild iandouglas736.csproj /p:Configuration=Release /p:StreamerBotPath=/Applications/Streamer.bot.app/Contents/MacOS
```

---

## Notes

- This DLL is a **work in progress**. The API may change as more helpers are added.
- The helpers intentionally use `Core.LinkStreamerbot(CPH)` so they work inside Streamer.bot's isolated `Execute C# Code` sandbox.
- `Media` duration helpers do not need a context. `Media` playback helpers (`PlayMp4InObs`, `PlayMp3`) require `Core.LinkStreamerbot(CPH)`. `Data` and `GoogleSheets` do **not** need a context because they do not call Streamer.bot APIs.
- Platform detection relies on the `userType` argument that Streamer.bot provides on chat triggers.
- Points are stored in platform-specific user variables because Streamer.bot does not have a single global user variable across platforms.

---

## License

Add your license here.
