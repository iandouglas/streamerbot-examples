"""Build a local summary of the Streamer.bot TypeScript WebSocket client.

This script aggregates:
- The documentation site source (apps/docs/content)
- The client source code (packages/client/src)
- The Vue integration source (packages/vue/src)
- Real-world example usage (examples/ and apps/toolkit/src)

It emits structured JSON and Markdown notes into this directory so the client
library can be used confidently for browser overlays, interactive HTML pages,
games, and other Streamer.bot / OBS integrations.
"""

from __future__ import annotations

import json
import re
import shutil
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

WORKSPACE_ROOT = Path(__file__).resolve().parent.parent
OUTPUT_ROOT = Path(__file__).resolve().parent
SOURCE_ROOT = WORKSPACE_ROOT / "streamerbot-typescript-websocket-client"
DOCS_ROOT = SOURCE_ROOT / "apps" / "docs" / "content"
CLIENT_SRC = SOURCE_ROOT / "packages" / "client" / "src"
VUE_SRC = SOURCE_ROOT / "packages" / "vue" / "src"


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, default=str), encoding="utf-8")


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def copy_docs() -> dict[str, Any]:
    """Mirror the docs content tree into scraped-site/."""
    scraped_dir = OUTPUT_ROOT / "scraped-site"
    if scraped_dir.exists():
        shutil.rmtree(scraped_dir)
    scraped_dir.mkdir(parents=True, exist_ok=True)

    files_copied: list[dict[str, Any]] = []
    for src in sorted(DOCS_ROOT.rglob("*")):
        if src.is_file():
            rel = src.relative_to(DOCS_ROOT)
            dst = scraped_dir / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
            files_copied.append(
                {"source": str(rel), "path": str(dst.relative_to(OUTPUT_ROOT))}
            )

    return {"count": len(files_copied), "files": files_copied}


def parse_frontmatter(text: str) -> tuple[dict[str, Any], str]:
    if text.startswith("---"):
        parts = text.split("---", 2)
        if len(parts) >= 3:
            fm_text = parts[1]
            body = parts[2]
            try:
                import yaml

                fm = yaml.safe_load(fm_text) or {}
            except Exception:
                fm = {"raw_frontmatter": fm_text}
            return fm, body
    return {}, text


def extract_public_methods(client_ts: str) -> list[dict[str, Any]]:
    """Parse public methods from StreamerbotClient.ts along with JSDoc."""
    methods: list[dict[str, Any]] = []

    # Find all public async/property declarations
    pattern = re.compile(
        r"(?P<jsdoc>(?:\s*/\*\*[\s\S]*?\*/)?)\s*"
        r"public\s+(?:async\s+)?(?P<name>[a-zA-Z_$][\w$]*)\s*"
        r"(?P<signature>\([^)]*\))"
        r"(?::\s*(?P<return>[^\{]+))?",
        re.MULTILINE,
    )

    for match in pattern.finditer(client_ts):
        jsdoc = match.group("jsdoc") or ""
        name = match.group("name")
        signature = match.group("signature")
        return_type = (match.group("return") or "").strip()

        # Skip constructors and getters that aren't useful API surface
        if name == "constructor" or name in ("authenticated", "ready"):
            continue

        description = ""
        for line in jsdoc.splitlines():
            line = line.strip().lstrip("*").strip()
            if line and not line.startswith("@"):
                description = line
                break

        params: list[dict[str, str]] = []
        for line in jsdoc.splitlines():
            line = line.strip().lstrip("*").strip()
            m = re.match(r"@param\s+(?P<name>\S+)\s*[-]?\s*(?P<desc>.*)", line)
            if m:
                params.append(
                    {"name": m.group("name"), "description": m.group("desc").strip()}
                )

        methods.append(
            {
                "name": name,
                "signature": f"{name}{signature}",
                "returnType": return_type,
                "description": description,
                "params": params,
                "source": "packages/client/src/ws/StreamerbotClient.ts",
            }
        )

    # De-duplicate and sort
    seen: set[str] = set()
    unique: list[dict[str, Any]] = []
    for m in methods:
        if m["name"] not in seen:
            seen.add(m["name"])
            unique.append(m)

    return sorted(unique, key=lambda x: x["name"])


def extract_event_sources(events_const: str) -> dict[str, list[str]]:
    """Parse the StreamerbotEvents const into a source->types map."""
    # Find the object literal body
    match = re.search(
        r"export\s+const\s+StreamerbotEvents\s*=\s*\{(.*)\}\s*as\s+const;",
        events_const,
        re.DOTALL,
    )
    if not match:
        return {}

    body = match.group(1)
    result: dict[str, list[str]] = {}
    # Match each key and its string array
    for src_match in re.finditer(r'"([^"]+)"\s*:\s*\[([^\]]*)\]', body):
        source = src_match.group(1)
        types_str = src_match.group(2)
        types = re.findall(r'"([^"]+)"', types_str)
        result[source] = types
    return dict(sorted(result.items()))


def extract_type_file(path: Path) -> dict[str, Any]:
    """Return a simple structural summary of a TypeScript type file."""
    text = read_text(path)
    exports: list[str] = re.findall(
        r"export\s+(?:type|interface)\s+([A-Za-z0-9_]+)", text
    )
    consts: list[str] = re.findall(r"export\s+const\s+([A-Za-z0-9_]+)", text)
    return {
        "file": str(path.relative_to(SOURCE_ROOT)),
        "exports": exports,
        "consts": consts,
    }


def extract_client_types() -> list[dict[str, Any]]:
    types_dir = CLIENT_SRC / "ws" / "types"
    summaries: list[dict[str, Any]] = []
    for path in sorted(types_dir.rglob("*.ts")):
        summaries.append(extract_type_file(path))
    return summaries


def extract_examples() -> list[dict[str, Any]]:
    examples_root = SOURCE_ROOT / "examples"
    results: list[dict[str, Any]] = []
    for example_dir in sorted(examples_root.iterdir()):
        if example_dir.is_dir():
            files = sorted(
                p.relative_to(example_dir)
                for p in example_dir.rglob("*")
                if p.is_file()
            )
            results.append(
                {
                    "name": example_dir.name,
                    "path": str(example_dir.relative_to(SOURCE_ROOT)),
                    "files": [str(f) for f in files],
                }
            )
    return results


def extract_vue_composables() -> dict[str, Any]:
    return {
        "composables": [
            str(p.relative_to(SOURCE_ROOT)) for p in sorted(VUE_SRC.rglob("*.ts"))
        ],
        "components": [
            str(p.relative_to(SOURCE_ROOT))
            for p in (VUE_SRC / "components").rglob("*.vue")
        ]
        if (VUE_SRC / "components").exists()
        else [],
    }


def build_overview(counts: dict[str, Any]) -> str:
    return f"""# Streamer.bot TypeScript WebSocket Client Notes

## What this package is

`@streamerbot/client` is the official TypeScript / JavaScript client for the
Streamer.bot WebSocket API. It runs in the browser, in Node.js, and inside
bundlers like Vite. Use it to build browser sources, interactive overlays,
standalone games, control panels, or Node.js integrations that talk to
Streamer.bot in real time.

## What these notes cover

- Documentation site pages captured: **{counts["docsPages"]}**
- Public client methods catalogued: **{counts["methods"]}**
- Event sources catalogued: **{counts["eventSources"]}**
- Event types catalogued: **{counts["eventTypes"]}**
- Type definition files catalogued: **{counts["typeFiles"]}**
- Built-in examples captured: **{counts["examples"]}**

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
- `source-analysis/types.json`: index of all TypeScript type definition files.
- `QUICK-REFERENCE.md`: fast lookup for common tasks and snippets.
- `usage-patterns/interactive-html-and-obs.md`: patterns for overlays, games, and OBS integration.

## Official resources

- Site: https://streamerbot.github.io/client/
- Repo: https://github.com/Streamerbot/client
- NPM: https://www.npmjs.com/package/@streamerbot/client
"""


def build_quick_reference(
    method_names: list[str], event_sources: dict[str, list[str]]
) -> str:
    common_methods = [
        ("new StreamerbotClient(opts)", "Create and optionally auto-connect."),
        (
            "client.on('Twitch.ChatMessage', cb)",
            "Subscribe to an event with auto-subscription.",
        ),
        ("client.subscribe({ Twitch: ['ChatMessage'] })", "Manual subscription."),
        ("client.unsubscribe('*')", "Unsubscribe from all events."),
        ("client.getActions()", "List all Streamer.bot actions."),
        (
            "client.doAction(id, args, opts)",
            "Execute an action; supports customEventResponse.",
        ),
        ("client.getBroadcaster()", "Fetch connected broadcaster platform info."),
        ("client.getActiveViewers()", "Fetch current Twitch/YouTube active viewers."),
        (
            "client.getGlobals() / getGlobal(name)",
            "Fetch persisted or non-persisted globals.",
        ),
        (
            "client.sendMessage(platform, message, opts)",
            "Send chat; requires authenticated WebSocket.",
        ),
        (
            "client.executeCodeTrigger(name, args)",
            "Fire a custom C# trigger from the client.",
        ),
        ("client.getInfo()", "Get Streamer.bot instance info."),
    ]

    lines = ["# Streamer.bot Client Quick Reference\n"]

    lines.append("## Common snippets\n")
    for sig, desc in common_methods:
        lines.append(f"- `{sig}` — {desc}")
    lines.append("")

    lines.append("## Minimal browser overlay\n")
    lines.append("""```html
<!DOCTYPE html>
<html>
  <head>
    <script src="https://cdn.jsdelivr.net/npm/@streamerbot/client/dist/streamerbot-client.js"></script>
  </head>
  <body>
    <div id="chat"></div>
    <script>
      const client = new StreamerbotClient();
      client.on('Twitch.ChatMessage', ({ data }) => {
        document.getElementById('chat').innerHTML +=
          `<div><b>${data.message.displayName}</b>: ${data.message.message}</div>`;
      });
    </script>
  </body>
</html>
```
""")

    lines.append("## Event source quick list\n")
    for source in sorted(event_sources):
        lines.append(f"- `{source}` — {len(event_sources[source])} types")
    lines.append("")

    lines.append("## Important event sources for overlays/games\n")
    important = [
        "Twitch",
        "YouTube",
        "Obs",
        "Misc",
        "General",
        "Custom",
        "Streamlabs",
        "StreamElements",
        "CrowdControl",
    ]
    for source in important:
        if source in event_sources:
            types = event_sources[source]
            lines.append(f"### {source}")
            for t in types:
                lines.append(f"- `{source}.{t}`")
            lines.append("")

    return "\n".join(lines)


def build_usage_patterns() -> str:
    return """# Usage Patterns: Interactive HTML Pages, Games, and OBS

This file collects practical patterns for using `@streamerbot/client` in the
context the site is built for: browser overlays, interactive games, and
Streamer.bot / OBS integrations.

## 1. Browser overlay basics

Browser overlays are plain HTML/JS pages loaded as a Browser source in OBS or
as a dock in Streamer.bot. The CDN build is the fastest way to start:

```html
<script src="https://cdn.jsdelivr.net/npm/@streamerbot/client/dist/streamerbot-client.js"></script>
```

In the page:

```js
const client = new StreamerbotClient({
  host: '127.0.0.1',
  port: 8080,
  endpoint: '/',
  autoReconnect: true,
});
```

### Reliable overlay rules

- Always set `autoReconnect: true` so OBS or a reload doesn't lose the link.
- Keep DOM updates inside `requestAnimationFrame` when many events fire quickly.
- Use CSS classes and `transform` for animations; avoid layout thrashing.
- Destroy listeners on page hide to prevent memory leaks:
  `window.addEventListener('beforeunload', () => client.disconnect());`

## 2. Reacting to chat / redeems / subs

The `client.on()` helper auto-subscribes and gives typed event payloads:

```js
client.on('Twitch.ChatMessage', ({ event, data, timeStamp }) => {
  const { displayName, message, color } = data.message;
  addChatMessage(displayName, message, color);
});

client.on('Twitch.RewardRedemption', ({ data }) => {
  if (data.reward.title === 'Launch Firework') {
    launchFirework(data.user.displayName);
  }
});
```

For high-frequency events (cheers, mass subs) consider batching DOM updates:

```js
let queue = [];
client.on('Twitch.Cheer', ({ data }) => queue.push(data));
setInterval(() => {
  if (!queue.length) return;
  renderCheers(queue);
  queue = [];
}, 100);
```

## 3. Driving games from Streamer.bot

Two-way pattern:

1. **Streamer.bot -> game**: send a custom event from a C# action with
   `CPH.WebsocketBroadcastJson(json)`, then listen on the client:

   ```js
   client.on('General.Custom', ({ data }) => {
     if (data.event === 'spawnEnemy' && data.enemyType) {
       spawnEnemy(data.enemyType);
     }
   });
   ```

2. **Game -> Streamer.bot**: call `client.doAction(id, args)` from the game:

   ```js
   client.doAction('my-game-over-action', {
     score: player.score,
     winner: player.name,
   });
   ```

Use `customEventResponse: true` when the game needs data back from the action:

```js
const res = await client.doAction('get-high-scores', {}, { customEventResponse: true });
const scores = res.customEventResponseArgs?.scores ?? [];
```

On the Streamer.bot side, end the action with the **Custom Event Trigger**
sub-action and include the `%sbClientResponse%` argument in the payload.

## 4. OBS-aware clients

Subscribe to OBS events from the same WebSocket connection:

```js
client.on('Obs.SceneChanged', ({ data }) => {
  updateOverlayForScene(data.sceneName);
});

client.on('Obs.StreamingStarted', () => showLiveIndicator(true));
client.on('Obs.StreamingStopped', () => showLiveIndicator(false));
client.on('Obs.RecordingStarted', () => showRecIndicator(true));
client.on('Obs.RecordingStopped', () => showRecIndicator(false));
```

Use `Application.*` to refresh action lists when actions are edited:

```js
client.on('Application.*', async () => {
  const actions = await client.getActions();
  populateActionPicker(actions.actions);
});
```

## 5. Reading and writing shared state

Read global variables for overlays:

```js
const latest = await client.getGlobal('latestFollower');
console.log(latest.variable.value);
```

For write-back, use a Streamer.bot action or C# code triggered from the client:

```js
client.doAction('update-overlay-state', {
  scene: currentScene,
  round: currentRound,
});
```

## 6. Vue.js dashboards

The `@streamerbot/vue` package wraps the client in a reactive composable:

```vue
<script setup>
import { useStreamerbot } from '@streamerbot/vue';

const { client, status, data, error, connect, disconnect } = useStreamerbot({
  subscribe: '*',
});
</script>
```

`data` is a ref that updates on every incoming message; `status` is
`'OPEN' | 'CONNECTING' | 'CLOSED'`.

## 7. Security and connection tips

- The default WebSocket is unauthenticated. Enable a password in Streamer.bot
  `Servers/Clients > WebSocket Server` and pass it as `password`.
- For remote tunnels use `scheme: 'wss'`.
- `immediate: false` lets you create the client and call `connect()` later.
- The client auto-reconnects with exponential-ish backoff (1s per attempt up
  to 30s). Set `retries` to limit attempts.

## 8. Common pitfalls

- Subscriptions only take effect if the WebSocket server is enabled and
  running in Streamer.bot.
- `sendMessage` requires an **authenticated** WebSocket connection.
- Event names use the format `Source.Type` (e.g. `Twitch.ChatMessage`).
- `client.on()` accepts wildcards (`Twitch.*`, `*`), but the client still
  expands them into real subscriptions before sending to Streamer.bot.
- The `Raw` source emits internal `Action`, `SubAction`, and `ActionCompleted`
  events for introspection.
"""


def main() -> None:
    generated_at = datetime.now(timezone.utc).isoformat()

    # 1. Copy docs
    docs_meta = copy_docs()

    # 2. Extract source facts
    client_ts = read_text(CLIENT_SRC / "ws" / "StreamerbotClient.ts")
    methods = extract_public_methods(client_ts)

    events_const = read_text(CLIENT_SRC / "ws" / "types" / "events" / "events.const.ts")
    event_sources = extract_event_sources(events_const)
    total_event_types = sum(len(v) for v in event_sources.values())

    type_summaries = extract_client_types()
    examples = extract_examples()
    vue_info = extract_vue_composables()

    # 3. Write structured data
    write_json(OUTPUT_ROOT / "source-analysis" / "methods.json", methods)
    write_json(OUTPUT_ROOT / "source-analysis" / "events.json", event_sources)
    write_json(OUTPUT_ROOT / "source-analysis" / "types.json", type_summaries)
    write_json(OUTPUT_ROOT / "source-analysis" / "examples.json", examples)
    write_json(OUTPUT_ROOT / "source-analysis" / "vue.json", vue_info)

    # 4. Build overview
    counts = {
        "docsPages": docs_meta["count"],
        "methods": len(methods),
        "eventSources": len(event_sources),
        "eventTypes": total_event_types,
        "typeFiles": len(type_summaries),
        "examples": len(examples),
    }
    write_text(OUTPUT_ROOT / "overview.md", build_overview(counts))

    # 5. Build quick reference and usage patterns
    write_text(
        OUTPUT_ROOT / "QUICK-REFERENCE.md",
        build_quick_reference([m["name"] for m in methods], event_sources),
    )
    write_text(
        OUTPUT_ROOT / "usage-patterns" / "interactive-html-and-obs.md",
        build_usage_patterns(),
    )

    # 6. Write index manifest
    index = {
        "generatedAt": generated_at,
        "officialSite": "https://streamerbot.github.io/client/",
        "officialRepo": "https://github.com/Streamerbot/client",
        "npmPackage": "@streamerbot/client",
        "counts": counts,
        "files": {
            "overview": "overview.md",
            "quickReference": "QUICK-REFERENCE.md",
            "usagePatterns": "usage-patterns/interactive-html-and-obs.md",
            "docsMirror": "scraped-site/",
            "methods": "source-analysis/methods.json",
            "events": "source-analysis/events.json",
            "types": "source-analysis/types.json",
            "examples": "source-analysis/examples.json",
            "vue": "source-analysis/vue.json",
            "worklog": "WORKLOG.md",
        },
    }
    write_json(OUTPUT_ROOT / "index.json", index)

    # 7. Worklog
    worklog = f"""# Work Log

Generated: {generated_at}

## Steps performed

1. Mirrored the local documentation site source (`apps/docs/content`) into `scraped-site/`.
2. Parsed `packages/client/src/ws/StreamerbotClient.ts` for public methods, signatures, and JSDoc.
3. Parsed `packages/client/src/ws/types/events/events.const.ts` for the full event source/type catalog.
4. Indexed all TypeScript type definition files in `packages/client/src/ws/types/`.
5. Captured built-in examples from `examples/`.
6. Indexed Vue integration source from `packages/vue/src/`.
7. Generated `overview.md`, `QUICK-REFERENCE.md`, and `usage-patterns/interactive-html-and-obs.md`.
8. Wrote `index.json` as the top-level manifest.

## Notes for future updates

- Re-run `build_summary.py` after pulling the upstream client repo to refresh the notes.
- The site at streamerbot.github.io/client is built from `apps/docs/content`, so the local mirror is the most reliable source.
- Event payload shapes are defined in `packages/client/src/ws/types/events/*.types.ts` and can be added to the summary if needed.
"""
    write_text(OUTPUT_ROOT / "WORKLOG.md", worklog)

    print("Summary built at:", OUTPUT_ROOT)
    print(json.dumps(counts, indent=2))


if __name__ == "__main__":
    main()
