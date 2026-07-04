# Streamer.bot TypeScript WebSocket Client Notes

## What this package is

`@streamerbot/client` is the official TypeScript / JavaScript client for the
Streamer.bot WebSocket API. It runs in the browser, in Node.js, and inside
bundlers like Vite. Use it to build browser sources, interactive overlays,
standalone games, control panels, or Node.js integrations that talk to
Streamer.bot in real time.

## What these notes cover

- Documentation site pages captured: **16**
- Public client methods catalogued: **27**
- Event sources catalogued: **31**
- Event types catalogued: **369**
- Type definition files catalogued: **19**
- Built-in examples captured: **3**
- Twitch event payload types captured: **49**
- YouTube event payload types captured: **2**

## Core capabilities

1. **Connect** to the Streamer.bot WebSocket server (`ws://127.0.0.1:8080` by default).
2. **Subscribe** to real-time events from Twitch, YouTube, OBS, Streamlabs, etc.
3. **Execute** Streamer.bot actions by id/name with custom arguments.
4. **Request** data: actions, commands, globals, active viewers, broadcaster info, emotes, credits, etc.
5. **Send** chat messages (authenticated WebSocket required).
6. **Bridge** custom events between C# code inside Streamer.bot and your client.

## Target use cases emphasized here

- Interactive HTML pages and browser overlays for streams.
- Games or widgets driven by chat, channel point redemptions, follows, subs, etc.
- OBS-aware clients that react to scene changes, streaming state, or recording state.
- Vue.js dashboards using the `@streamerbot/vue` composable.

## Important local files

- `index.json`: top-level manifest with counts and file paths.
- `scraped-site/`: full mirror of the documentation site markdown source.
- `source-analysis/methods.json`: every public method on `StreamerbotClient`.
- `source-analysis/events.json`: every event source and type the client can subscribe to.
- `source-analysis/twitch-event-types.json`: payload field reference for Twitch events.
- `source-analysis/youtube-event-types.json`: payload field reference for YouTube events.
- `source-analysis/toolkit.json`: structure of the official Streamer.bot Toolkit demo app.
- `cookbook.md`: ready-to-adapt code recipes for overlays, games, and OBS.
- `obs-integration.md`: OBS-aware overlay and remote-control patterns.
- `usage-patterns/interactive-html-and-obs.md`: broader patterns for overlays, games, and OBS integration.

## Official resources

- Site: https://streamerbot.github.io/client/
- Repo: https://github.com/Streamerbot/client
- NPM: https://www.npmjs.com/package/@streamerbot/client
