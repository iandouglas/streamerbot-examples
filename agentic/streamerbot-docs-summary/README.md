# Streamer.bot Docs Summary

This folder is a generated, local expert reference for Streamer.bot. It combines the official docs site (`agentic/streamerbot-docs`) and the community wiki (`agentic/streamerbot-wiki`) into searchable JSON and readable Markdown.

## How it is generated

Run the sync script from the repository root:

```bash
bash agentic/sync-streamerbot-docs.sh
```

This will:

1. Pull the latest `main` branch from both `streamerbot-docs` and `streamerbot-wiki`.
2. Fetch every remote work branch so in-progress changes are recorded in `work-branches.json`.
3. Re-run `build_summary.py` to rebuild all JSON/Markdown artifacts.
4. Run `validate_summary.py` to sanity-check the outputs.

A GitHub Actions workflow (`.github/workflows/weekly-docs-sync.yml`) also runs the same script every Sunday and commits any summary changes.

## Key files

| File | Purpose |
|------|---------|
| `index.json` | Master manifest with counts, source revisions, branch lists, and file paths. |
| `all-pages.json` | Searchable catalog of every captured docs + wiki page. |
| `work-branches.json` | All remote branches in both source repos. |
| `api-calls/*.json` | Structured datasets: C# methods, classes, enums, sub-actions, triggers, HTTP/WebSocket/UDP APIs, guides, examples, and wiki pages. |
| `topic-commands.json` / `topic-commands.md` | Focused reference for chat commands. |
| `topic-triggers.json` / `topic-triggers.md` | Focused reference for events and triggers. |
| `topic-timers.json` / `topic-timers.md` | Focused reference for timed actions. |
| `topic-queues.json` / `topic-queues.md` | Focused reference for action queues. |
| `csharp-patterns/best-practices.md` | Safe, compact C# patterns for Streamer.bot inline actions. |
| `csharp-patterns/interactive-controls.md` | Design notes for Twitch/YouTube interactive controls. |
| `no-code-packaging.md` | Guide for building projects that non-programmer streamers can install out-of-the-box. |
| `QUICK-REFERENCE.md` | Durable lookup note for finding the right dataset. |

## How to use it as an agent

When asked to design Streamer.bot actions, C# code, commands, triggers, timers, or queues:

1. Search the relevant `topic-*.json` or `api-calls/*.json` dataset first.
2. Use `sourceUrl` from a record only if more context is needed.
3. Follow the `no-code-packaging.md` checklist when the deliverable is meant for a non-programmer streamer to install.
