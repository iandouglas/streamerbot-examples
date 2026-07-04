"""Validate locally generated Streamer.bot documentation summary artifacts."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parent


def read_json(path: Path) -> Any:
    """Load JSON data from disk."""

    return json.loads(path.read_text(encoding="utf-8"))


def assert_file_exists(path: Path) -> None:
    """Assert that a required generated file exists."""

    if not path.exists():
        raise AssertionError(f"Missing required file: {path}")


def assert_count(index_data: dict[str, Any], file_key: str, count_key: str) -> None:
    """Assert that a dataset file length matches the manifest count."""

    data = read_json(ROOT / index_data["files"][file_key])
    expected = index_data["counts"][count_key]
    actual = len(data)
    if actual != expected:
        raise AssertionError(
            f"Count mismatch for {file_key}: expected {expected}, got {actual}"
        )


def assert_contains_title(path: Path, expected_title: str) -> None:
    """Assert that a JSON dataset contains an entry with a given title."""

    data = read_json(path)
    titles = {entry.get("title") for entry in data if isinstance(entry, dict)}
    if expected_title not in titles:
        raise AssertionError(f"Expected title {expected_title!r} not found in {path}")


def assert_routes_and_urls(path: Path) -> None:
    """Assert that exported JSON entries include routes and valid source URLs."""

    data = read_json(path)
    for entry in data:
        route = entry.get("route")
        source_url = entry.get("sourceUrl")
        source_repo = entry.get("sourceRepo")
        if not isinstance(route, str) or not route.startswith("/"):
            raise AssertionError(f"Invalid route in {path}: {route!r}")
        if not isinstance(source_url, str):
            raise AssertionError(f"Invalid source URL in {path}: {source_url!r}")
        if source_repo == "streamerbot-docs" and not source_url.startswith(
            "https://docs.streamer.bot/"
        ):
            raise AssertionError(f"Invalid docs source URL in {path}: {source_url!r}")
        if source_repo == "streamerbot-wiki" and not source_url.startswith(
            "https://github.com/Streamerbot/streamerbot-wiki/wiki/"
        ):
            raise AssertionError(f"Invalid wiki source URL in {path}: {source_url!r}")


def assert_markdown_contains(path: Path, expected_text: str) -> None:
    """Assert that a markdown artifact contains required reference text."""

    content = path.read_text(encoding="utf-8")
    if expected_text not in content:
        raise AssertionError(f"Expected text {expected_text!r} not found in {path}")


def assert_local_root_paths(index_data: dict[str, Any]) -> None:
    """Assert that manifest localRoot paths point inside agentic/."""

    docs_root = index_data["sources"]["officialDocs"].get("localRoot", "")
    wiki_root = index_data["sources"]["wiki"].get("localRoot", "")
    if not docs_root.startswith("agentic/"):
        raise AssertionError(
            f"Expected docs localRoot under agentic/, got {docs_root!r}"
        )
    if not wiki_root.startswith("agentic/"):
        raise AssertionError(
            f"Expected wiki localRoot under agentic/, got {wiki_root!r}"
        )


def main() -> None:
    """Run validation checks across all generated summary artifacts."""

    index_path = ROOT / "index.json"
    assert_file_exists(index_path)

    index_data = read_json(index_path)

    for relative_path in index_data["files"].values():
        assert_file_exists(ROOT / relative_path)

    # Core counts from the manifest.
    assert_count(index_data, "allPages", "allPages")
    assert_count(index_data, "csharpMethods", "csharpMethods")
    assert_count(index_data, "csharpGuides", "csharpGuides")
    assert_count(index_data, "csharpClasses", "csharpClasses")
    assert_count(index_data, "csharpEnums", "csharpEnums")
    assert_count(index_data, "subActions", "subActions")
    assert_count(index_data, "triggers", "triggers")
    assert_count(index_data, "httpApi", "httpPages")
    assert_count(index_data, "websocketApi", "websocketPages")
    assert_count(index_data, "udpApi", "udpPages")
    assert_count(index_data, "guidePages", "guidePages")
    assert_count(index_data, "examples", "examplePages")
    assert_count(index_data, "wikiPages", "wikiPages")
    assert_count(index_data, "topicCommands", "commandPages")
    assert_count(index_data, "topicTriggers", "triggerPages")
    assert_count(index_data, "topicTimers", "timerPages")
    assert_count(index_data, "topicQueues", "queuePages")

    # Spot-check key API entries.
    assert_contains_title(ROOT / index_data["files"]["csharpMethods"], "RunAction")
    assert_contains_title(ROOT / index_data["files"]["csharpMethods"], "TryGetArg")
    assert_contains_title(ROOT / index_data["files"]["httpApi"], "DoAction")
    assert_contains_title(
        ROOT / index_data["files"]["websocketApi"], "WebSocket Requests"
    )
    assert_contains_title(ROOT / index_data["files"]["udpApi"], "DoAction (UDP)")
    assert_contains_title(ROOT / index_data["files"]["triggers"], "Follow")
    assert_contains_title(ROOT / index_data["files"]["subActions"], "Execute C# Code")

    # Spot-check topic pages.
    assert_contains_title(ROOT / index_data["files"]["topicCommands"], "Commands")
    assert_contains_title(ROOT / index_data["files"]["topicTriggers"], "Triggers")

    # URLs/routes are well-formed for docs and wiki datasets.
    assert_routes_and_urls(ROOT / index_data["files"]["csharpMethods"])
    assert_routes_and_urls(ROOT / index_data["files"]["httpApi"])
    assert_routes_and_urls(ROOT / index_data["files"]["websocketApi"])
    assert_routes_and_urls(ROOT / index_data["files"]["udpApi"])
    assert_routes_and_urls(ROOT / index_data["files"]["wikiPages"])

    # Local roots point under agentic/.
    assert_local_root_paths(index_data)

    # Key Markdown artifacts reference the right datasets.
    assert_markdown_contains(
        ROOT / index_data["files"]["quickReference"], "api-calls/csharp-methods.json"
    )
    assert_markdown_contains(
        ROOT / index_data["files"]["quickReference"], "api-calls/triggers.json"
    )
    assert_markdown_contains(
        ROOT / index_data["files"]["noCodePackaging"], "import_code.txt"
    )
    assert_markdown_contains(
        ROOT / index_data["files"]["overview"], "no-code-packaging.md"
    )

    # Sanity-check minimum coverage.
    if index_data["counts"]["csharpMethods"] < 100:
        raise AssertionError("Expected at least 100 C# methods in the local dataset")
    if index_data["counts"]["subActions"] < 300:
        raise AssertionError("Expected at least 300 sub-actions in the local dataset")
    if index_data["counts"]["triggers"] < 300:
        raise AssertionError("Expected at least 300 triggers in the local dataset")
    if index_data["counts"]["wikiPages"] < 10:
        raise AssertionError("Expected at least 10 wiki pages in the local dataset")


if __name__ == "__main__":
    main()
