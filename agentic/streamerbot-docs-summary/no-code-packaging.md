# Packaging Streamer.bot Projects for Non-Programmer Streamers

The goal is for a streamer to copy an import string, paste it into Streamer.bot, and have a working project with sensible defaults and easy configuration.

## One-paste install

- Generate a Streamer.bot import string for every release. Put it in `import_code.txt` in the project folder.
- Keep the import self-contained: actions, commands, timers, queues, variables, and OBS sources/browser sources it needs.
- Test the import in a clean Streamer.bot profile before publishing.

## Configuration without code

- Surface all tunables through Streamer.bot globals (e.g., `ProjectName::Enabled`, `ProjectName::CooldownSeconds`).
- Provide a single 'Settings' action that validates and sets those globals with clear labels.
- Use chat commands only for runtime control; first-time setup should happen through the Settings action or a simple JSON config file read by a C# sub-action.
- Document every global in the README with copy-paste names so streamers do not have to guess.

## Friendly defaults

- Default to safe, low-risk values (long cooldowns, moderator-only commands, disabled optional features).
- Add a 'First Time Setup' command that prints the current configuration and usage instructions.
- Prefer built-in sub-actions over custom C# when a built-in one exists; it makes the project easier to audit and modify.

## Compact, powerful C# when needed

- Keep C# blocks small and focused on one job: parse input, read/write globals, decide what to do, return true/false.
- Let actions handle presentation: chat messages, OBS visibility, sound playback, overlays.
- Expose helper methods inside the CPHInline class so complex logic can be unit-tested mentally by reading one screen.
- Log configuration errors clearly so a streamer can report them without reading code.

## Commands, triggers, timers, queues

- Commands: use groups and clear names. Provide aliases only when they do not conflict with common bot commands.
- Triggers: subscribe only to events the project actually uses; avoid catch-all triggers that fire constantly.
- Timers: randomize interval slightly or use the built-in timer sub-action to avoid chat-pattern predictability.
- Queues: name queues after the project so they are easy to find. Document whether a queue is meant to be paused/resumed manually.

## Distribution checklist

- [ ] README explains what the project does and who it is for.
- [ ] `import_code.txt` is present and tested on the target Streamer.bot version.
- [ ] `README.md` has a 'Quick Start' section: paste, configure globals, enable.
- [ ] A list of required permissions (Twitch scopes, OBS connection, etc.) is included.
- [ ] Troubleshooting section covers the most common failure modes.
- [ ] Optional: a preview video or screenshots of the setup steps.

## Reference these local files while building

- `topic-commands.json` / `topic-commands.md`
- `topic-triggers.json` / `topic-triggers.md`
- `topic-timers.json` / `topic-timers.md`
- `topic-queues.json` / `topic-queues.md`
- `api-calls/csharp-methods.json`
- `api-calls/sub-actions.json`
