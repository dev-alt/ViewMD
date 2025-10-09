#!/usr/bin/env bash
# Combined helper to sync from WSL to Windows and run the app
# Usage: ./run-all.sh [--sync] [--wsl-source PATH] [--windows-target PATH] [--no-run] [--dry-run]

set -euo pipefail

SYNC=false
DRY_RUN=false
NO_RUN=false
WSL_SOURCE="/root/projects/Markdown_viewer"
WINDOWS_TARGET="/mnt/c/Users/andre/Desktop/Projects/Markdown_viewer"

print_help() {
    cat <<EOF
Usage: $0 [options]

Options:
  --sync                Sync files from WSL to Windows (rsync)
  --wsl-source PATH     Override default WSL source (default: $WSL_SOURCE)
  --windows-target PATH Override Windows target (default: $WINDOWS_TARGET)
  --no-run              Build but do not run the application
  --dry-run             Print actions but do not execute (passed to rsync)
  -h, --help            Show this help
EOF
}

# Parse args
while [[ $# -gt 0 ]]; do
    case "$1" in
        --sync) SYNC=true; shift ;;
        --wsl-source) WSL_SOURCE="$2"; shift 2 ;;
        --windows-target) WINDOWS_TARGET="$2"; shift 2 ;;
        --no-run) NO_RUN=true; shift ;;
        --dry-run) DRY_RUN=true; shift ;;
        -h|--help) print_help; exit 0 ;;
        *) echo "Unknown arg: $1"; print_help; exit 1 ;;
    esac
done

# Move to repo root (assume script is in scripts/)
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
REPO_ROOT=$(dirname "$SCRIPT_DIR")
cd "$REPO_ROOT"

if [ "$SYNC" = true ]; then
    echo "🔄 Syncing from WSL: $WSL_SOURCE -> $WINDOWS_TARGET"
    mkdir -p "$WINDOWS_TARGET"

    RSYNC_OPTS=( -av )
    if [ "$DRY_RUN" = true ]; then
        RSYNC_OPTS+=( --dry-run )
    fi

    RSYNC_OPTS+=( --exclude='bin/' --exclude='obj/' --exclude='.vs/' --exclude='.vscode/' --exclude='*.user' --exclude='.git/' )

    rsync "${RSYNC_OPTS[@]}" "$WSL_SOURCE/" "$WINDOWS_TARGET/"

    echo "✅ Sync complete: $WINDOWS_TARGET"
    echo
fi

# Clean and build
echo "Cleaning previous builds..."
dotnet clean --verbosity quiet || { echo "Clean failed"; exit 1; }

echo "Building project..."
dotnet build --verbosity quiet || { echo "Build failed. Showing details..."; dotnet build; exit 1; }

echo "Build successful"

if [ "$NO_RUN" = false ]; then
    echo "Starting Markdown Viewer..."
    dotnet run
else
    echo "Skipping run because --no-run was given"
fi
