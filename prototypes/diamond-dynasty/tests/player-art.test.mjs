// PROTOTYPE - NOT FOR PRODUCTION

import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import test from "node:test";
import {
  BATTER_SPRITE_PATHS,
  PITCHER_SPRITE_PATHS,
  batterFrameForElapsed,
  batterFrameForSwing,
  drawBatter,
  drawPitcher,
  pitcherFrameForProgress,
} from "../src/player-art.mjs";

function pngDimensions(relativePath) {
  const bytes = readFileSync(new URL(`../${relativePath.replace("./", "")}`, import.meta.url));
  assert.equal(bytes.subarray(1, 4).toString(), "PNG");
  return {
    width: bytes.readUInt32BE(16),
    height: bytes.readUInt32BE(20),
    colorType: bytes[25],
  };
}

function pngHash(relativePath) {
  const bytes = readFileSync(new URL(`../${relativePath.replace("./", "")}`, import.meta.url));
  return createHash("sha256").update(bytes).digest("hex");
}

function animationGif() {
  return readFileSync(new URL("../screenshots/batter-animation.gif", import.meta.url));
}

function captureRaster(draw) {
  const rectangles = [];
  const context = {
    fillStyle: "",
    fillRect(...values) {
      rectangles.push([this.fillStyle, ...values]);
    },
  };
  draw(context);
  return rectangles;
}

test("batter animation exposes contact, extension, and a held follow-through", () => {
  assert.equal(batterFrameForSwing(null, 1000), 0);
  assert.equal(batterFrameForSwing(1000, 1000), 1);
  assert.equal(batterFrameForElapsed(-1), 0);
  assert.equal(batterFrameForElapsed(80), 2);
  assert.equal(batterFrameForElapsed(150), 3);
  assert.equal(batterFrameForElapsed(250), 5);
  assert.equal(batterFrameForElapsed(320), 6);
  assert.equal(batterFrameForElapsed(400), 7);
  assert.equal(batterFrameForElapsed(1000), 8);
});

test("pitcher animation exposes seven ordered delivery states", () => {
  const frames = [0, 0.15, 0.3, 0.5, 0.65, 0.8, 1].map(pitcherFrameForProgress);
  assert.deepEqual(frames, [0, 1, 2, 3, 4, 5, 6]);
});

test("detailed character limbs stay on the native integer pixel grid", () => {
  const rasters = [
    ...Array.from({ length: 9 }, (_, frame) => captureRaster((ctx) => drawBatter(ctx, frame))),
    ...Array.from({ length: 7 }, (_, frame) => captureRaster((ctx) => drawPitcher(ctx, frame))),
  ];
  for (const raster of rasters) {
    assert.ok(raster.length > 300);
    for (const [, x, y, width, height] of raster) {
      assert.ok([x, y, width, height].every(Number.isInteger));
      assert.ok(width > 0 && height > 0);
    }
  }
});

test("every batter and pitcher pose has a distinct full-body raster", () => {
  const signature = (draw) => JSON.stringify(captureRaster(draw));
  const batterFrames = new Set(Array.from(
    { length: 9 },
    (_, frame) => signature((ctx) => drawBatter(ctx, frame)),
  ));
  const pitcherFrames = new Set(Array.from(
    { length: 7 },
    (_, frame) => signature((ctx) => drawPitcher(ctx, frame)),
  ));
  assert.equal(batterFrames.size, 9);
  assert.equal(pitcherFrames.size, 7);
});

test("sprite manifests expose nine batter and seven pitcher alpha PNG frames", () => {
  assert.equal(BATTER_SPRITE_PATHS.length, 9);
  assert.equal(PITCHER_SPRITE_PATHS.length, 7);
  for (const path of BATTER_SPRITE_PATHS) {
    assert.deepEqual(pngDimensions(path), { width: 104, height: 100, colorType: 6 });
  }
  for (const path of PITCHER_SPRITE_PATHS) {
    assert.deepEqual(pngDimensions(path), { width: 76, height: 58, colorType: 6 });
  }
  assert.equal(new Set([
    ...BATTER_SPRITE_PATHS.map(pngHash),
    ...PITCHER_SPRITE_PATHS.map(pngHash),
  ]).size, 16);
});

test("rear-view batter renders behind the left side of the plate on integer coordinates", () => {
  const images = [];
  const context = {
    fillStyle: "",
    fillRect() {},
    drawImage(...values) { images.push(values); },
  };
  const batter = { complete: true, naturalWidth: 104 };
  const pitcher = { complete: true, naturalWidth: 76 };
  drawBatter(context, 0, { sprite: batter });
  drawPitcher(context, 0, { sprite: pitcher });
  assert.deepEqual(images, [
    [batter, 30, 94],
    [pitcher, 90, 65],
  ]);
  assert.ok(images[0][1] >= 0 && images[0][1] < images[1][1]);
  assert.ok(images[0][1] + batter.naturalWidth <= 256);
  assert.ok(images.flatMap(([, ...coordinates]) => coordinates).every(Number.isInteger));
});

test("batter review artifact contains a full nine-pose cycle and finish hold", () => {
  const gif = animationGif();
  assert.equal(gif.subarray(0, 6).toString(), "GIF89a");
  assert.equal(gif.readUInt16LE(6), 512);
  assert.equal(gif.readUInt16LE(8), 448);

  let frameCount = 0;
  for (let index = 0; index < gif.length - 2; index += 1) {
    if (gif[index] === 0x21 && gif[index + 1] === 0xf9 && gif[index + 2] === 0x04) {
      frameCount += 1;
    }
  }
  // The final follow-through hold is repeated so the GIF concat pipeline
  // preserves its display duration before looping back to ready.
  assert.equal(frameCount, 10);
});
