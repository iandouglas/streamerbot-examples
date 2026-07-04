# Agentic Setup Scripts

This folder (`agentic/`) contains scripts, source mirrors, and summaries that let an AI coding agent behave as a Streamer.bot expert.

## Purpose

The parent repository (`streamerbot-examples`) stores working Streamer.bot projects, C# snippets, and import strings for streamers. To answer deep questions about Streamer.bot internals, commands, triggers, timers, queues, and the `CPH` API, the agent needs fresh, local copies of the official documentation sources.

The scripts here automate that maintenance:

- **Sync official docs + wiki** — pull the latest changes and all remote work branches from the separate `streamerbot-docs` and `streamerbot-wiki` git repositories.
- **Rebuild the summary** — run the documentation crawler in `streamerbot-docs-summary` to produce searchable JSON and readable Markdown.
- **Validate outputs** — check that the generated files are consistent and complete.

## Layout

| Path | Contents |
|------|----------|
| `sb-ts-ws-client-summary/` | Structured notes on `@streamerbot/client` for browser overlays, games, and OBS integrations. |
| `streamerbot-docs/` | Clone of the official docs site source. |
| `streamerbot-docs-summary/` | Generated searchable JSON/Markdown summary of the docs + wiki. |
| `streamerbot-typescript-websocket-client/` | Clone of the official TypeScript WebSocket client repo. |
| `streamerbot-wiki/` | Clone of the community wiki source. |

## Scripts

| Script | What it does | How to run |
|--------|--------------|------------|
| `sync-streamerbot-docs.sh` | Clones/updates both doc repos on the default branch, fetches every remote branch, regenerates the summary, and validates it. | `bash agentic/sync-streamerbot-docs.sh` |

## Intended use

- **Local development:** run the sync script before a long coding session so the agent has the freshest docs snapshot.
- **Scheduled automation:** a GitHub Actions workflow (`.github/workflows/weekly-docs-sync.yml`) runs the same script weekly so the `agentic/streamerbot-docs-summary` folder stays current without manual work.
- **Agent context:** when the agent is asked to design Streamer.bot actions or browser overlays, it should search the relevant `agentic/*-summary` folder first, then fall back to the online sources only if more detail is needed.

## Notes

- The `streamerbot-docs` and `streamerbot-wiki` directories are independent git repositories nested inside `agentic/`. They are not git submodules.
- The sync script fetches all remote branches so the summary can report in-progress work via `work-branches.json`, but the generated reference files are built from the default branch (currently `main`).
