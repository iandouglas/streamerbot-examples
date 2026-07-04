# TakeJoshyy-StreamerBot-ImportString-Decoder

## Source

- **Repository:** `agentic/3rd-party/TakeJoshyy-StreamerBot-ImportString-Decoder`
- **GitHub:** https://takejoshyy.github.io/StreamerBot-Decoder/ (live page)
- **Authors:** TakeJoshyy, WhazzItToYa, LeBluxTv

## Repository Activity

| Metric | Value |
|--------|-------|
| Latest commit | `862981b` — 2025-04-27T17:57:53Z — "Update index.html" |
| Branch count | 1 (`main`) |
| Active branches | `main` |
| Age | ~2 months old at time of analysis (very recent) |

## What the Project Does

This is a **browser-based utility** for decoding Streamer.bot action import strings. Streamer.bot exports actions as a Base64-encoded, gzip-compressed JSON blob. The tool lets a user paste that blob (or drag-and-drop a file) and see:

1. The 4-byte header signature (hex, ASCII, UInt32 LE).
2. The decompressed top-level JSON structure.
3. Any C# `byteCode` fields decoded back into readable C# source.
4. A list of sub-actions (e.g., file I/O, Fetch URL) extracted from the JSON.

It is **not** a Streamer.bot plugin or action itself — it is a helper for inspecting imports before importing them into Streamer.bot.

## How the Decoder Works

The JavaScript logic in `index.html` performs the following steps:

1. **Base64 decode** the input string to raw bytes.
2. **Skip the first 4 bytes** (a header/signature).
3. **Gunzip** the remaining bytes to produce a JSON string.
4. **Parse the JSON** and recursively walk it.
5. **Base64-decode every `byteCode` field** found under action sub-actions and render the resulting C# code with syntax highlighting.
6. **Collect sub-action metadata** (`type`, file paths, URLs, etc.) and display them alongside the decoded code.

Key functions:

- `decodeBase64ToUint8Array(base64)` — converts Base64 to raw bytes.
- `decodeFirst4Bytes(base64)` — inspects the 4-byte header.
- `decodeTopLayer(base64Input)` — strips header and gunzips the rest.
- `traverseActions(obj, path)` — recursively extracts `byteCode`, sub-action types, and triggers.
- `syntaxHighlightCSharp(entry)` — renders C# with highlight.js.

## Files in the Repo

| File | Purpose | Lines |
|------|---------|-------|
| `index.html` | Full decoder UI + JavaScript logic | ~380 lines (HTML + JS) |
| `README.md` | Usage instructions | ~12 lines |

## Code Capability

- **No C# code** in the repository itself.
- **JavaScript decoder** understands the Streamer.bot export format well enough to recover C# sub-action code and JSON action definitions.
- Useful for auditing imports from other developers before importing them into Streamer.bot.

## Scoring

Using the rubric (recency 0–50, C# LOC 0–30, capability 0–20):

| Category | Score | Notes |
|----------|-------|-------|
| Recency | 50 | Latest commit within 6 months. |
| C# LOC | 0 | No C# files; JS decoder only. |
| Capability | 10 | Decodes Streamer.bot imports, exposes C# bytecode, recognizes sub-action types. |
| **Total** | **60** | |

## Notes

- This repo is primarily a **reference tool**. It does not contain runnable Streamer.bot actions, but its decoder logic can be reused if we ever need to programmatically decode import strings in other repos.
- The decoder confirms that Streamer.bot import strings are: `Base64(gzip(JSON with base64-encoded C# byteCode))` after a 4-byte header.
