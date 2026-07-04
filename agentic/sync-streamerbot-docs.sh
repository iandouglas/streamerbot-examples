#!/usr/bin/env bash
# Sync the Streamer.bot docs and wiki sub-repos, then regenerate the local summary.
#
# This script is safe to run locally or from a scheduled CI job.
# It fetches all remote branches so work-in-progress docs are visible in
# work-branches.json, then builds the summary from the default branch.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

DOCS_DIR="${REPO_ROOT}/agentic/streamerbot-docs"
WIKI_DIR="${REPO_ROOT}/agentic/streamerbot-wiki"
SUMMARY_DIR="${REPO_ROOT}/agentic/streamerbot-docs-summary"
VENV_DIR="${REPO_ROOT}/.venv"

# Default branch names to update. Override with DEFAULT_BRANCH if your repos differ.
DEFAULT_BRANCH="${DEFAULT_BRANCH:-main}"

ensure_repo() {
    local dir="$1"
    local url="$2"

    if [[ ! -d "${dir}/.git" ]]; then
        echo "Cloning ${url} into ${dir} ..."
        git clone "${url}" "${dir}"
    fi
}

sync_repo() {
    local dir="$1"
    local name="$2"

    echo "Syncing ${name} ..."
    git -C "${dir}" fetch --all --prune

    # Make sure the default branch is tracked locally and up to date.
    if ! git -C "${dir}" rev-parse --verify "${DEFAULT_BRANCH}" >/dev/null 2>&1; then
        git -C "${dir}" checkout -b "${DEFAULT_BRANCH}" "origin/${DEFAULT_BRANCH}"
    else
        git -C "${dir}" checkout "${DEFAULT_BRANCH}"
        git -C "${dir}" pull origin "${DEFAULT_BRANCH}"
    fi
}

ensure_python_env() {
    if [[ ! -d "${VENV_DIR}" ]]; then
        echo "Creating Python virtual environment ..."
        python3 -m venv "${VENV_DIR}"
    fi

    source "${VENV_DIR}/bin/activate"
    python3 -m pip install -q -r "${SUMMARY_DIR}/requirements.txt"
}

main() {
    ensure_repo "${DOCS_DIR}" "https://github.com/Streamerbot/docs.git"
    ensure_repo "${WIKI_DIR}" "https://github.com/Streamerbot/streamerbot-wiki.git"

    sync_repo "${DOCS_DIR}" "streamerbot-docs"
    sync_repo "${WIKI_DIR}" "streamerbot-wiki"

    ensure_python_env

    echo "Building documentation summary ..."
    python3 "${SUMMARY_DIR}/build_summary.py"

    echo "Validating generated artifacts ..."
    python3 "${SUMMARY_DIR}/validate_summary.py"

    echo "Done. Summary generated in ${SUMMARY_DIR}"
}

main "$@"
