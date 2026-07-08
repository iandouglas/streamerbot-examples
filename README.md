# Streamer.bot Examples

This is a respository of things I've built or tweaked for [Streamer.bot](https://Streamer.bot) for myself or other streamers on Twitch/YouTube.

## How to use it

Each folder has a README with setup instructions. Most examples include `.cs` files that you paste into an **Execute C# Code** sub-action inside Streamer.bot. We no longer ship `import_code.txt` import strings; instead, the READMEs walk you through creating the action, trigger, and sub-actions step by step.

> **Stuck?** Come ask for free help in [my Discord community](https://736.fyi/discord). I hang out there regularly and I'm happy to troubleshoot setup, compilation errors, or anything else.

If you make a helpful improvement, please open a pull request!

## iandouglas736 Helper DLL

Many of the newer examples in this repo use a shared helper DLL called `iandouglas736.dll`. It provides common, cross-platform helpers for:

- Sending chat messages on the right platform (Twitch, YouTube, Kick)
- Managing user groups across platforms
- Awarding points/currency in the correct platform's user variables
- Enabling or disabling commands per platform
- Reading media file durations
- Converting JSON to nested dictionaries
- Reading public Google Sheets as nested dictionaries

### Why a DLL?

Streamer.bot runs each `Execute C# Code` sub-action in its own isolated class. That makes it hard to share helper methods between actions without duplicating code. The DLL lets me write reusable helpers once and call them from any action.

### Quick start in C#

The DLL's namespace is `iandouglas736`. You can reference helpers directly:

```csharp
using iandouglas736;

Chat.SetContext(CPH);
Chat.SendMessage("Hello chat!");
```

Or use a short alias if you prefer:

```csharp
using id736 = iandouglas736;

id736.Chat.SetContext(CPH);
id736.Chat.SendMessage("Hello chat!");
```

### Where to get the DLL

- **Source code:** [`./DLL`](./DLL) — open-source, same license as this repo.
- **Compile instructions:** See [`DLL/README.md`](./DLL/README.md).
- **Pre-built binaries and required dependencies:** See [`./dlls-needed`](./dlls-needed).

### Quick install

1. Copy `iandouglas736.dll` (and any optional media DLLs you need) into a folder on your PC. A common choice is your **Streamer.bot installation folder** or a `DLLs` subfolder inside it. All referenced DLLs must live in the same folder.
2. In Streamer.bot, go to **Settings → C# Compile Settings**.
3. In the **Common References** area, right-click and choose **Add Reference**.
4. Select `iandouglas736.dll` and any media DLLs you want to use.
5. Click **OK** and restart Streamer.bot if needed.

See [`dlls-needed/README.md`](./dlls-needed/README.md) for a complete list of files and where to put them.

## Yes, it's open-source

If you find a way to improve it, please share it back here. I'm not an expert at C# or Streamer.bot, I hack things 'til they work then move on to the next thing.

## Yes, I'm here to help

Probably on my [live stream](https://twitch.tv/iandouglas736) but you can contact me as `iandouglas736` on most social media platforms, or come join [my Discord community](https://736.fyi/discord).

## What's the /agentic folder?

I work a LOT with various AI tools, and I have them running through Streamer.bot documentation, wiki pages and code examples on a regular basis to make sure I always have the latest and greatest documentation at my fingertips.

I figured that all the code I made is open-sourced, so I might as well share the documentation summaries for others to use, too.

### My AI Committment

That said, the code I share here is my own. AI will assist with code completion (based on this documentation etc), but I never go "full AI mode" and get AI to do everything for me. It definitely helps improve the speed and improves how thorough my README files will be.

Also, any AI tools are fully localized on my own network as I do my Streamer.bot work, with occasional cloud models if I'm traveling and have an idea.

## Warranty, etc.

No warranty. You're on your own. I disclaim all responsibility if you use any of my code.

## Copyright

Please look at the `LICENSE` file. The information here is free to use, but you can't include it in commercial (paid) bundles of any kind, and you must attribute the work to me with a link back to this repository.
