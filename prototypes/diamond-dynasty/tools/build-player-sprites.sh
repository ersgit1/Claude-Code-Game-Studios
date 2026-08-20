#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BATTER_SOURCE="$ROOT_DIR/art/concepts/original-batter-side-swing-concept-v6.png"
PITCHER_SOURCE="$ROOT_DIR/art/concepts/original-player-sprite-concept-v3-alpha.png"
OUTPUT_DIR="$ROOT_DIR/art/sprites"

mkdir -p "$OUTPUT_DIR"

render_sprite() {
  local source="$1"
  local name="$2"
  local crop="$3"
  local scale_box="$4"
  local canvas="$5"
  local alignment="$6"
  local key_filter="${7:-colorkey=color=0xED08E0:similarity=0.22:blend=0.0}"

  ffmpeg -y -loglevel error -i "$source" \
    -vf "crop=$crop,$key_filter,scale=$scale_box:force_original_aspect_ratio=decrease:flags=neighbor,format=rgba,pad=$canvas:$alignment:color=0x00000000" \
    -frames:v 1 "$OUTPUT_DIR/$name.png"
}

# The source is a regular 3x3 grid. Each crop keeps a stable camera-facing
# silhouette and baseline; the slightly larger cell recreates the close,
# foreground batter scale of a classic pitcher/batter screen without copying a
# commercial player, uniform, or pose.
BATTER_KEY="colorkey=color=0xD30DC8:similarity=0.34:blend=0.0"
render_sprite "$BATTER_SOURCE" batter-0 "418:418:0:0" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-1 "418:418:418:0" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-2 "418:418:836:0" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-3 "418:418:0:418" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-4 "418:418:418:418" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-5 "418:418:836:418" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-6 "418:418:0:836" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-7 "418:418:418:836" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"
render_sprite "$BATTER_SOURCE" batter-8 "418:418:784:836" "114:108" "120:112" "(ow-iw)/2:112-ih" "$BATTER_KEY"

render_sprite "$PITCHER_SOURCE" pitcher-0 "150:310:40:575" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-1 "175:310:230:575" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-2 "195:290:400:590" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-3 "280:290:575:590" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-4 "225:270:840:610" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-5 "240:250:1080:630" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-6 "165:280:1310:600" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"

printf 'Built 16 native player sprites in %s\n' "$OUTPUT_DIR"
