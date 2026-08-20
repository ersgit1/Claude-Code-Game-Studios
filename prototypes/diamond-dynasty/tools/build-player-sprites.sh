#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="$ROOT_DIR/art/concepts/original-player-sprite-concept-v3.png"
OUTPUT_DIR="$ROOT_DIR/art/sprites"

mkdir -p "$OUTPUT_DIR"

render_sprite() {
  local name="$1"
  local crop="$2"
  local scale_box="$3"
  local canvas="$4"
  local alignment="$5"

  ffmpeg -y -loglevel error -i "$SOURCE" \
    -vf "crop=$crop,colorkey=color=0xED08E0:similarity=0.22:blend=0.0,scale=$scale_box:force_original_aspect_ratio=decrease:flags=neighbor,format=rgba,pad=$canvas:$alignment:color=0x00000000" \
    -frames:v 1 "$OUTPUT_DIR/$name.png"
}

# Source crops are intentionally generous. Transparent padding gives every
# batter a 104x100 cell and every pitcher a 76x58 cell with a shared baseline.
render_sprite batter-0 "220:360:20:100" "96:92" "104:100" "(ow-iw)/2:100-ih"
render_sprite batter-1 "240:320:450:130" "96:92" "104:100" "(ow-iw)/2:100-ih"
render_sprite batter-2 "190:340:255:110" "96:92" "104:100" "(ow-iw)/2:100-ih"
render_sprite batter-3 "220:300:680:150" "96:92" "104:100" "(ow-iw)/2:100-ih"
render_sprite batter-4 "180:300:900:150" "96:92" "104:100" "(ow-iw)/2:100-ih"
render_sprite batter-5 "205:330:1095:120" "96:92" "104:100" "(ow-iw)/2:100-ih"
render_sprite batter-6 "190:300:1310:150" "96:92" "104:100" "(ow-iw)/2:100-ih"

render_sprite pitcher-0 "150:310:40:575" "68:52" "76:58" "(ow-iw)/2:58-ih"
render_sprite pitcher-1 "175:310:230:575" "68:52" "76:58" "(ow-iw)/2:58-ih"
render_sprite pitcher-2 "195:290:400:590" "68:52" "76:58" "(ow-iw)/2:58-ih"
render_sprite pitcher-3 "280:290:575:590" "68:52" "76:58" "(ow-iw)/2:58-ih"
render_sprite pitcher-4 "225:270:840:610" "68:52" "76:58" "(ow-iw)/2:58-ih"
render_sprite pitcher-5 "240:250:1080:630" "68:52" "76:58" "(ow-iw)/2:58-ih"
render_sprite pitcher-6 "165:280:1310:600" "68:52" "76:58" "(ow-iw)/2:58-ih"

printf 'Built 14 native player sprites in %s\n' "$OUTPUT_DIR"
