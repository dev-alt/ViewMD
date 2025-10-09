#!/bin/bash

# Sync Markdown Viewer from WSL to Windows
# Target: C:\Users\andre\Desktop\Projects\Markdown_viewer

echo "🔄 Syncing Markdown Viewer to Windows..."
echo ""

# Define paths
WSL_SOURCE="/root/projects/Markdown_viewer"
WINDOWS_TARGET="/mnt/c/Users/andre/Desktop/Projects/Markdown_viewer"

# Create target directory if it doesn't exist
echo "📁 Creating target directory if needed..."
mkdir -p "$WINDOWS_TARGET"

# Sync files (excluding bin, obj, and hidden files)
echo "📦 Copying files..."
rsync -av \
    --exclude='bin/' \
    --exclude='obj/' \
    --exclude='.vs/' \
    --exclude='.vscode/' \
    --exclude='*.user' \
    --exclude='.git/' \
    "$WSL_SOURCE/" \
    "$WINDOWS_TARGET/"

echo ""
echo "✅ Sync complete!"
echo ""
echo "📍 Files synced to: $WINDOWS_TARGET"
echo "🪟 Windows path: C:\Users\andre\Desktop\Projects\Markdown_viewer"
echo ""
echo "To run on Windows:"
echo "  1. Open PowerShell in the Windows directory"
echo "  2. Run: dotnet build"
echo "  3. Run: dotnet run"
echo ""
