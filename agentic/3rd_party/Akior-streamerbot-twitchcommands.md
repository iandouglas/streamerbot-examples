# Akior-streamerbot-twitchcommands

## Source

- **Repository:** `agentic/3rd-party/Akior-streamerbot-twitchcommands`
- **Author:** Akior

## Repository Activity

| Metric | Value |
|--------|-------|
| Latest commit | `309345e` — 2025-02-24T10:09:22+03:00 — "Create .gitattributes" |
| Branch count | 1 (`main`) |
| Active branches | `main` |
| Age | ~4 months old at time of analysis (recent) |

## What the Project Does

This repository contains five Streamer.bot **import strings** that implement various chat commands and integrations:

1. **`!vtube.cs`** — VTube Studio / VSeeFace-style interactive commands.
   - Chat commands: `!bonk`, `!pat`, `!vfx`, `!angry`, `!random`, plus several reactive actions without explicit commands (`!cry`, `!bao`, `!white_f`, `!white_f2`).
   - Likely sends hotkey triggers or API calls to a VTubing application.

2. **`!????.cs`** — A `!bite` command.
   - Lets a viewer target another user.
   - If no target is provided, picks a random user.
   - If the target is not a follower, runs a different action.

3. **`bonk?pat.cs`** — Additional `!bonk` / `!pat` variants and follower-check variations.

4. **`RAID.cs`** — Raid/shotout automation.
   - Trigger type `107` (likely Twitch raid event).
   - Actions: `Raid`, `shotout most view`, `shotout last clip`.
   - C# code parses Twitch clip durations, normalizes comma/dot decimal separators, and sets a `timer` global variable.
   - Also exposes `bearerToken` and `clientId` arguments for Twitch API calls.

5. **`youtube.cs`** — YouTube music/playback integration.
   - Regex command `!music <YouTube URL>`.
   - C# extracts `videoId` and start time `t` from the URL.
   - C# converts YouTube's ISO 8601 duration string to seconds using `XmlConvert.ToTimeSpan`.
   - Includes filter actions for watch count, parental controls, and video length.
   - Works with an OBS browser source.

## Files in the Repo

| File | Purpose | Decoded Content |
|------|---------|-----------------|
| `README.md` | Brief description in mixed French/English | 15 lines |
| `LICENSE` | License text | 553 lines |
| `!vtube.cs` | VTube/VTubing command import string | 9 actions, 5 commands, no C# |
| `!????.cs` | `!bite` command import string | 5 actions, 1 command, no C# |
| `bonk?pat.cs` | `!bonk`/`!pat` follower variants | 6 actions, 2 commands, no C# |
| `RAID.cs` | Raid shoutout automation | 3 actions, trigger type 107, 79 C# lines |
| `youtube.cs` | YouTube music command | 5 actions, 1 regex command, 59 C# lines |

## Decoded C# Code Summary

### RAID.cs (~79 lines)

Two very similar snippets parse Twitch clip duration data and store it in a global variable:

```csharp
CPH.SetGlobalVar("timer", formattedDuration, true);
```

A third snippet exposes Twitch OAuth credentials for API calls:

```csharp
CPH.SetArgument("bearerToken", CPH.TwitchOAuthToken);
CPH.SetArgument("clientId", CPH.TwitchClientId);
```

### youtube.cs (~59 lines)

Two snippets:

1. Extract `videoId` and start time from a YouTube URL:
   ```csharp
   string extractedVideoId = query["v"];
   string time = query["t"];
   CPH.SetGlobalVar("videoId", extractedVideoId, true);
   CPH.SetGlobalVar("ttt", time, true);
   ```

2. Convert YouTube duration to seconds:
   ```csharp
   TimeSpan ts = System.Xml.XmlConvert.ToTimeSpan(timer);
   double seconds = ts.TotalSeconds;
   CPH.SetGlobalVar("duration", seconds, true);
   ```

## Code Capability

| Area | Assessment |
|------|------------|
| Integrations | VTube/VTubing hotkeys, Twitch raids/clips API, YouTube URL parsing + OBS browser source |
| Commands | `!bonk`, `!pat`, `!vfx`, `!angry`, `!random`, `!bite`, `!music <YouTube URL>` |
| Complexity | Moderate; the YouTube and raid snippets are the most involved |
| Reusability | YouTube duration/URL parsing is reusable; raid duration normalization is reusable |

## Scoring

| Category | Score | Notes |
|----------|-------|-------|
| Recency | 40 | Latest commit is 4–5 months old (Feb 2025). |
| C# LOC | 5 | ~138 decoded C# lines. |
| Capability | 14 | Multiple command integrations (VTube, Twitch, YouTube, OBS). |
| **Total** | **59** | |

## Notes

- All logic is stored inside Streamer.bot import strings; there are no standalone `.cs` source files.
- The repository predates the current Streamer.bot summaries but aligns with the v0.2.x action format (exported from `0.2.4`/`0.2.5` era based on JSON structure).
- Filenames contain non-ASCII characters, which may cause issues on some filesystems.
