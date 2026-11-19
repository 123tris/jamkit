#!/bin/bash
# Mirrors a scaffolded project's Assets/_Project into the package's Template~ folder, .meta
# files included. The project template is edited and play-tested in a scaffolded project, then
# exported with this. Save the project in Unity first.
#
# Usage: bash Packages/com.metz.jamkit/Tools~/export-template.sh [project-root]
set -eu
PROJ="${1:-$(cd "$(dirname "$0")/../../.." && pwd)}"
PKG="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$PROJ/Assets/_Project"
DEST="$PKG/Template~"

if [ ! -d "$SRC" ]; then echo "No scaffolded project at $SRC"; exit 2; fi

rm -rf "$DEST"
cp -r "$SRC" "$DEST"
echo "Exported $(find "$DEST" -type f ! -name '*.meta' | wc -l) files to $DEST"
