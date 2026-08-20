// PROTOTYPE - NOT FOR PRODUCTION

import assert from "node:assert/strict";
import test from "node:test";
import {
  batterFrameForElapsed,
  drawBatter,
  drawCatcher,
  drawPitcher,
  pitcherFrameForProgress,
} from "../src/player-art.mjs";

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
