# Agentic Setup Scripts

This folder contains scripts that set up and maintain the environment so an AI coding agent can behave as a Streamer.bot expert.

## Purpose

The parent repository (`streamerbot-examples`) stores working Streamer.bot projects, C# snippets, and import strings for streamers. To answer deep questions about Streamer.bot internals, commands, triggers, timers, queues, and the `CPH` API, the agent needs a fresh, local copy of the official documentation sources.

The scripts here automate that maintenance:

- **Sync official docs + wiki** — pull the latest changes and all remote work branches from the separate `streamerbot-docs` and `streamerbot-wiki` git repositories.
- **Rebuild the summary** — run the documentation crawler in `../streamerbot-docs-summary` to produce searchable JSON and readable Markdown.
- **Validate outputs** — check that the generated files are consistent and complete.

## Scripts

| Script | What it does | How to run |
|--------|--------------|------------|
| `sync-streamerbot-docs.sh` | Clones/updates both doc repos on the default branch, fetches every remote branch, regenerates the summary, and validates it. | `bash agentic-setup-scripts/sync-streamerbot-docs.sh` |

## Intended use

- **Local development:** run the sync script before a long coding session so the agent has the freshest docs snapshot.
- **Scheduled automation:** a GitHub Actions workflow (`.github/workflows/weekly-docs-sync.yml`) runs the same script weekly so the `streamerbot-docs-summary` folder stays current without manual work.
- **Agent context:** when the agent is asked to design Streamer.bot actions, it should search `streamerbot-docs-summary` first, then fall back to the online sources only if more detail is needed.

## Notes

- The `streamerbot-docs` and `streamerbot-wiki` directories are independent git repositories nested inside this project. They are not git submodules.
- The sync script fetches all remote branches so the summary can report in-progress work via `work-branches.json`, but the generated reference files are built from the default branch (currently `main`).
