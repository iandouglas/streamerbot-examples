# Streamer.bot Examples

This is a respository of things I've built or tweaked for [Streamer.bot](https://Streamer.bot) for myself or other streamers on Twitch/YouTube.

[![Linkedin: iandouglas736](https://img.shields.io/badge/-iandouglas736-blue?style=round-square&logo=Linkedin&logoColor=white&link=https://www.linkedin.com/in/iandouglas736/)](https://www.linkedin.com/in/iandouglas736/)

[![GitHub iandouglas](https://img.shields.io/github/followers/iandouglas?label=follow&style=social)](https://github.com/iandouglas )


## How to use it

Each folder has a README with setup instructions. Most examples include `.cs` files that you paste into an **Execute C# Code** sub-action inside Streamer.bot. The READMEs walk you through creating the action, trigger, and sub-actions step by step.

> **Stuck?** Come ask for free help in [my Discord community](https://736.fyi/discord). I hang out there regularly and I'm happy to troubleshoot setup, compilation errors, or anything else.

If you make a helpful improvement, please open a pull request!

## iandouglas736 Helper DLL

Most of the examples in this repo use a shared helper DLL called `iandouglas736.dll` which is found in the [ddls-needed](./dlls-needed) folder. It provides common, cross-platform helpers for:

- Sending chat messages on the right platform (Twitch, YouTube, Kick)
- Managing user groups across platforms
- Awarding points/currency in the correct platform's user variables
- Enabling or disabling commands per platform
- Reading media file durations
- Converting JSON to nested dictionaries
- Reading public Google Sheets as nested dictionaries

## Setup

Using the DLL file will require some quick setup, but you only need to do this once:

In Streamer.bot, make a new Action, call it "___Streamer.bot Start" The underlines in the name will make it show up at the very top of the list of Actions.

For the Trigger, right click in the Trigger area, go to "Add > Core > Streamer.bot Started"

In the Sub-Actions, you're going to add 2 things:

1. "Add > Core > Globals > Global (set)". Make sure this is "Persisted", "Global", and set to "Auto Type". The "Variable Name" will be `id736LogPath` and its value should be a folder somewhere on your system where you want custom log files. This will be extremely helpful for debugging. The folder will already need to exist. It's okay if there are spaces in the folder path. I recommend changing the common Windows backslash `\` to a forward slash `/` so `C:\custom logs` would become `C:/custom logs`. The backslash can do interesting things in software.

2. "Add > Core > Globals > Global (set)". Make sure this is "Persisted", "Global", and set to "Auto Type". The "Variable Name" will be `id736DefaultFilenamePrefix` and you can set this whatever you like, but I recommend `iandouglas736`. I'll explain why in a moment.

That's all you need. Whenever Streamer.bot is started, it will make sure that these two variables are set in Streamer.bot's peristent global memory.

### How are these used?

When you want to log something, you could use Streamer.bot's logging functions like `CPH.LogDebug("my message here");` but if you have a very busy list of actions, that message is going to be buried in a massive log file.

These variables will allow you to write log files to a disk path of your choosing (that's the `id736LogPath` variable), and by default it will make a filename using the "default filename prefix" to make a filename like "iandouglas736_20260128.txt" in that folder. The "prefix" will be the first part of the filename, followed by an underscore, followed by the system date of your Windows machine in YYYYMMDD format, so July 18th 2026 would be "20260718", followed by ".txt" as a file extension so you can open this in Notepad or any other text editor.

---

### Why Make a Custom DLL?

Streamer.bot runs each `Execute C# Code` sub-action in its own isolated class. That makes it hard to share helper methods between actions without duplicating code, and updating that code (like how to log to an external file) might mean updating that code in dozens of places. Having a custom DLL lets me write reusable helpers once and call them from any action just by "using" my DLL.

### Quick start in C#

The DLL's namespace is `iandouglas736`. You can reference helpers directly:

```csharp
using iandouglas736;

Chat.SetContext(CPH);
Chat.SendMessage("Hello chat!");
```

But I prefer using a short alias:

```csharp
using id736 = iandouglas736;

id736.Chat.SetContext(CPH);
id736.Chat.SendMessage("Hello chat!");
```

This will allow you to use other DLL files from other developers who might also have a `Chat` module or `Points` module or `Media` module, and now they won't conflict with my library.


### Where to get the DLL

I have a pre-compiled version of the DLL in the [dlls-needed](./dlls-needed) folder for convenience, but I also provide the source code so you can review everything my code is doing and compile your own version.

For the developers out there, the DLL version I provide will typically be a 'debug' build to make using a debugger a little easier.

- **Source code:** [`./DLL`](./DLL) — open-source, same license as this repo.
- **Compile instructions:** See [`DLL/README.md`](./DLL/README.md).
- **Pre-built binaries and required dependencies:** See [`./dlls-needed`](./dlls-needed).

## Installation of iandouglas.DLL

See [`dlls-needed/README.md`](./dlls-needed/README.md) for a complete list of files and where to put them, how to update them and more.

## Yes, it's open-source

If you find a way to improve anything in this repo, whether it's the DLL or any of the games or extensions, please share it back here.

## Yes, I'm here to help

Drop by my [live stream](https://twitch.tv/iandouglas736) or contact me as `iandouglas736` on most social media platforms, or come join [my Discord community](https://736.fyi/discord).

## What's the /agentic folder?

I work a LOT with various AI tools, and I have them running through Streamer.bot documentation, wiki pages and code examples on a regular basis to make sure I always have the latest and greatest documentation.

I figured that all the code I made is open-sourced, so I might as well share the documentation summaries for others to use, too.

### My AI Committment

That said, the code I share here is my own. AI will assist with code completion (based on this documentation etc), but I never go "full AI mode" and get AI to do everything for me. It definitely helps improve the speed and improves how thorough my README files will be.

Also, any AI tools are fully localized on my own network as I do my Streamer.bot work, with occasional cloud models if I'm traveling and have an idea.

## Warranty, etc.

No warranty. You're on your own. I disclaim all responsibility if you use any of my code.

## Copyright

Please look at the `LICENSE` file. The information here is free to use, but you can't include it in commercial (paid) bundles of any kind, and you must attribute the work to me with a link back to this repository.

## Want to support my work?

You can [support me here on GitHub](https://github.com/sponsors/iandouglas), or [buy me a coffee](buymeacoffee.com/iandouglas).