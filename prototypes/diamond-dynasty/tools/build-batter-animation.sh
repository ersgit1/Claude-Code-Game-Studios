#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="$ROOT_DIR/screenshots/batter-animation.gif"
TEMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TEMP_DIR"' EXIT

durations=(0.50 0.070 0.065 0.060 0.060 0.060 0.070 0.515 0.45)
sequence=(0 1 2 3 4 5 6 7 0)

for frame in "${!sequence[@]}"; do
  sprite_frame="${sequence[$frame]}"
  ffmpeg -y -loglevel error \
    -i "$ROOT_DIR/assets/stadium.png" \
    -i "$ROOT_DIR/art/sprites/pitcher-0.png" \
    -i "$ROOT_DIR/art/sprites/batter-$sprite_frame.png" \
    -filter_complex "[0:v][1:v]overlay=90:65:format=auto[field];[field][2:v]overlay=20:94:format=auto,scale=512:448:flags=neighbor" \
    -frames:v 1 "$TEMP_DIR/frame-$frame.png"
  printf "file '%s'\nduration %s\n" "$TEMP_DIR/frame-$frame.png" "${durations[$frame]}" >> "$TEMP_DIR/frames.txt"
done

# Repeat the final raised READY frame so the concat demuxer preserves its hold.
printf "file '%s'\n" "$TEMP_DIR/frame-8.png" >> "$TEMP_DIR/frames.txt"

ffmpeg -y -loglevel error -f concat -safe 0 -i "$TEMP_DIR/frames.txt" \
  -filter_complex "split[frames][palette];[palette]palettegen=stats_mode=diff[colors];[frames][colors]paletteuse=dither=none" \
  -loop 0 "$OUTPUT"

printf 'Built batter animation review at %s\n' "$OUTPUT"
