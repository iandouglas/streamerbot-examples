# Streamer.bot Documentation Crawl Worklog

## Completed

- [x] Inspect repository structure and choose the output location (`agentic/streamerbot-docs-summary/`).
- [x] Clone/mirror official docs (`agentic/streamerbot-docs`) and community wiki (`agentic/streamerbot-wiki`).
- [x] Build a crawler that ingests both docs and wiki Markdown into a unified, searchable catalog.
- [x] Extract API calls, parameters, variables, headings, code blocks, and C# usage details.
- [x] Write structured summaries for fast local lookup (`index.json`, `all-pages.json`, `api-calls/*.json`).
- [x] Add topic-focused indexes for commands, triggers, timers, and queues.
- [x] Add a no-code packaging guide for shipping Streamer.bot projects to non-programmer streamers.
- [x] Capture all remote work branches in `work-branches.json` so in-progress docs are visible.
- [x] Create `agentic/sync-streamerbot-docs.sh` to refresh both mirrors and regenerate the summary.
- [x] Create `.github/workflows/weekly-docs-sync.yml` to run the sync every Sunday and commit updates.
- [x] Validate generated files with `validate_summary.py`.

## Next ideas

- [ ] Add a similar structured summary for `agentic/streamerbot-typescript-websocket-client`.
- [ ] Generate a cross-referenced "cookbook" that pairs common streamer needs (e.g., "chat game") with the exact triggers, sub-actions, and C# methods needed.
- [ ] Add a JSON search index or embedding-friendly chunked output for LLM retrieval.
