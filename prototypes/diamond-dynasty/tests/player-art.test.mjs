// PROTOTYPE - NOT FOR PRODUCTION

import assert from "node:assert/strict";
import test from "node:test";
import {
  batterFrameForElapsed,
  pitcherFrameForProgress,
} from "../src/player-art.mjs";

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
