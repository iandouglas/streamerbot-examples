# Streamer.bot Quick Reference

Use this folder first before going back to the online docs.

## Primary entrypoints

- `./agentic/streamerbot-docs-summary/index.json` — master manifest of all generated files.
- `./agentic/streamerbot-docs-summary/all-pages.json` — full local searchable page catalog.
- `./agentic/streamerbot-docs-summary/overview.md` — tool overview and high-level guidance.

## When the question is about...

- **Inline C# methods** → `api-calls/csharp-methods.json`
- **CPH classes and enums** → `api-calls/csharp-classes.json`, `api-calls/csharp-enums.json`
- **Trigger variables** → `api-calls/triggers.json`
- **Built-in sub-actions** → `api-calls/sub-actions.json`
- **HTTP control** → `api-calls/http-api.json`
- **WebSocket control/events** → `api-calls/websocket-api.json`
- **UDP control** → `api-calls/udp-api.json`
- **General docs/guides** → `api-calls/guide-pages.json`
- **Official examples** → `api-calls/examples.json`
- **Commands** → `topic-commands.json` / `topic-commands.md`
- **Triggers/Events** → `topic-triggers.json` / `topic-triggers.md`
- **Timers** → `topic-timers.json` / `topic-timers.md`
- **Queues** → `topic-queues.json` / `topic-queues.md`
- **Best-practice code style** → `csharp-patterns/best-practices.md`
- **Interactive stream control design** → `csharp-patterns/interactive-controls.md`
- **Packaging for non-programmers** → `no-code-packaging.md`

## Important reminders

- Prefer official Streamer.bot docs behavior over generic .NET assumptions.
- For inline code, prefer `CPH.TryGetArg<T>()` over direct `args` access.
- Use action IDs/names, trigger variables, and CPH methods exactly as documented in the local dataset.

## Suggested retrieval order

1. Check `index.json` for the right local file.
2. Search the relevant JSON dataset for the method, trigger, or sub-action name.
3. Use `sourceUrl` from that record only if more context is needed.
