# Interactive Controls for Twitch and YouTube

## Recommended architecture

- Trigger receives the platform event.
- A small C# action validates args and normalizes the event into a stable internal shape.
- The action writes or reads the minimum shared state needed from globals.
- Follow-up actions handle side effects such as chat output, OBS changes, counters, reward updates, or overlays.

## Why this maps well to Streamer.bot

- It aligns with the docs' actions + triggers + variables model.
- It keeps `CPHInline` code focused on logic instead of UI orchestration.
- It makes testing easier because each action contract is smaller.

## Guardrails

- Validate platform-specific args before doing stateful work.
- Keep remote-triggered actions on explicit allowlists of action IDs or names.
- Prefer fixed variable names for shared state and document them in the action description.
- Avoid direct `args` indexing in hot paths when `TryGetArg<T>()` is available.

## Use the local datasets

- Check `api-calls/triggers.json` for event variables.
- Check `api-calls/sub-actions.json` for built-in action building blocks.
- Check `api-calls/csharp-methods.json` for inline code methods.
- Check `api-calls/http-api.json`, `websocket-api.json`, and `udp-api.json` for remote entrypoints.
