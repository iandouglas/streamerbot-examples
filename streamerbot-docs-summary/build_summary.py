"""Build a local summary set from the official Streamer.bot docs repo."""

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
SOURCE_ROOT = Path("/tmp/opencode/streamerbot-docs-official/streamerbot")
API_ROOT = SOURCE_ROOT / "3.api"


def read_text(path: Path) -> str:
    """Return UTF-8 text for a source file.

    Args:
        path: Filesystem path to read.

    Returns:
        The full file contents as a string.
    """

    return path.read_text(encoding="utf-8")


def load_yaml_file(path: Path) -> dict[str, Any]:
    """Load a YAML file into a dictionary.

    Args:
        path: YAML file path.

    Returns:
        Parsed YAML content or an empty dictionary.
    """

    data = yaml.safe_load(read_text(path))
    return data if isinstance(data, dict) else {}


def split_front_matter(text: str) -> tuple[dict[str, Any], str]:
    """Split markdown front matter from the body text.

    Args:
        text: Raw markdown file contents.

    Returns:
        A tuple of parsed front matter and markdown body text.
    """

    match = re.match(r"\A---\n(.*?)\n---\n?(.*)\Z", text, flags=re.DOTALL)
    if match is None:
        return {}, text
    front_matter_text = match.group(1)
    remainder = match.group(2)
    data = parse_front_matter_yaml(front_matter_text)
    return data if isinstance(data, dict) else {}, remainder


def parse_front_matter_yaml(text: str) -> dict[str, Any]:
    """Parse docs front matter and tolerate common authoring mistakes.

    Args:
        text: Raw front matter text between --- markers.

    Returns:
        Parsed front matter as a dictionary.
    """

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
    """Remove docs ordering prefixes from a path segment.

    Args:
        value: File or directory segment that may begin with a numeric prefix.

    Returns:
        The normalized segment without ordering prefixes.
    """

    return re.sub(r"^\d+\.", "", value)


def build_doc_route(path: Path) -> str:
    """Convert a docs repo file path into a website route.

    Args:
        path: Markdown or YAML file path beneath the official docs root.

    Returns:
        A website route such as /api/csharp/methods.
    """

    relative = path.relative_to(SOURCE_ROOT)
    parts = [strip_numeric_prefix(part) for part in relative.parts]
    stem = Path(parts[-1]).stem
    stem = strip_numeric_prefix(stem)
    parts = list(parts[:-1])
    if stem not in {"index", ".navigation", "0.index"}:
        parts.append(stem)
    route = "/" + "/".join(part for part in parts if part and not part.startswith("."))
    return route.replace("//", "/")


def clean_markdown_text(text: str) -> str:
    """Remove heavy markdown syntax for lightweight summaries.

    Args:
        text: Markdown body content.

    Returns:
        Simplified text suitable for search-friendly summaries.
    """

    text = re.sub(r"```.*?```", "", text, flags=re.DOTALL)
    text = re.sub(r"::[^\n]*", "", text)
    text = re.sub(r"\[(.*?)\]\((.*?)\)", r"\1", text)
    text = re.sub(r"`([^`]+)`", r"\1", text)
    text = re.sub(r"^#{1,6}\s*", "", text, flags=re.MULTILINE)
    text = re.sub(r"\|.*\|", "", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def extract_headings(text: str) -> list[str]:
    """Extract markdown headings from body content.

    Args:
        text: Markdown body content.

    Returns:
        Ordered heading labels without hash prefixes.
    """

    return [match.strip() for match in re.findall(r"^#{1,6}\s+(.+)$", text, flags=re.MULTILINE)]


def extract_code_blocks(text: str) -> list[dict[str, str]]:
    """Extract fenced code blocks from markdown.

    Args:
        text: Markdown body content.

    Returns:
        A list of code block dictionaries with language, label, and code text.
    """

    blocks: list[dict[str, str]] = []
    pattern = re.compile(r"```(?P<lang>[^\s\[]+)?(?:\s*\[(?P<label>[^\]]+)\])?\n(?P<code>.*?)```", re.DOTALL)
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
    """Build a compact summary from the first non-empty paragraphs.

    Args:
        text: Markdown body content.
        limit: Maximum number of paragraphs to include.

    Returns:
        A condensed summary string.
    """

    cleaned = clean_markdown_text(text)
    paragraphs = [paragraph.strip() for paragraph in cleaned.split("\n\n") if paragraph.strip()]
    return "\n\n".join(paragraphs[:limit])


def load_parameter_imports() -> dict[str, dict[str, Any]]:
    """Load reusable parameter definitions from the official API docs.

    Returns:
        Mapping of import keys to parameter metadata.
    """

    imports: dict[str, dict[str, Any]] = {}
    for path in API_ROOT.rglob("*.yml"):
        if "/.parameters/" not in path.as_posix():
            continue
        relative = path.relative_to(API_ROOT)
        key = str(relative).replace(".parameters/", "").replace(".yml", "")
        imports[key] = load_yaml_file(path)
    return imports


def load_variable_pages() -> dict[str, dict[str, Any]]:
    """Load variable definition pages used by triggers and sub-actions.

    Returns:
        Mapping of variable page names to parsed metadata.
    """

    pages: dict[str, dict[str, Any]] = {}
    variables_root = API_ROOT / ".variables"
    for path in variables_root.rglob("*.md"):
        data, _ = split_front_matter(read_text(path))
        name = path.stem
        pages[name] = data
    return pages


def resolve_parameters(parameters: Any, parameter_imports: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    """Resolve inline and imported parameter definitions.

    Args:
        parameters: Raw front matter parameter list.
        parameter_imports: Lookup table for reusable parameter definitions.

    Returns:
        A normalized list of parameter dictionaries.
    """

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


def resolve_variables(values: Any, variable_pages: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    """Resolve direct and common variable references for docs pages.

    Args:
        values: Raw variable or commonVariables front matter value.
        variable_pages: Lookup table for variable reference pages.

    Returns:
        A normalized list of variable dictionaries.
    """

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


def infer_section(relative_path: str) -> str:
    """Infer the high-level docs section from a relative file path.

    Args:
        relative_path: Repo-relative documentation path string.

    Returns:
        High-level section name for grouping.
    """

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
    return "other"


def collect_pages() -> list[dict[str, Any]]:
    """Collect and normalize official Streamer.bot markdown pages.

    Returns:
        A list of normalized page dictionaries for downstream summaries.
    """

    parameter_imports = load_parameter_imports()
    variable_pages = load_variable_pages()
    pages: list[dict[str, Any]] = []

    for path in sorted(SOURCE_ROOT.rglob("*.md")):
        text = read_text(path)
        front_matter, body = split_front_matter(text)
        relative_path = str(path.relative_to(SOURCE_ROOT))
        code_blocks = extract_code_blocks(body)
        page = {
            "sourcePath": relative_path,
            "sourceUrl": f"https://docs.streamer.bot{build_doc_route(path)}",
            "route": build_doc_route(path),
            "section": infer_section(relative_path),
            "title": front_matter.get("title") or front_matter.get("name") or path.stem,
            "name": front_matter.get("name"),
            "description": front_matter.get("description", ""),
            "version": front_matter.get("version"),
            "frontMatter": front_matter,
            "parameters": resolve_parameters(front_matter.get("parameters"), parameter_imports),
            "variables": resolve_variables(front_matter.get("variables"), variable_pages),
            "commonVariables": resolve_variables(front_matter.get("commonVariables"), variable_pages),
            "headings": extract_headings(body),
            "summary": extract_summary(body),
            "codeBlocks": code_blocks,
            "codeBlockCount": len(code_blocks),
            "body": body.strip(),
        }
        pages.append(page)

    return pages


def docs_git_revision() -> str:
    """Read the git revision of the cloned official docs repo.

    Returns:
        The current commit hash or an empty string if unavailable.
    """

    try:
        result = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            check=True,
            capture_output=True,
            text=True,
            cwd=SOURCE_ROOT.parent,
        )
    except (OSError, subprocess.CalledProcessError):
        return ""
    return result.stdout.strip()


def filter_pages(pages: list[dict[str, Any]], prefix: str) -> list[dict[str, Any]]:
    """Filter normalized pages by source path prefix.

    Args:
        pages: All normalized page dictionaries.
        prefix: Prefix to match beneath the official docs root.

    Returns:
        Matching page dictionaries.
    """

    return [page for page in pages if page["sourcePath"].startswith(prefix)]


def slim_page(page: dict[str, Any]) -> dict[str, Any]:
    """Reduce a full page record to a compact searchable structure.

    Args:
        page: Full normalized page dictionary.

    Returns:
        Compact dictionary for JSON export.
    """

    return {
        "title": page["title"],
        "name": page["name"],
        "description": page["description"],
        "version": page["version"],
        "route": page["route"],
        "sourceUrl": page["sourceUrl"],
        "sourcePath": page["sourcePath"],
        "headings": page["headings"],
        "summary": page["summary"],
        "parameters": page["parameters"],
        "variables": page["variables"],
        "commonVariables": page["commonVariables"],
        "codeBlocks": page["codeBlocks"],
        "codeBlockCount": page["codeBlockCount"],
    }


def write_json(path: Path, data: Any) -> None:
    """Write JSON with deterministic formatting.

    Args:
        path: Output file path.
        data: JSON-serializable value.

    Returns:
        None.
    """

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(data, indent=2, sort_keys=False, ensure_ascii=False, default=str) + "\n",
        encoding="utf-8",
    )


def write_markdown(path: Path, content: str) -> None:
    """Write UTF-8 markdown text to disk.

    Args:
        path: Output markdown path.
        content: Markdown content.

    Returns:
        None.
    """

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.rstrip() + "\n", encoding="utf-8")


def build_overview_markdown(pages: list[dict[str, Any]], manifests: dict[str, list[dict[str, Any]]]) -> str:
    """Create a human-readable project overview from normalized docs.

    Args:
        pages: All normalized page dictionaries.
        manifests: Grouped API and guide collections.

    Returns:
        Markdown overview text.
    """

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

    lines = [
        "# Streamer.bot Notes",
        "",
        "## What Streamer.bot is",
        "",
        "Streamer.bot is an event-driven automation tool for livestreamers. The official docs position it around actions, triggers, variables, platform connections, stream app integrations, and an embedded C# execution surface for custom logic.",
        "",
        "## What the official docs cover",
        "",
        f"- Total Streamer.bot markdown pages captured: **{len(pages)}**",
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
        "- `all-pages.json`: compact searchable catalog of all captured Streamer.bot docs pages.",
        "- `api-calls/*.json`: structured API datasets by area.",
        "- `csharp-patterns/*.md`: opinionated notes for writing reliable inline Streamer.bot code.",
        "",
        "## High-value guide areas",
        "",
    ]

    for page in guide_pages[:12]:
        lines.append(f"- `{page['title']}` — {page['description']}")

    lines.extend(["", "## Official examples captured", ""])
    for page in examples[:12]:
        lines.append(f"- `{page['title']}` — {page['sourceUrl']}")

    return "\n".join(lines)


def build_csharp_practices_markdown() -> str:
    """Create a concise best-practices note for Streamer.bot inline C#.

    Returns:
        Markdown guidance focused on safe, performant inline code.
    """

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
            "        if (!CPH.TryGetArg(\"user\", out string user) || string.IsNullOrWhiteSpace(user))",
            "            return false;",
            "",
            "        int count = CPH.GetGlobalVar<int?>(\"interactionCount\", true) ?? 0;",
            "        count++;",
            "        CPH.SetGlobalVar(\"interactionCount\", count, true);",
            "",
            "        CPH.LogInfo($\"Interaction from {user}; count={count}\");",
            "        CPH.RunAction(\"Post Interaction Overlay\", false);",
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
            "        if (!CPH.TryGetArg(\"rawInput\", out string rawInput))",
            "            return false;",
            "",
            "        return HandleInput(rawInput);",
            "    }",
            "",
            "    /// <summary>",
            "    /// Handles a validated user input string and emits a log entry for later troubleshooting.",
            "    /// </summary>",
            "    /// <param name=\"rawInput\">Non-null command or chat input provided by the current action context.</param>",
            "    /// <returns>",
            "    /// True when the input is accepted; otherwise false to halt downstream execution.",
            "    /// </returns>",
            "    public bool HandleInput(string rawInput)",
            "    {",
            "        if (string.IsNullOrWhiteSpace(rawInput))",
            "            return false;",
            "",
            "        CPH.LogInfo($\"Handling input: {rawInput}\");",
            "        return true;",
            "    }",
            "}",
            "```",
        ]
    )


def build_interactive_controls_markdown() -> str:
    """Create design notes for interactive streamer controls using official APIs.

    Returns:
        Markdown guidance for Twitch and YouTube interaction design.
    """

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
        ]
    )


def build_index(pages: list[dict[str, Any]], manifests: dict[str, list[dict[str, Any]]]) -> dict[str, Any]:
    """Build the top-level summary manifest.

    Args:
        pages: All normalized page dictionaries.
        manifests: Grouped collections already prepared for export.

    Returns:
        Manifest dictionary for index.json.
    """

    counts = {
        "allPages": len(pages),
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
    }

    return {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "officialDocs": {
            "site": "https://docs.streamer.bot/",
            "repo": "https://github.com/Streamerbot/docs",
            "revision": docs_git_revision(),
        },
        "counts": counts,
        "files": {
            "overview": "overview.md",
            "allPages": "all-pages.json",
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
            "csharpBestPractices": "csharp-patterns/best-practices.md",
            "interactiveControls": "csharp-patterns/interactive-controls.md",
            "worklog": "WORKLOG.md",
        },
        "highlights": {
            "toolModel": [
                "Actions orchestrate work.",
                "Triggers supply event-driven execution.",
                "Variables carry event state and shared state.",
                "Inline C# extends built-in sub-actions through the CPH API.",
            ],
            "csharpGuidance": [
                "Use CPH.TryGetArg<T>() for safe argument access.",
                "Use Execute() as the required action entrypoint.",
                "Use Init() and Dispose() only when lifecycle setup or cleanup is needed.",
                "Use documented HTTP, WebSocket, and UDP entrypoints for remote control.",
            ],
        },
    }


def main() -> None:
    """Generate all local summary artifacts from the official docs clone.

    Returns:
        None.
    """

    if not SOURCE_ROOT.exists():
        raise SystemExit(f"Official docs root not found: {SOURCE_ROOT}")

    pages = collect_pages()
    manifests = {
        "all_pages": [slim_page(page) for page in pages],
        "csharp_guides": [slim_page(page) for page in filter_pages(pages, "3.api/3.csharp/0.guide/") + filter_pages(pages, "3.api/3.csharp/1.recipes/")],
        "csharp_methods": [slim_page(page) for page in filter_pages(pages, "3.api/3.csharp/3.methods/")],
        "csharp_classes": [slim_page(page) for page in filter_pages(pages, "3.api/3.csharp/4.classes/")],
        "csharp_enums": [slim_page(page) for page in filter_pages(pages, "3.api/3.csharp/5.enums/")],
        "sub_actions": [slim_page(page) for page in filter_pages(pages, "3.api/1.sub-actions/")],
        "triggers": [slim_page(page) for page in filter_pages(pages, "3.api/2.triggers/")],
        "http_pages": [slim_page(page) for page in filter_pages(pages, "3.api/5.http/")],
        "websocket_pages": [slim_page(page) for page in filter_pages(pages, "3.api/4.websocket/")],
        "udp_pages": [slim_page(page) for page in filter_pages(pages, "3.api/6.udp/")],
        "guide_pages": [slim_page(page) for page in filter_pages(pages, "2.guide/") + filter_pages(pages, "1.get-started/")],
        "example_pages": [slim_page(page) for page in filter_pages(pages, "4.examples/")],
    }

    write_json(OUTPUT_ROOT / "all-pages.json", manifests["all_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "csharp-guides.json", manifests["csharp_guides"])
    write_json(OUTPUT_ROOT / "api-calls" / "csharp-methods.json", manifests["csharp_methods"])
    write_json(OUTPUT_ROOT / "api-calls" / "csharp-classes.json", manifests["csharp_classes"])
    write_json(OUTPUT_ROOT / "api-calls" / "csharp-enums.json", manifests["csharp_enums"])
    write_json(OUTPUT_ROOT / "api-calls" / "sub-actions.json", manifests["sub_actions"])
    write_json(OUTPUT_ROOT / "api-calls" / "triggers.json", manifests["triggers"])
    write_json(OUTPUT_ROOT / "api-calls" / "http-api.json", manifests["http_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "websocket-api.json", manifests["websocket_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "udp-api.json", manifests["udp_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "guide-pages.json", manifests["guide_pages"])
    write_json(OUTPUT_ROOT / "api-calls" / "examples.json", manifests["example_pages"])
    write_markdown(OUTPUT_ROOT / "overview.md", build_overview_markdown(pages, manifests))
    write_markdown(OUTPUT_ROOT / "csharp-patterns" / "best-practices.md", build_csharp_practices_markdown())
    write_markdown(OUTPUT_ROOT / "csharp-patterns" / "interactive-controls.md", build_interactive_controls_markdown())
    write_json(OUTPUT_ROOT / "index.json", build_index(pages, manifests))


if __name__ == "__main__":
    main()
