// PROTOTYPE - NOT FOR PRODUCTION

import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import test from "node:test";
import {
  BATTER_SPRITE_PATHS,
  PITCHER_SPRITE_PATHS,
  batterFrameForElapsed,
  drawBatter,
  drawCatcher,
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

test("batter animation exposes load, contact, and follow-through states", () => {
  assert.equal(batterFrameForElapsed(-1), 0);
  assert.equal(batterFrameForElapsed(80), 2);
  assert.equal(batterFrameForElapsed(150), 3);
  assert.equal(batterFrameForElapsed(300), 5);
  assert.equal(batterFrameForElapsed(1000), 6);
});

test("pitcher animation exposes seven ordered delivery states", () => {
  const frames = [0, 0.15, 0.3, 0.5, 0.65, 0.8, 1].map(pitcherFrameForProgress);
  assert.deepEqual(frames, [0, 1, 2, 3, 4, 5, 6]);
});

test("detailed character limbs stay on the native integer pixel grid", () => {
  const rasters = [
    ...Array.from({ length: 7 }, (_, frame) => captureRaster((ctx) => drawBatter(ctx, frame))),
    ...Array.from({ length: 7 }, (_, frame) => captureRaster((ctx) => drawPitcher(ctx, frame))),
    captureRaster((ctx) => drawCatcher(ctx)),
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
    { length: 7 },
    (_, frame) => signature((ctx) => drawBatter(ctx, frame)),
  ));
  const pitcherFrames = new Set(Array.from(
    { length: 7 },
    (_, frame) => signature((ctx) => drawPitcher(ctx, frame)),
  ));
  assert.equal(batterFrames.size, 7);
  assert.equal(pitcherFrames.size, 7);
});

test("sprite manifests expose seven alpha PNG frames at native dimensions", () => {
  assert.equal(BATTER_SPRITE_PATHS.length, 7);
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
  ]).size, 14);
});

test("loaded sprites render on integer-aligned gameplay coordinates", () => {
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
    [batter, 152, 94],
    [pitcher, 90, 65],
  ]);
  assert.ok(images.flatMap(([, ...coordinates]) => coordinates).every(Number.isInteger));
});
