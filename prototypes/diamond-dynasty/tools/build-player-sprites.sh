#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BATTER_SOURCE="$ROOT_DIR/art/concepts/original-batter-rear-swing-concept-v5-alpha.png"
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

# Source crops are intentionally generous. Transparent padding gives every
# batter a 104x100 cell and every pitcher a 76x58 cell with a shared baseline.
render_sprite "$BATTER_SOURCE" batter-0 "320:340:80:50" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-1 "320:340:470:50" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-2 "320:340:850:50" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-3 "320:340:100:440" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-4 "320:340:470:440" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-5 "320:340:850:440" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-6 "320:340:120:810" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-7 "320:340:480:810" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"
render_sprite "$BATTER_SOURCE" batter-8 "320:340:850:810" "96:92" "104:100" "(ow-iw)/2:100-ih" "format=rgba"

render_sprite "$PITCHER_SOURCE" pitcher-0 "150:310:40:575" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-1 "175:310:230:575" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-2 "195:290:400:590" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-3 "280:290:575:590" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-4 "225:270:840:610" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-5 "240:250:1080:630" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"
render_sprite "$PITCHER_SOURCE" pitcher-6 "165:280:1310:600" "68:52" "76:58" "(ow-iw)/2:58-ih" "format=rgba"

printf 'Built 16 native player sprites in %s\n' "$OUTPUT_DIR"
