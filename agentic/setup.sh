#!/usr/bin/env bash
# One-time setup for the agentic documentation mirrors and summary tooling.
#
# Run this from the repository root to clone the Streamer.bot docs, wiki,
# and TypeScript WebSocket client into agentic/ and install the Python
# dependencies needed to build the summary.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

(
    cd "${REPO_ROOT}/agentic"

    echo "Cloning Streamer.bot documentation and client sources ..."
    git clone https://github.com/Streamerbot/docs streamerbot-docs
    git clone https://github.com/Streamerbot/client streamerbot-typescript-websocket-client
    git clone https://github.com/Streamerbot/streamerbot-wiki

    echo "Creating Python virtual environment ..."
    python3 -m venv "${REPO_ROOT}/.venv"

    echo "Installing Python dependencies ..."
    source "${REPO_ROOT}/.venv/bin/activate"
    python3 -m pip install -r "${REPO_ROOT}/agentic/streamerbot-docs-summary/requirements.txt"
)

echo "Setup complete. Run 'bash agentic/sync-streamerbot-docs.sh' to refresh the summary."
