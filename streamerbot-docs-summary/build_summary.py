"""Build a local expert summary set from Streamer.bot docs and wiki sources.

This script ingests:
- ./streamerbot-docs   (official docs site source)
- ./streamerbot-wiki   (community wiki source)

and emits searchable JSON and readable Markdown into ./streamerbot-docs-summary.
"""

from __future__ import annotations

import json
import re
import subprocess
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import yaml
from yaml import YAMLError

WORKSPACE_ROOT = Path(__file__).resolve().parent.parent
OUTPUT_ROOT = WORKSPACE_ROOT / "streamerbot-docs-summary"

DOCS_REPO_ROOT = WORKSPACE_ROOT / "streamerbot-docs"
DOCS_SOURCE_ROOT = DOCS_REPO_ROOT / "streamerbot"
DOCS_API_ROOT = DOCS_SOURCE_ROOT / "3.api"

WIKI_REPO_ROOT = WORKSPACE_ROOT / "streamerbot-wiki"


# ---------------------------------------------------------------------------
# Low-level helpers
# ---------------------------------------------------------------------------


def read_text(path: Path) -> str:
    """Return UTF-8 text for a source file."""

    return path.read_text(encoding="utf-8")


def load_yaml_file(path: Path) -> dict[str, Any]:
    """Load a YAML file into a dictionary."""

    data = yaml.safe_load(read_text(path))
    return data if isinstance(data, dict) else {}


def split_front_matter(text: str) -> tuple[dict[str, Any], str]:
    """Split markdown front matter from the body text."""

    match = re.match(r"\A---\n(.*?)\n---\n?(.*)\Z", text, flags=re.DOTALL)
    if match is None:
        return {}, text
    front_matter_text = match.group(1)
    remainder = match.group(2)
    data = parse_front_matter_yaml(front_matter_text)
    return data if isinstance(data, dict) else {}, remainder


def parse_front_matter_yaml(text: str) -> dict[str, Any]:
    """Parse docs front matter and tolerate common authoring mistakes."""

    try:
        data = yaml.safe_load(text) or {}
        return data if isinstance(data, dict) else {}
    except YAMLError:
        repaired = re.sub(r"\n(-\s+name:\s)", r"\nparameters:\n\1", text, count=1)
        try:
            data = yaml.safe_load(repaired) or {}
            return data if isinstance(data, dict) else {"__rawFrontMatter": text}
        except YAMLError:
            return {"__rawFrontMatter": text}


def strip_numeric_prefix(value: str) -> str:
    """Remove docs ordering prefixes from a path segment."""

    return re.sub(r"^\d+\.", "", value)


def build_doc_route(path: Path) -> str:
    """Convert a docs repo file path into a website route."""

    relative = path.relative_to(DOCS_SOURCE_ROOT)
    parts = [strip_numeric_prefix(part) for part in relative.parts]
    stem = Path(parts[-1]).stem
    stem = strip_numeric_prefix(stem)
    parts = list(parts[:-1])
    if stem not in {"index", ".navigation", "0.index"}:
        parts.append(stem)
    route = "/" + "/".join(part for part in parts if part and not part.startswith("."))
    return route.replace("//", "/")


def build_wiki_route(path: Path) -> str:
    """Convert a wiki file path into a wiki page route."""

    relative = path.relative_to(WIKI_REPO_ROOT)
    stem = path.stem
    # GitHub wiki URLs use the file stem with spaces as dashes, but the repo
    # stores files with dashes already. Keep the stem as-is.
    return "/" + stem


def clean_markdown_text(text: str) -> str:
    """Remove heavy markdown syntax for lightweight summaries."""

    text = re.sub(r"```.*?```", "", text, flags=re.DOTALL)
    text = re.sub(r"::[^\n]*", "", text)
    text = re.sub(r"\[(.*?)\]\((.*?)\)", r"\1", text)
    text = re.sub(r"`([^`]+)`", r"\1", text)
    text = re.sub(r"^#{1,6}\s*", "", text, flags=re.MULTILINE)
    text = re.sub(r"\|.*\|", "", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def extract_headings(text: str) -> list[str]:
    """Extract markdown headings from body content."""

    return [
        match.strip()
        for match in re.findall(r"^#{1,6}\s+(.+)$", text, flags=re.MULTILINE)
    ]


def extract_code_blocks(text: str) -> list[dict[str, str]]:
    """Extract fenced code blocks from markdown."""

    blocks: list[dict[str, str]] = []
    pattern = re.compile(
        r"```(?P<lang>[^\s\[]+)?(?:\s*\[(?P<label>[^\]]+)\])?\n(?P<code>.*?)```",
        re.DOTALL,
    )
    for match in pattern.finditer(text):
        blocks.append(
            {
                "language": (match.group("lang") or "").strip(),
                "label": (match.group("label") or "").strip(),
                "code": match.group("code").strip(),
            }
        )
    return blocks


def extract_summary(text: str, limit: int = 3) -> str:
    """Build a compact summary from the first non-empty paragraphs."""

    cleaned = clean_markdown_text(text)
    paragraphs = [
        paragraph.strip() for paragraph in cleaned.split("\n\n") if paragraph.strip()
    ]
    return "\n\n".join(paragraphs[:limit])


def load_parameter_imports() -> dict[str, dict[str, Any]]:
    """Load reusable parameter definitions from the official API docs."""

    imports: dict[str, dict[str, Any]] = {}
    for path in DOCS_API_ROOT.rglob("*.yml"):
        if "/.parameters/" not in path.as_posix():
            continue
        relative = path.relative_to(DOCS_API_ROOT)
        key = str(relative).replace(".parameters/", "").replace(".yml", "")
        imports[key] = load_yaml_file(path)
    return imports


def load_variable_pages() -> dict[str, dict[str, Any]]:
    """Load variable definition pages used by triggers and sub-actions."""

    pages: dict[str, dict[str, Any]] = {}
    variables_root = DOCS_API_ROOT / ".variables"
    if variables_root.exists():
        for path in variables_root.rglob("*.md"):
            data, _ = split_front_matter(read_text(path))
            name = path.stem
            pages[name] = data
    return pages


def resolve_parameters(
    parameters: Any, parameter_imports: dict[str, dict[str, Any]]
) -> list[dict[str, Any]]:
    """Resolve inline and imported parameter definitions."""

    if not isinstance(parameters, list):
        return []
    resolved: list[dict[str, Any]] = []
    for parameter in parameters:
        if not isinstance(parameter, dict):
            continue
        merged = dict(parameter_imports.get(parameter.get("import", ""), {}))
        merged.update(parameter)
        resolved.append(merged)
    return resolved


def resolve_variables(
    values: Any, variable_pages: dict[str, dict[str, Any]]
) -> list[dict[str, Any]]:
    """Resolve direct and common variable references for docs pages."""

    if not isinstance(values, list):
        return []
    resolved: list[dict[str, Any]] = []
    for value in values:
        if isinstance(value, dict):
            resolved.append(value)
            continue
        if isinstance(value, str):
            page = variable_pages.get(value)
            if isinstance(page, dict) and isinstance(page.get("variables"), list):
                resolved.extend(page["variables"])
            else:
                resolved.append({"name": value})
    return resolved


# ---------------------------------------------------------------------------
# Source collection
# ---------------------------------------------------------------------------


def infer_docs_section(relative_path: str) -> str:
    """Infer the high-level docs section from a relative file path."""

    if relative_path.startswith("3.api/"):
        return "api"
    if relative_path.startswith("2.guide/"):
        return "guide"
    if relative_path.startswith("1.get-started/"):
        return "get-started"
    if relative_path.startswith("4.examples/"):
        return "examples"
    if relative_path.startswith("5.changelogs/"):
        return "changelogs"
    if relative_path.startswith("faq/"):
        return "faq"
    return "other"


def infer_wiki_section(relative_path: str, title: str) -> str:
    """Infer a wiki section from the file path or title."""

    lower = (relative_path + " " + title).lower()
    topic_map = [
        ("commands", ["commands", "command"]),
        ("triggers", ["triggers", "trigger", "events"]),
        ("actions", ["actions", "action", "sub-action"]),
        ("variables", ["variables", "variable"]),
        ("timers", ["timers", "timer", "timed"]),
        ("queues", ["queues", "queue"]),
        ("csharp", ["csharp", "c#", "inline"]),
        ("broadcasters", ["obs", "broadcasters", "streamdeck", "polypop"]),
        (
            "integrations",
            [
                "integrations",
                "integration",
                "streamelements",
                "kofi",
                "pulsoid",
                "voicemod",
                "midi",
            ],
        ),
        ("platforms", ["twitch", "youtube", "platforms"]),
        ("settings", ["settings", "backup", "update"]),
        ("quick-start", ["quick-start", "install", "linux"]),
        ("plugins", ["plugins", "plugin"]),
        ("servers-clients", ["websocket", "http", "udp", "servers", "clients"]),
    ]
    for section, keywords in topic_map:
        if any(keyword in lower for keyword in keywords):
            return section
    return "other"


def collect_docs_pages() -> list[dict[str, Any]]:
    """Collect and normalize official Streamer.bot markdown pages."""

    parameter_imports = load_parameter_imports()
    variable_pages = load_variable_pages()
    pages: list[dict[str, Any]] = []

    for path in sorted(DOCS_SOURCE_ROOT.rglob("*.md")):
        text = read_text(path)
        front_matter, body = split_front_matter(text)
        relative_path = str(path.relative_to(DOCS_SOURCE_ROOT))
        code_blocks = extract_code_blocks(body)
        route = build_doc_route(path)
        page = {
            "sourceRepo": "streamerbot-docs",
            "sourcePath": relative_path,
            "sourceUrl": f"https://docs.streamer.bot{route}",
            "route": route,
            "section": infer_docs_section(relative_path),
            "title": front_matter.get("title") or front_matter.get("name") or path.stem,
            "name": front_matter.get("name"),
            "description": front_matter.get("description", ""),
            "version": front_matter.get("version"),
            "frontMatter": front_matter,
            "parameters": resolve_parameters(
                front_matter.get("parameters"), parameter_imports
            ),
            "variables": resolve_variables(
                front_matter.get("variables"), variable_pages
            ),
            "commonVariables": resolve_variables(
                front_matter.get("commonVariables"), variable_pages
            ),
            "headings": extract_headings(body),
            "summary": extract_summary(body),
            "codeBlocks": code_blocks,
            "codeBlockCount": len(code_blocks),
            "body": body.strip(),
        }
        pages.append(page)

    return pages


def collect_wiki_pages() -> list[dict[str, Any]]:
    """Collect and normalize Streamer.bot wiki markdown pages."""

    pages: list[dict[str, Any]] = []
    if not WIKI_REPO_ROOT.exists():
        return pages

    for path in sorted(WIKI_REPO_ROOT.rglob("*.md")):
        # Skip loose test files and non-content assets.
        if path.name in {"test.md", "home.md", "README.md"}:
            continue
        text = read_text(path)
        front_matter, body = split_front_matter(text)
        relative_path = str(path.relative_to(WIKI_REPO_ROOT))
        route = build_wiki_route(path)
        title = front_matter.get("title") or path.stem.replace("-", " ").replace(
            "_", " "
        )
        code_blocks = extract_code_blocks(body)
        page = {
            "sourceRepo": "streamerbot-wiki",
            "sourcePath": relative_path,
            "sourceUrl": f"https://github.com/Streamerbot/streamerbot-wiki/wiki{route}",
            "route": route,
            "section": infer_wiki_section(relative_path, title),
            "title": title,
            "name": title,
            "description": front_matter.get("description", ""),
            "version": front_matter.get("version"),
            "frontMatter": front_matter,
            "parameters": [],
            "variables": [],
            "commonVariables": [],
            "headings": extract_headings(body),
            "summary": extract_summary(body),
            "codeBlocks": code_blocks,
            "codeBlockCount": len(code_blocks),
            "body": body.strip(),
        }
        pages.append(page)

    return pages


# ---------------------------------------------------------------------------
# Git metadata
# ---------------------------------------------------------------------------


def git_revision(repo_root: Path) -> str:
    """Read the current git HEAD revision for a repository."""

    try:
        result = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            check=True,
            capture_output=True,
            text=True,
            cwd=repo_root,
        )
    except (OSError, subprocess.CalledProcessError):
        return ""
    return result.stdout.strip()


def git_branches(repo_root: Path) -> list[dict[str, str]]:
    """List all remote-tracking branches with their current commit."""

    branches: list[dict[str, str]] = []
    if not repo_root.exists():
        return branches
    try:
        result = subprocess.run(
            [
                "git",
                "branch",
                "-r",
                "--format=%(refname:short)	%(objectname:short)	%(subject)",
            ],
            check=True,
            capture_output=True,
            text=True,
            cwd=repo_root,
        )
    except (OSError, subprocess.CalledProcessError):
        return branches

    for line in result.stdout.strip().splitlines():
        parts = line.split("\t", 2)
        if len(parts) < 2:
            continue
        ref, short_hash = parts[0], parts[1]
        subject = parts[2] if len(parts) > 2 else ""
        # Skip the duplicate HEAD symref.
        if ref.endswith("/HEAD"):
            continue
        branches.append(
            {
                "name": ref,
                "shortHash": short_hash,
                "subject": subject,
            }
        )
    return branches


# ---------------------------------------------------------------------------
# Filtering and slimming
# ---------------------------------------------------------------------------


def filter_pages(pages: list[dict[str, Any]], prefix: str) -> list[dict[str, Any]]:
    """Filter normalized pages by source path prefix."""

    return [page for page in pages if page["sourcePath"].startswith(prefix)]


def filter_by_section(
    pages: list[dict[str, Any]], section: str
) -> list[dict[str, Any]]:
    """Filter normalized pages by inferred section/topic."""

    return [page for page in pages if page.get("section") == section]


def slim_page(page: dict[str, Any]) -> dict[str, Any]:
    """Reduce a full page record to a compact searchable structure."""

    return {
        "title": page["title"],
        "name": page["name"],
        "description": page["description"],
        "version": page["version"],
        "route": page["route"],
        "sourceRepo": page["sourceRepo"],
        "sourceUrl": page["sourceUrl"],
        "sourcePath": page["sourcePath"],
        "section": page.get("section"),
        "headings": page["headings"],
        "summary": page["summary"],
        "parameters": page["parameters"],
        "variables": page["variables"],
        "commonVariables": page["commonVariables"],
        "codeBlocks": page["codeBlocks"],
        "codeBlockCount": page["codeBlockCount"],
    }


# ---------------------------------------------------------------------------
# Writers
# ---------------------------------------------------------------------------


def write_json(path: Path, data: Any) -> None:
    """Write JSON with deterministic formatting."""

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(data, indent=2, sort_keys=False, ensure_ascii=False, default=str)
        + "\n",
        encoding="utf-8",
    )


def write_markdown(path: Path, content: str) -> None:
    """Write UTF-8 markdown text to disk."""

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


# ---------------------------------------------------------------------------
# Markdown synthesizers
# ---------------------------------------------------------------------------


def build_overview_markdown(
    pages: list[dict[str, Any]], manifests: dict[str, list[dict[str, Any]]]
) -> str:
    """Create a human-readable project overview from normalized docs."""

    sections = Counter(page["section"] for page in pages)
    csharp_guides = manifests["csharp_guides"]
    guide_pages = manifests["guide_pages"]
    examples = manifests["example_pages"]
    methods = manifests["csharp_methods"]
    triggers = manifests["triggers"]
    sub_actions = manifests["sub_actions"]
    http_pages = manifests["http_pages"]
    websocket_pages = manifests["websocket_pages"]
    udp_pages = manifests["udp_pages"]
    wiki_pages = manifests["wiki_pages"]

    lines = [
        "# Streamer.bot Expert Notes",
        "",
        "## What Streamer.bot is",
        "",
        "Streamer.bot is an event-driven automation tool for livestreamers. The official docs position it around actions, triggers, variables, platform connections, stream app integrations, and an embedded C# execution surface for custom logic. The companion wiki adds practical walkthroughs, screenshots, and community recipes.",
        "",
        "## What the local docs snapshot covers",
        "",
        f"- Total markdown pages captured: **{len(pages)}**",
        f"- Official docs pages: **{len([p for p in pages if p['sourceRepo'] == 'streamerbot-docs'])}**",
        f"- Wiki pages: **{len(wiki_pages)}**",
        f"- API reference pages captured: **{sections['api']}**",
        f"- Guide pages captured: **{sections['guide']}**",
        f"- Get started pages captured: **{sections['get-started']}**",
        f"- Example pages captured: **{sections['examples']}**",
        f"- Changelog pages captured: **{sections['changelogs']}**",
        "",
        "## API coverage captured locally",
        "",
        f"- C# guide + recipe pages: **{len(csharp_guides)}**",
        f"- C# method reference pages: **{len(methods)}**",
        f"- Trigger reference pages: **{len(triggers)}**",
        f"- Sub-action reference pages: **{len(sub_actions)}**",
        f"- HTTP API pages: **{len(http_pages)}**",
        f"- WebSocket API pages: **{len(websocket_pages)}**",
        f"- UDP API pages: **{len(udp_pages)}**",
        "",
        "## C# code model",
        "",
        "- Inline code runs inside a `CPHInline` class.",
        "- `Execute()` is the required entrypoint and returns `bool` to continue or stop downstream sub-actions.",
        "- `Init()` and `Dispose()` are optional lifecycle hooks for setup and cleanup.",
        "- The `CPH` object exposes Streamer.bot methods for actions, users, Twitch, YouTube, OBS, variables, logging, and more.",
        "- The docs explicitly recommend `CPH.TryGetArg<T>()` over direct `args` access for safety.",
        "",
        "## Best-practice emphasis from the docs",
        "",
        "- Prefer official `CPH` methods and documented argument patterns over ad-hoc state access.",
        "- Use `TryGetArg<T>()` or safe dictionary checks for trigger/action arguments.",
        "- Treat `Execute()` return values carefully because `false` stops remaining sub-actions unless the sub-action is configured to save the result instead.",
        "- Use `Init()` for one-time setup and `Dispose()` for cleanup where long-lived objects are involved.",
        "- Use persisted globals intentionally and validate null/default behavior when reading them back.",
        "",
        "## Important local files",
        "",
        "- `index.json`: top-level manifest for fast lookup.",
        "- `all-pages.json`: compact searchable catalog of all captured Streamer.bot docs + wiki pages.",
        "- `api-calls/*.json`: structured API datasets by area.",
        "- `topic-*.json` / `topic-*.md`: focused indexes for commands, triggers, timers, and queues.",
        "- `csharp-patterns/*.md`: opinionated notes for writing reliable inline Streamer.bot code.",
        "- `no-code-packaging.md`: guide for building projects that non-programmer streamers can install out-of-the-box.",
        "",
        "## High-value guide areas",
        "",
    ]

    for page in guide_pages[:12]:
        lines.append(f"- `{page['title']}` — {page['description']}")

    lines.extend(["", "## Official examples captured", ""])
    for page in examples[:12]:
        lines.append(f"- `{page['title']}` — {page['sourceUrl']}")

    lines.extend(["", "## Wiki highlights", ""])
    for page in wiki_pages[:12]:
        lines.append(f"- `{page['title']}` — {page['sourceUrl']}")

    return "\n".join(lines)


def build_csharp_practices_markdown() -> str:
    """Create a concise best-practices note for Streamer.bot inline C#."""

    return "\n".join(
        [
            "# Streamer.bot C# Best Practices",
            "",
            "## Priorities",
            "",
            "1. Follow Streamer.bot's documented `CPH` API shape first.",
            "2. Keep inline actions small, deterministic, and easy to rerun.",
            "3. Use `TryGetArg<T>()` for argument access whenever possible.",
            "4. Use globals only for deliberate shared state.",
            "5. Use action chaining for orchestration and C# for logic-heavy steps.",
            "",
            "## Recommended coding style inside `CPHInline`",
            "",
            "- Validate every external input from args, globals, chat, or remote APIs.",
            "- Favor early returns when a required argument is missing.",
            "- Keep `Execute()` short and move logic into helper methods when the action grows.",
            "- Log state transitions and failure paths with `CPH.LogInfo`, `CPH.LogWarn`, or `CPH.LogError`.",
            "- Use `Init()` only for reusable setup that actually benefits from one-time compilation-time initialization.",
            "- Use `Dispose()` to release any long-lived disposable resources.",
            "",
            "## Live interactive control guidance",
            "",
            "- Treat triggers as ingestion, variables as state, and actions as orchestration units.",
            "- Keep chat-driven actions idempotent where practical so retries do not corrupt state.",
            "- Prefer named arguments into child actions for clean action-to-action contracts.",
            "- Separate viewer-facing effects from moderator/admin safety checks.",
            "- For overlays or remote controls, favor WebSocket or HTTP entrypoints that simply enqueue named actions with validated args.",
            "",
            "## Practical patterns",
            "",
            "- Counter/state machine: use persisted globals with default fallbacks.",
            "- Reusable utility action: encapsulate logic in one C# code action and call it through `RunAction` with args.",
            "- Remote control: expose a minimal HTTP or WebSocket command surface that maps to approved action names/IDs.",
            "- Platform branching: read `eventSource`/`__source` with the documented enum types instead of guessing from strings.",
            "",
            "## Safe inline action template",
            "",
            "```cs",
            "using System;",
            "",
            "public class CPHInline",
            "{",
            "    /// <summary>",
            "    /// Executes a Streamer.bot inline action by validating inputs, updating shared state, and delegating follow-up work.",
            "    /// </summary>",
            "    /// <returns>",
            "    /// True to continue downstream sub-actions; false to stop execution when required input is missing.",
            "    /// </returns>",
            "    public bool Execute()",
            "    {",
            '        if (!CPH.TryGetArg("user", out string user) || string.IsNullOrWhiteSpace(user))',
            "            return false;",
            "",
            '        int count = CPH.GetGlobalVar<int?>("interactionCount", true) ?? 0;',
            "        count++;",
            '        CPH.SetGlobalVar("interactionCount", count, true);',
            "",
            '        CPH.LogInfo($"Interaction from {user}; count={count}");',
            '        CPH.RunAction("Post Interaction Overlay", false);',
            "        return true;",
            "    }",
            "}",
            "```",
            "",
            "## Helper-method pattern",
            "",
            "```cs",
            "using System;",
            "",
            "public class CPHInline",
            "{",
            "    /// <summary>",
            "    /// Executes the action after reading a validated command argument from Streamer.bot's argument stack.",
            "    /// </summary>",
            "    /// <returns>",
            "    /// True when the action handled the request successfully; otherwise false.",
            "    /// </returns>",
            "    public bool Execute()",
            "    {",
            '        if (!CPH.TryGetArg("rawInput", out string rawInput))',
            "            return false;",
            "",
            "        return HandleInput(rawInput);",
            "    }",
            "",
            "    /// <summary>",
            "    /// Handles a validated user input string and emits a log entry for later troubleshooting.",
            "    /// </summary>",
            '    /// <param name="rawInput">Non-null command or chat input provided by the current action context.</param>',
            "    /// <returns>",
            "    /// True when the input is accepted; otherwise false to halt downstream execution.",
            "    /// </returns>",
            "    public bool HandleInput(string rawInput)",
            "    {",
            "        if (string.IsNullOrWhiteSpace(rawInput))",
            "            return false;",
            "",
            '        CPH.LogInfo($"Handling input: {rawInput}");',
            "        return true;",
            "    }",
            "}",
            "```",
        ]
    )


def build_interactive_controls_markdown() -> str:
    """Create design notes for interactive streamer controls using official APIs."""

    return "\n".join(
        [
            "# Interactive Controls for Twitch and YouTube",
            "",
            "## Recommended architecture",
            "",
            "- Trigger receives the platform event.",
            "- A small C# action validates args and normalizes the event into a stable internal shape.",
            "- The action writes or reads the minimum shared state needed from globals.",
            "- Follow-up actions handle side effects such as chat output, OBS changes, counters, reward updates, or overlays.",
            "",
            "## Why this maps well to Streamer.bot",
            "",
            "- It aligns with the docs' actions + triggers + variables model.",
            "- It keeps `CPHInline` code focused on logic instead of UI orchestration.",
            "- It makes testing easier because each action contract is smaller.",
            "",
            "## Guardrails",
            "",
            "- Validate platform-specific args before doing stateful work.",
            "- Keep remote-triggered actions on explicit allowlists of action IDs or names.",
            "- Prefer fixed variable names for shared state and document them in the action description.",
            "- Avoid direct `args` indexing in hot paths when `TryGetArg<T>()` is available.",
            "",
            "## Use the local datasets",
            "",
            "- Check `api-calls/triggers.json` for event variables.",
            "- Check `api-calls/sub-actions.json` for built-in action building blocks.",
            "- Check `api-calls/csharp-methods.json` for inline code methods.",
            "- Check `api-calls/http-api.json`, `websocket-api.json`, and `udp-api.json` for remote entrypoints.",
            "- Check `topic-commands.json`, `topic-triggers.json`, `topic-timers.json`, and `topic-queues.json` for focused references.",
        ]
    )


def build_quick_reference_markdown() -> str:
    """Create a durable lookup note for future Streamer.bot questions."""

    return "\n".join(
        [
            "# Streamer.bot Quick Reference",
            "",
            "Use this folder first before going back to the online docs.",
            "",
            "## Primary entrypoints",
            "",
            "- `./streamerbot-docs-summary/index.json` — master manifest of all generated files.",
            "- `./streamerbot-docs-summary/all-pages.json` — full local searchable page catalog.",
            "- `./streamerbot-docs-summary/overview.md` — tool overview and high-level guidance.",
            "",
            "## When the question is about...",
            "",
            "- **Inline C# methods** → `api-calls/csharp-methods.json`",
            "- **CPH classes and enums** → `api-calls/csharp-classes.json`, `api-calls/csharp-enums.json`",
            "- **Trigger variables** → `api-calls/triggers.json`",
            "- **Built-in sub-actions** → `api-calls/sub-actions.json`",
            "- **HTTP control** → `api-calls/http-api.json`",
            "- **WebSocket control/events** → `api-calls/websocket-api.json`",
            "- **UDP control** → `api-calls/udp-api.json`",
            "- **General docs/guides** → `api-calls/guide-pages.json`",
            "- **Official examples** → `api-calls/examples.json`",
            "- **Commands** → `topic-commands.json` / `topic-commands.md`",
            "- **Triggers/Events** → `topic-triggers.json` / `topic-triggers.md`",
            "- **Timers** → `topic-timers.json` / `topic-timers.md`",
            "- **Queues** → `topic-queues.json` / `topic-queues.md`",
            "- **Best-practice code style** → `csharp-patterns/best-practices.md`",
            "- **Interactive stream control design** → `csharp-patterns/interactive-controls.md`",
            "- **Packaging for non-programmers** → `no-code-packaging.md`",
            "",
            "## Important reminders",
            "",
            "- Prefer official Streamer.bot docs behavior over generic .NET assumptions.",
            "- For inline code, prefer `CPH.TryGetArg<T>()` over direct `args` access.",
            "- Use action IDs/names, trigger variables, and CPH methods exactly as documented in the local dataset.",
            "",
            "## Suggested retrieval order",
            "",
            "1. Check `index.json` for the right local file.",
            "2. Search the relevant JSON dataset for the method, trigger, or sub-action name.",
            "3. Use `sourceUrl` from that record only if more context is needed.",
        ]
    )


def build_no_code_packaging_markdown() -> str:
    """Create a guide for shipping Streamer.bot projects that non-coders can install."""

    return "\n".join(
        [
            "# Packaging Streamer.bot Projects for Non-Programmer Streamers",
            "",
            "The goal is for a streamer to copy an import string, paste it into Streamer.bot, and have a working project with sensible defaults and easy configuration.",
            "",
            "## One-paste install",
            "",
            "- Generate a Streamer.bot import string for every release. Put it in `import_code.txt` in the project folder.",
            "- Keep the import self-contained: actions, commands, timers, queues, variables, and OBS sources/browser sources it needs.",
            "- Test the import in a clean Streamer.bot profile before publishing.",
            "",
            "## Configuration without code",
            "",
            "- Surface all tunables through Streamer.bot globals (e.g., `ProjectName::Enabled`, `ProjectName::CooldownSeconds`).",
            "- Provide a single 'Settings' action that validates and sets those globals with clear labels.",
            "- Use chat commands only for runtime control; first-time setup should happen through the Settings action or a simple JSON config file read by a C# sub-action.",
            "- Document every global in the README with copy-paste names so streamers do not have to guess.",
            "",
            "## Friendly defaults",
            "",
            "- Default to safe, low-risk values (long cooldowns, moderator-only commands, disabled optional features).",
            "- Add a 'First Time Setup' command that prints the current configuration and usage instructions.",
            "- Prefer built-in sub-actions over custom C# when a built-in one exists; it makes the project easier to audit and modify.",
            "",
            "## Compact, powerful C# when needed",
            "",
            "- Keep C# blocks small and focused on one job: parse input, read/write globals, decide what to do, return true/false.",
            "- Let actions handle presentation: chat messages, OBS visibility, sound playback, overlays.",
            "- Expose helper methods inside the CPHInline class so complex logic can be unit-tested mentally by reading one screen.",
            "- Log configuration errors clearly so a streamer can report them without reading code.",
            "",
            "## Commands, triggers, timers, queues",
            "",
            "- Commands: use groups and clear names. Provide aliases only when they do not conflict with common bot commands.",
            "- Triggers: subscribe only to events the project actually uses; avoid catch-all triggers that fire constantly.",
            "- Timers: randomize interval slightly or use the built-in timer sub-action to avoid chat-pattern predictability.",
            "- Queues: name queues after the project so they are easy to find. Document whether a queue is meant to be paused/resumed manually.",
            "",
            "## Distribution checklist",
            "",
            "- [ ] README explains what the project does and who it is for.",
            "- [ ] `import_code.txt` is present and tested on the target Streamer.bot version.",
            "- [ ] `README.md` has a 'Quick Start' section: paste, configure globals, enable.",
            "- [ ] A list of required permissions (Twitch scopes, OBS connection, etc.) is included.",
            "- [ ] Troubleshooting section covers the most common failure modes.",
            "- [ ] Optional: a preview video or screenshots of the setup steps.",
            "",
            "## Reference these local files while building",
            "",
            "- `topic-commands.json` / `topic-commands.md`",
            "- `topic-triggers.json` / `topic-triggers.md`",
            "- `topic-timers.json` / `topic-timers.md`",
            "- `topic-queues.json` / `topic-queues.md`",
            "- `api-calls/csharp-methods.json`",
            "- `api-calls/sub-actions.json`",
        ]
    )


def build_topic_markdown(
    title: str, description: str, pages: list[dict[str, Any]]
) -> str:
    """Build a focused topic overview from a filtered page list."""

    lines = [
        f"# {title}",
        "",
        description,
        "",
        f"## Pages captured ({len(pages)})",
        "",
    ]
    for page in pages[:40]:
        lines.append(
            f"- [`{page['title']}`]({page['sourceUrl']}) — {page.get('description', '') or page.get('summary', '').splitlines()[0] if page.get('summary') else ''}"
        )
    if len(pages) > 40:
        lines.append(
            f"- … and {len(pages) - 40} more. See `topic-{title.lower().replace(' ', '-').replace('/', '-')}.json`."
        )
    lines.append("")
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Index builder
# ---------------------------------------------------------------------------


def build_index(
    pages: list[dict[str, Any]],
    manifests: dict[str, list[dict[str, Any]]],
    branches: dict[str, list[dict[str, str]]],
) -> dict[str, Any]:
    """Build the top-level summary manifest."""

    counts = {
        "allPages": len(pages),
        "docsPages": len([p for p in pages if p["sourceRepo"] == "streamerbot-docs"]),
        "wikiPages": len(manifests["wiki_pages"]),
        "api": sum(1 for p in pages if p.get("section") == "api"),
        "guide": sum(1 for p in pages if p.get("section") == "guide"),
        "getStarted": sum(1 for p in pages if p.get("section") == "get-started"),
        "examples": sum(1 for p in pages if p.get("section") == "examples"),
        "changelogs": sum(1 for p in pages if p.get("section") == "changelogs"),
        "csharpGuides": len(manifests["csharp_guides"]),
        "csharpMethods": len(manifests["csharp_methods"]),
        "csharpClasses": len(manifests["csharp_classes"]),
        "csharpEnums": len(manifests["csharp_enums"]),
        "subActions": len(manifests["sub_actions"]),
        "triggers": len(manifests["triggers"]),
        "httpPages": len(manifests["http_pages"]),
        "websocketPages": len(manifests["websocket_pages"]),
        "udpPages": len(manifests["udp_pages"]),
        "guidePages": len(manifests["guide_pages"]),
        "examplePages": len(manifests["example_pages"]),
        "commandPages": len(manifests["topic_commands"]),
        "triggerPages": len(manifests["topic_triggers"]),
        "timerPages": len(manifests["topic_timers"]),
        "queuePages": len(manifests["topic_queues"]),
    }

    return {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "sources": {
            "officialDocs": {
                "site": "https://docs.streamer.bot/",
                "repo": "https://github.com/Streamerbot/docs",
                "localRoot": "streamerbot-docs",
                "revision": git_revision(DOCS_REPO_ROOT),
                "branches": branches.get("streamerbot-docs", []),
            },
            "wiki": {
                "site": "https://github.com/Streamerbot/streamerbot-wiki/wiki",
                "repo": "https://github.com/Streamerbot/streamerbot-wiki",
                "localRoot": "streamerbot-wiki",
                "revision": git_revision(WIKI_REPO_ROOT),
                "branches": branches.get("streamerbot-wiki", []),
            },
        },
        "counts": counts,
        "files": {
            "overview": "overview.md",
            "allPages": "all-pages.json",
            "workBranches": "work-branches.json",
            "csharpMethods": "api-calls/csharp-methods.json",
            "csharpGuides": "api-calls/csharp-guides.json",
            "csharpClasses": "api-calls/csharp-classes.json",
            "csharpEnums": "api-calls/csharp-enums.json",
            "subActions": "api-calls/sub-actions.json",
            "triggers": "api-calls/triggers.json",
            "httpApi": "api-calls/http-api.json",
            "websocketApi": "api-calls/websocket-api.json",
            "udpApi": "api-calls/udp-api.json",
            "guidePages": "api-calls/guide-pages.json",
            "examples": "api-calls/examples.json",
            "wikiPages": "api-calls/wiki-pages.json",
            "topicCommands": "topic-commands.json",
            "topicTriggers": "topic-triggers.json",
            "topicTimers": "topic-timers.json",
            "topicQueues": "topic-queues.json",
            "csharpBestPractices": "csharp-patterns/best-practices.md",
            "interactiveControls": "csharp-patterns/interactive-controls.md",
            "quickReference": "QUICK-REFERENCE.md",
            "noCodePackaging": "no-code-packaging.md",
            "worklog": "WORKLOG.md",
        },
        "highlights": {
            "toolModel": [
                "Actions orchestrate work.",
                "Triggers supply event-driven execution.",
                "Variables carry event state and shared state.",
                "Inline C# extends built-in sub-actions through the CPH API.",
                "Commands, timers, and queues manage how viewers interact with the bot.",
            ],
            "csharpGuidance": [
                "Use CPH.TryGetArg<T>() for safe argument access.",
                "Use Execute() as the required action entrypoint.",
                "Use Init() and Dispose() only when lifecycle setup or cleanup is needed.",
                "Use documented HTTP, WebSocket, and UDP entrypoints for remote control.",
            ],
        },
    }


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main() -> None:
    """Generate all local summary artifacts from the docs and wiki clones."""

    if not DOCS_SOURCE_ROOT.exists():
        raise SystemExit(f"Official docs root not found: {DOCS_SOURCE_ROOT}")

    docs_pages = collect_docs_pages()
    wiki_pages = collect_wiki_pages()
    all_pages = docs_pages + wiki_pages

    # Topic-focused filters (cross docs + wiki).
    topic_commands = [
        slim_page(p)
        for p in all_pages
        if infer_wiki_section(p["sourcePath"], p["title"]) == "commands"
    ]
    topic_triggers = [
        slim_page(p)
        for p in all_pages
        if infer_wiki_section(p["sourcePath"], p["title"]) == "triggers"
    ]
    topic_timers = [
        slim_page(p)
        for p in all_pages
        if infer_wiki_section(p["sourcePath"], p["title"]) == "timers"
    ]
    topic_queues = [
        slim_page(p)
        for p in all_pages
        if infer_wiki_section(p["sourcePath"], p["title"]) == "queues"
    ]

    manifests = {
        "all_pages": [slim_page(page) for page in all_pages],
        "wiki_pages": [slim_page(page) for page in wiki_pages],
        "csharp_guides": [
            slim_page(page)
            for page in filter_pages(docs_pages, "3.api/3.csharp/0.guide/")
            + filter_pages(docs_pages, "3.api/3.csharp/1.recipes/")
        ],
        "csharp_methods": [
            slim_page(page)
            for page in filter_pages(docs_pages, "3.api/3.csharp/3.methods/")
        ],
        "csharp_classes": [
            slim_page(page)
            for page in filter_pages(docs_pages, "3.api/3.csharp/4.classes/")
        ],
        "csharp_enums": [
            slim_page(page)
            for page in filter_pages(docs_pages, "3.api/3.csharp/5.enums/")
        ],
        "sub_actions": [
            slim_page(page) for page in filter_pages(docs_pages, "3.api/1.sub-actions/")
        ],
        "triggers": [
            slim_page(page) for page in filter_pages(docs_pages, "3.api/2.triggers/")
        ],
        "http_pages": [
            slim_page(page) for page in filter_pages(docs_pages, "3.api/5.http/")
        ],
        "websocket_pages": [
            slim_page(page) for page in filter_pages(docs_pages, "3.api/4.websocket/")
        ],
        "udp_pages": [
            slim_page(page) for page in filter_pages(docs_pages, "3.api/6.udp/")
        ],
        "guide_pages": [
            slim_page(page)
            for page in filter_pages(docs_pages, "2.guide/")
            + filter_pages(docs_pages, "1.get-started/")
        ],
        "example_pages": [
            slim_page(page) for page in filter_pages(docs_pages, "4.examples/")
        ],
        "topic_commands": topic_commands,
        "topic_triggers": topic_triggers,
        "topic_timers": topic_timers,
        "topic_queues": topic_queues,
    }

    branches = {
        "streamerbot-docs": git_branches(DOCS_REPO_ROOT),
        "streamerbot-wiki": git_branches(WIKI_REPO_ROOT),
    }

    # Core datasets.
    write_json(OUTPUT_ROOT / "all-pages.json", manifests["all_pages"])
    write_json(OUTPUT_ROOT / "work-branches.json", branches)
    write_json(
        OUTPUT_ROOT / "api-calls" / "csharp-guides.json", manifests["csharp_guides"]
    )
    write_json(
        OUTPUT_ROOT / "api-calls" / "csharp-methods.json", manifests["csharp_methods"]
    )
    write_json(
        OUTPUT_ROOT / "api-calls" / "csharp-classes.json", manifests["csharp_classes"]
    )
    write_json(
        OUTPUT_ROOT / "api-calls" / "csharp-enums.json", manifests["csharp_enums"]
    )
    write_json(OUTPUT_ROOT / "api-calls" / "sub-actions.json", manifests["sub_actions"])
    write_json(OUTPUT_ROOT / "api-calls" / "triggers.json", manifests["triggers"])
    write_json(OUTPUT_ROOT / "api-calls" / "http-api.json", manifests["http_pages"])
    write_json(
        OUTPUT_ROOT / "api-calls" / "websocket-api.json", manifests["websocket_pages"]
    )
    write_json(OUTPUT_ROOT / "api-calls" / "udp-api.json", manifests["udp_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "guide-pages.json", manifests["guide_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "examples.json", manifests["example_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "wiki-pages.json", manifests["wiki_pages"])

    # Topic-focused datasets.
    write_json(OUTPUT_ROOT / "topic-commands.json", manifests["topic_commands"])
    write_json(OUTPUT_ROOT / "topic-triggers.json", manifests["topic_triggers"])
    write_json(OUTPUT_ROOT / "topic-timers.json", manifests["topic_timers"])
    write_json(OUTPUT_ROOT / "topic-queues.json", manifests["topic_queues"])

    # Readable summaries.
    write_markdown(
        OUTPUT_ROOT / "overview.md", build_overview_markdown(all_pages, manifests)
    )
    write_markdown(OUTPUT_ROOT / "QUICK-REFERENCE.md", build_quick_reference_markdown())
    write_markdown(
        OUTPUT_ROOT / "no-code-packaging.md", build_no_code_packaging_markdown()
    )
    write_markdown(
        OUTPUT_ROOT / "topic-commands.md",
        build_topic_markdown(
            "Commands",
            "Chat commands: how they are defined, matched, grouped, cooled down, and wired to actions.",
            manifests["topic_commands"],
        ),
    )
    write_markdown(
        OUTPUT_ROOT / "topic-triggers.md",
        build_topic_markdown(
            "Triggers & Events",
            "Triggers fire actions in response to Twitch, YouTube, OBS, timer, and custom events.",
            manifests["topic_triggers"],
        ),
    )
    write_markdown(
        OUTPUT_ROOT / "topic-timers.md",
        build_topic_markdown(
            "Timers",
            "Timed actions that run on an interval or randomized schedule.",
            manifests["topic_timers"],
        ),
    )
    write_markdown(
        OUTPUT_ROOT / "topic-queues.md",
        build_topic_markdown(
            "Action Queues",
            "Action queues let you serialize, pause, resume, and manage concurrent action execution.",
            manifests["topic_queues"],
        ),
    )
    write_markdown(
        OUTPUT_ROOT / "csharp-patterns" / "best-practices.md",
        build_csharp_practices_markdown(),
    )
    write_markdown(
        OUTPUT_ROOT / "csharp-patterns" / "interactive-controls.md",
        build_interactive_controls_markdown(),
    )

    # Top-level manifest.
    write_json(OUTPUT_ROOT / "index.json", build_index(all_pages, manifests, branches))


if __name__ == "__main__":
    main()
