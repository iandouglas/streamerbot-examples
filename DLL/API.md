# iandouglas736 API Reference

(this document is AI generated based on the code I've been building for this compiled DLL)

This document lists every public helper in the `iandouglas736` DLL, with short examples taken from the open-source projects in this repository.

For build instructions see [`README.md`](./README.md).

---

## Installing the DLL in Streamer.bot

1. Build or download `iandouglas736.dll`.
2. In Streamer.bot, open **Settings → C# Compile Settings**.
3. In the **Common References** area, right-click and choose **Add Reference**.
4. Browse to `iandouglas736.dll` and select it.
5. Click **OK** and restart Streamer.bot if needed.

After that, every `Execute C# Code` sub-action can use the helpers below with:

```csharp
using iandouglas736;
```

---

## Quick start pattern

Most helpers require a one-time `SetContext(CPH)` call at the start of your action:

```csharp
using iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        Chat.SetContext(CPH);
        Groups.SetContext(CPH);
        Points.SetContext(CPH);
        PlatformConfig.SetContext(CPH);

        Chat.SendMessage("Hello from iandouglas736!");
        return true;
    }
}
```

`Media`, `Data`, and `GoogleSheets` do **not** need a context because they do not call Streamer.bot APIs.

---

## Helper overview

| Helper | Needs `CPH` context | Purpose |
|--------|----------------------|---------|
| `Chat` | Yes | Send messages and replies on the right platform |
| `Groups` | Yes | Cross-platform user group management |
| `Points` | Yes | Award currency on Twitch/YouTube/Kick user variables |
| `PlatformConfig` | Yes | Enable/disable platforms per action via global vars |
| `Media` | No | Get audio/video file durations |
| `Data` | No | Convert JSON to nested dictionaries with inferred types |
| `GoogleSheets` | No | Read public Google Sheets as nested dictionaries |

---

## Chat

```csharp
public static class Chat
```

### Methods

| Method | Description |
|--------|-------------|
| `SetContext(IInlineInvokeProxy cph)` | Required before using any `Chat` method. |
| `SendMessage(string message, bool fromBot = true)` | Sends to the platform that triggered the action. |
| `SendMessageTo(string platform, string message, bool fromBot = true)` | Sends to a specific platform by name. |
| `SendMessageTo(Platform platform, string message, bool fromBot = true)` | Sends to a specific `Platform` enum value. |
| `SendReplyOrMessage(string message, string msgId = null, bool fromBot = true)` | Sends a Twitch reply if `msgId` is present, otherwise sends a normal message. |
| `GetCurrentPlatform()` | Returns `"twitch"`, `"youtube"`, or `"kick"` from `userType`. |
| `GetCurrentPlatformEnum()` | Returns the current platform as a `Platform` enum. |
| `GetCurrentUserName()` | Returns the login name from `userName` (falls back to `user`). |

### Example: per-platform response from the Higher or Lower game

```csharp
Chat.SetContext(CPH);
string userName = Chat.GetCurrentUserName();
Platform platform = Chat.GetCurrentPlatformEnum();

Chat.SendMessage($"@{userName} joined the game!");
```

---

## Groups

```csharp
public static class Groups
```

### Methods

| Method | Description |
|--------|-------------|
| `SetContext(IInlineInvokeProxy cph)` | Required before using any `Groups` method. |
| `EnsureGroup(string groupName)` | Creates the group if it does not already exist. |
| `IsInGroup(string userName, string groupName)` | True if the user is in the group on any platform. |
| `IsInGroup(string userName, Platform platform, string groupName)` | True if the user is in the group on the given platform. |
| `AddUser(string userName, Platform platform, string groupName)` | Adds the user to the group on a specific platform. |
| `AddUser(string userName, string platform, string groupName)` | Same as above, but accepts a string platform. |
| `RemoveUser(string userName, string groupName)` | Removes the user from the group on all platforms. |
| `Clear(string groupName)` | Removes all members from the group. |
| `Count(string groupName)` | Counts members in the group. |

### Example: allow one join per user across platforms

```csharp
Chat.SetContext(CPH);
Groups.SetContext(CPH);

string userName = Chat.GetCurrentUserName();
if (Groups.IsInGroup(userName, "higher-lower-group"))
{
    Chat.SendMessage("You've already joined!");
    return true;
}

Groups.AddUser(userName, Chat.GetCurrentPlatformEnum(), "higher-lower-group");
Chat.SendMessage($"@{userName} welcome to Higher or Lower!");
```

---

## Points

```csharp
public static class Points
```

### Methods

| Method | Description |
|--------|-------------|
| `SetContext(IInlineInvokeProxy cph)` | Required before using any `Points` method. |
| `Get(string userName, Platform platform, string currencyName)` | Returns the user's balance, defaulting to `0`. |
| `Get(string userName, string platform, string currencyName)` | Same, but accepts a string platform. |
| `Set(string userName, Platform platform, string currencyName, int value)` | Sets the balance. |
| `Set(string userName, string platform, string currencyName, int value)` | Same, but accepts a string platform. |
| `Add(string userName, Platform platform, string currencyName, int amount)` | Adds points and returns the new total. |
| `Add(string userName, string platform, string currencyName, int amount)` | Same, but accepts a string platform. |
| `Subtract(...)` | Subtracts points, optionally allowing negative balances. |

### Example: award participation points from Higher or Lower

```csharp
Chat.SetContext(CPH);
Points.SetContext(CPH);

string userName = Chat.GetCurrentUserName();
Platform platform = Chat.GetCurrentPlatformEnum();
int total = Points.Add(userName, platform, "points", 100);

Chat.SendMessage($"@{userName} now has {total} points!");
```

---

## PlatformConfig

```csharp
public static class PlatformConfig
```

### Methods

| Method | Description |
|--------|-------------|
| `SetContext(IInlineInvokeProxy cph)` | Required before using any `PlatformConfig` method. |
| `AllPlatforms` | Read-only list: `twitch`, `youtube`, `kick`. |
| `GetEnabledPlatforms(string varName = "iandouglas736_enabled_platforms")` | Reads comma-separated platform list from a global var. |
| `GetEnabledPlatformsForAction(string actionName)` | Reads per-action override, falling back to the global default. |
| `IsCurrentPlatformEnabled(string actionName = null)` | True if the current trigger's platform is enabled. |
| `IsPlatformEnabled(string platform, string actionName = null)` | True if the given platform is enabled. |
| `SetEnabledPlatforms(string platforms, string varName)` | Helper to set the global default list. |
| `SetEnabledPlatformsForAction(string actionName, string platforms)` | Helper to set a per-action list. |

### Example: disable a command on Kick

Set a global variable in Streamer.bot:

| Name | Value |
|------|-------|
| `iandouglas736_higherlower_enabled_platforms` | `twitch,youtube` |

Then in code:

```csharp
PlatformConfig.SetContext(CPH);
if (!PlatformConfig.IsCurrentPlatformEnabled("higherlower"))
    return true;
```

---

## Media

```csharp
public static class Media
```

No `SetContext` is needed.

### Methods

| Method | Description |
|--------|-------------|
| `LengthInSeconds(string filename)` | Returns the duration in seconds or `null`. |
| `LengthInSeconds(string filename, Func<string, double?> customProvider)` | Same, with an optional custom provider tried first. |
| `Length(string filename)` | Returns a `TimeSpan?` duration. |
| `IsMediaFile(string filename)` | True for common audio/video extensions. |
| `NLayerMp3Provider(string filename)` | Forces the NLayer MP3 provider. |
| `DurationMineMp4Provider(string filename)` | Forces the Duration.Mine.Mp4 provider. |

`LengthInSeconds` tries, in order:

1. NLayer for `.mp3` and Duration.Mine.Mp4 for `.mp4` if those DLLs are present
2. `ffprobe` if FFmpeg is on your PATH
3. Windows Shell properties for common media files
4. Returns `null` if no provider can read the file

### Example: wait for a victory video to finish

From `twitch/games/i-have-the-highground`:

```csharp
using iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        string filename = "C:/OBS/clips/victory.mp4";
        string scene = "Gameplay";
        string source = "VictoryVideo";

        double? seconds = Media.LengthInSeconds(filename);
        if (!seconds.HasValue)
            return true;

        CPH.ObsSetMediaSourceFile(scene, source, filename);
        CPH.ObsSetSourceVisibility(scene, source, true);
        CPH.Wait((int)(seconds.Value * 1000));
        CPH.ObsSetSourceVisibility(scene, source, false);

        return true;
    }
}
```

---

## Time

```csharp
public static class Time
```

No `SetContext` is needed.

### Methods

| Method | Description |
|--------|-------------|
| `NowEpoch()` | Current Unix time in seconds. |
| `NowEpochMs()` | Current Unix time in milliseconds. |
| `NowUtc()` | Current UTC `DateTimeOffset`. |
| `ToEpoch(DateTimeOffset dateTime)` | Converts a `DateTimeOffset` to epoch seconds. |
| `FromEpoch(long epochSeconds)` | Converts epoch seconds to a `DateTimeOffset`. |
| `EpochsEqual(long a, long b)` | Strict equality. |
| `EpochsSimilar(long a, long b, int driftSeconds)` | True if `a` and `b` are within +/- drift. |
| `CompareEpoch(long epoch1, long epoch2)` | Returns `"X ago"`, `"in X"`, or `"now"`. |
| `Ago(long epochSeconds)` | Returns `"X ago"` relative to now. |
| `Until(long epochSeconds)` | Returns `"in X"` relative to now. |

### Example: ad awareness timer

From `twitch/ad-awareness`:

```csharp
using iandouglas736;

long adStart = CPH.GetGlobalVar<long>("adsStartEpoch", false);
long now = Time.NowEpoch();

if (now < adStart)
{
    Chat.SendMessage($"Ads start {Time.Until(adStart)}.");
}
else
{
    Chat.SendMessage($"Ads started {Time.Ago(adStart)}.");
}
```

---

## Data

```csharp
public static class Data
```

No `SetContext` is needed.

### Methods

| Method | Description |
|--------|-------------|
| `JsonToNestedDictionary(string json)` | Parses JSON into `Dictionary<string, object>` with inferred types. |
| `JsonToNestedList(string json)` | Parses a JSON array into `List<object>`. |
| `JsonFileToNestedDictionary(string filePath)` | Reads a JSON file and converts it. |
| `GetValue(Dictionary<string, object> dict, string path)` | Gets a nested value by dot path; returns `null` if missing. |
| `GetValue<T>(Dictionary<string, object> dict, string path)` | Same, but casts to `T` with fallback to `default(T)`. |

### Example: read a JSON API response

```csharp
string json = CPH.GetGlobalVar<string>("my_api_response", false);
var data = Data.JsonToNestedDictionary(json);

string name = Data.GetValue<string>(data, "user.display_name");
int followers = Data.GetValue<int>(data, "user.followers_count");
var firstFriend = Data.GetValue(data, "friends.0.name");
```

### Example: load persistent game state

From the old chatbot and game code:

```csharp
string json = CPH.GetGlobalVar<string>("highGroundClaimCounts", true);
var counts = string.IsNullOrWhiteSpace(json)
    ? new Dictionary<string, int>()
    : Data.JsonToNestedDictionary(json) as Dictionary<string, int>;
```

---

## GoogleSheets

```csharp
public static class GoogleSheets
```

No `SetContext` is needed. Works only with **public** Google Sheets.

### Methods

| Method | Description |
|--------|-------------|
| `ReadFile(string url, string sheetName = "Sheet1", SheetDuplicateKeyMode mode = LastEntryWins, int timeoutSeconds = 30)` | Downloads and parses a sheet. |
| `ReadFileAsync(...)` | Async version for use outside Streamer.bot actions. |
| `BuildCsvExportUrl(string url, string sheetName = "Sheet1")` | Converts a browser URL into a CSV export URL. |

### Duplicate key modes

| Mode | Behavior |
|------|----------|
| `SheetDuplicateKeyMode.LastEntryWins` | Later rows overwrite earlier rows (default). |
| `SheetDuplicateKeyMode.BuildList` | Repeated keys become a `List<Dictionary<string, object>>`. |

Cell values are inferred: `"42"` → `long`, `"3.14"` → `double`, `"true"` → `bool`, empty → `null`.

### Example: load a media catalog from a Google Sheet

From `__old_code_SB_v0.2/bit-redeem-media-catalog`:

```csharp
using iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        string url = args["GoogleSheetURL"].ToString();

        var catalog = GoogleSheets.ReadFile(url, "Sheet1", SheetDuplicateKeyMode.BuildList);

        foreach (var kvp in catalog)
        {
            string filename = kvp.Key;

            if (kvp.Value is List<Dictionary<string, object>> list)
            {
                // multiple choices for this key
            }
            else if (kvp.Value is Dictionary<string, object> single)
            {
                string scene = Data.GetValue<string>(single, "OBSScene");
                string source = Data.GetValue<string>(single, "OBSSource");
            }
        }

        CPH.SetGlobalVar("ID736MediaCatalog", catalog, false);
        return true;
    }
}
```

---

## Real-world examples in this repo

| Project | Helpers used |
|---------|--------------|
| `multi-platform/games/higher-lower` | `Chat`, `Groups`, `Points`, `PlatformConfig` |
| `twitch/games/i-have-the-highground` | `Media` |
| `__old_code_SB_v0.2/bit-redeem-media-catalog` | `GoogleSheets`, `Media`, `Data` |
| `__old_code_SB_v0.2/chatgpt-chatbot-response` | `Data` |

These examples will be updated as the open-source actions are refactored to use the DLL.

---

## Notes

- The API is still evolving. Backward-incompatible changes are possible until a stable 1.0 release.
- All helpers are `static` so they can be called from Streamer.bot's isolated `Execute C# Code` blocks.
- Helpers that need `CPH` throw a clear `InvalidOperationException` if you forget `SetContext`.
