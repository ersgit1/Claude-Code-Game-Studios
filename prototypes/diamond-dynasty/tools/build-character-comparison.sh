#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="$ROOT_DIR/screenshots/character-comparison-v3.png"

ffmpeg -y -loglevel error \
  -i "$ROOT_DIR/art/references/ken-griffey-jr-snes-reference.png" \
  -i "$ROOT_DIR/assets/stadium.png" \
  -i "$ROOT_DIR/art/sprites/pitcher-0.png" \
  -i "$ROOT_DIR/art/sprites/batter-0.png" \
  -filter_complex \
    "[0:v]scale=768:768:flags=neighbor[left];[1:v][2:v]overlay=90:65:format=auto[field];[field][3:v]overlay=20:94:format=auto,scale=768:672:flags=neighbor,pad=768:768:0:48:color=0x07101f[right];[left][right]hstack=inputs=2" \
  -frames:v 1 "$OUTPUT"

printf 'Built character comparison at %s\n' "$OUTPUT"
