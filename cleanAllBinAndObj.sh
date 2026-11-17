#!/usr/bin/env sh
# Deletes all bin and obj folders below this directory.
set -e
cd "$(dirname "$0")"
find . -type d \( -name bin -o -name obj \) -prune -print -exec rm -rf {} +
echo "Done."