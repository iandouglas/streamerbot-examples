# 13bis-Partage_StreamerBot

## Source

- **Repository:** `agentic/3rd-party/13bis-Partage_StreamerBot`
- **Author:** 13bis

## Repository Activity

| Metric | Value |
|--------|-------|
| Latest commit | `ab8874d` — 2025-04-22T20:58:20+02:00 — "Delete Post To Mastodon IMG ALT text edition/import.sb" |
| Branch count | 1 (`main`) |
| Active branches | `main` |
| Age | ~2 months old at time of analysis (very recent) |

## What the Project Does

This repo shares two small Streamer.bot action collections:

1. **Collaborative Avertissement** — A content-warning / "avertissement" system for French-language streams.
   - Commands: `!avert` / `!avertissement` (on), `!avertoff` (off), `!AvertAdd` / `!addavert` (add topic).
   - Shows/hides an OBS source named `AVERT` in scene `INTERFACE`.
   - Sets a Twitch stream marker when warnings start/end.
   - Reads warning topics from a text file and posts them in chat.
   - Includes a small C# snippet that appends new warning topics to the text file.

2. **Post To Mastodon IMG ALT text edition** — Posts a message (and optional image with alt text) to a Mastodon instance.
   - Uses `HttpClient` to call the Mastodon API (`/api/v1/statuses` and `/api/v1/media`).
   - Supports image upload with alt text.
   - Converts `||` in the message text to newlines.

## Files in the Repo

| File | Purpose |
|------|---------|
| `README.md` | Links to the two action sets |
| `LICENSE` | License file |
| `Collaborative Avertissement/READ_ME` | French setup instructions |
| `Collaborative Avertissement/actions/I05WLC~F` | Streamer.bot import string (decoded: 4 actions, 1 queue, 4 commands, 1 timer, 1 C# snippet) |
| `Post To Mastodon IMG ALT text edition/READ_ME` | Brief description |
| `Post To Mastodon IMG ALT text edition/Actions/import.sb` | Streamer.bot import string (decoded: Mastodon poster with async HTTP + image upload) |

## Decoded C# Code

### Collaborative Avertissement

One small C# sub-action in the import string (~17 lines effective):

```csharp
using System;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
        string input = args["rawInput"].ToString();
        string user = args["userName"].ToString();
        string filePath = args["wordsAvert"].ToString();

        using (StreamWriter outputFile = new StreamWriter(filePath, true))
            outputFile.Write(input + ", ");

        return true;
    }
}
```

It appends the raw chat input to a text file of warning topics.

### Post To Mastodon

~162 decoded lines of C#. Key capabilities:

- Async `HttpClient` initialization.
- Bearer-token auth to Mastodon.
- Optional image upload with alt text.
- JSON POST to publish a status.
- Converts `||` delimiters to newlines.

## Code Capability

| Area | Assessment |
|------|------------|
| Integrations | Mastodon API (modern REST), OBS source toggle, Twitch stream markers |
| Commands | 4 chat commands, all `1_AVERT` group, mod/admin restricted for on/off |
| Complexity | Low-to-moderate; the Mastodon poster is the most advanced piece |
| Reusability | Mastodon snippet could be adapted for other REST APIs |

## Scoring

| Category | Score | Notes |
|----------|-------|-------|
| Recency | 50 | Latest commit within 6 months. |
| C# LOC | 5 | Decoded C# ~179 total lines (17 + 162). |
| Capability | 12 | OBS, Twitch markers, Mastodon API, file I/O, async HTTP. |
| **Total** | **67** | |

## Notes

- No `.cs` files in the repo directly; all logic is inside Streamer.bot import strings.
- The import strings decode cleanly to JSON action definitions plus base64 C# bytecode.
- The Mastodon action expects arguments `mastodonAccessToken`, `mastodonBaseUrl`, `postMessage`, `postIMG`, and `postALT`.
