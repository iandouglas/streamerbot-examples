# Streamer.bot Notes

## What Streamer.bot is

Streamer.bot is an event-driven automation tool for livestreamers. The official docs position it around actions, triggers, variables, platform connections, stream app integrations, and an embedded C# execution surface for custom logic.

## What the official docs cover

- Total Streamer.bot markdown pages captured: **1228**
- API reference pages captured: **1061**
- Guide pages captured: **47**
- Get started pages captured: **7**
- Example pages captured: **15**
- Changelog pages captured: **62**

## API coverage captured locally

- C# guide + recipe pages: **9**
- C# method reference pages: **195**
- Trigger reference pages: **408**
- Sub-action reference pages: **345**
- HTTP API pages: **10**
- WebSocket API pages: **10**
- UDP API pages: **3**

## C# code model

- Inline code runs inside a `CPHInline` class.
- `Execute()` is the required entrypoint and returns `bool` to continue or stop downstream sub-actions.
- `Init()` and `Dispose()` are optional lifecycle hooks for setup and cleanup.
- The `CPH` object exposes Streamer.bot methods for actions, users, Twitch, YouTube, OBS, variables, logging, and more.
- The docs explicitly recommend `CPH.TryGetArg<T>()` over direct `args` access for safety.

## Best-practice emphasis from the docs

- Prefer official `CPH` methods and documented argument patterns over ad-hoc state access.
- Use `TryGetArg<T>()` or safe dictionary checks for trigger/action arguments.
- Treat `Execute()` return values carefully because `false` stops remaining sub-actions unless the sub-action is configured to save the result instead.
- Use `Init()` for one-time setup and `Dispose()` for cleanup where long-lived objects are involved.
- Use persisted globals intentionally and validate null/default behavior when reading them back.

## Important local files

- `index.json`: top-level manifest for fast lookup.
- `all-pages.json`: compact searchable catalog of all captured Streamer.bot docs pages.
- `api-calls/*.json`: structured API datasets by area.
- `csharp-patterns/*.md`: opinionated notes for writing reliable inline Streamer.bot code.

## High-value guide areas

- `Actions` — Configuration of actions, sub-actions, and queues
- `Triggers` — Overview of trigger configuration in Streamer.bot
- `Variables` — Usage of arguments and variables in your Streamer.bot actions
- `Commands` — Define and configure your chat commands
- `Hot Keys` — Execute your actions with keyboard shortcuts or mouse inputs
- `Voice Control` — Execute actions with your own voice!
- `MIDI` — Configure MIDI I/O with Streamer.bot
- `Import & Export` — Import ready-to-use functionality from the community or share your own creations.
- `Backup & Restore` — Easily fix things when they go wrong!
- `Credits` — Configure the built-in Credits system in Streamer.bot
- `Quotes` — Interacting with the built-in quote system in Streamer.bot
- `Timers` — Configure triggers to execute your actions at specific intervals

## Official examples captured

- `Examples` — https://docs.streamer.bot/examples
- `AutoHotKey Actions` — https://docs.streamer.bot/examples/autohotkey
- `Chat Message Timer` — https://docs.streamer.bot/examples/chat-message-timer
- `Chatbot Commands` — https://docs.streamer.bot/examples/chatbot-commands
- `Simple Counter` — https://docs.streamer.bot/examples/counter
- `OBS Raw Requests in C#` — https://docs.streamer.bot/examples/csharp_obsraw
- `cURL POST Requests` — https://docs.streamer.bot/examples/curl-requests
- `Execute Scripts` — https://docs.streamer.bot/examples/execute-scripts
- `Advanced "Fetch URL"` — https://docs.streamer.bot/examples/http-post
- `AI Chat Command` — https://docs.streamer.bot/examples/ollama-chat-command
- `Parse JSON Utility` — https://docs.streamer.bot/examples/parse-json-utility
- `Quotes Commands for v1.0.0+` — https://docs.streamer.bot/examples/quotes-commands
