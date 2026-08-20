#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="$ROOT_DIR/screenshots/character-animation-sheet-v3.png"
TEMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TEMP_DIR"' EXIT

frame=0
for sprite in "$ROOT_DIR"/art/sprites/batter-{0..8}.png "$ROOT_DIR"/art/sprites/pitcher-{0..6}.png; do
  ffmpeg -y -loglevel error -i "$sprite" \
    -vf "scale=156:150:force_original_aspect_ratio=decrease:flags=neighbor,pad=176:170:(ow-iw)/2:(oh-ih)/2:color=0x20464c" \
    -frames:v 1 "$TEMP_DIR/frame-$frame.png"
  frame=$((frame + 1))
done

ffmpeg -y -loglevel error -start_number 0 -i "$TEMP_DIR/frame-%d.png" \
  -frames:v 16 -vf "tile=4x4:padding=4:margin=4:color=0x07101f" "$OUTPUT"

printf 'Built character animation sheet at %s\n' "$OUTPUT"
