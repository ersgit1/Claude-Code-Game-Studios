// PROTOTYPE - NOT FOR PRODUCTION
// Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
// Date: 2026-08-20

import assert from "node:assert/strict";
import test from "node:test";
import {
  STRIKE_ZONE,
  applyOutcome,
  beginPitch,
  createGameState,
  createPitch,
  moveAim,
  resolveSwing,
  resolveTakenPitch,
} from "../src/game-model.mjs";

function sequence(values) {
  let index = 0;
  return () => values[index++ % values.length];
}

test("test_pitch_generation_seeded_sequence_returns_expected_pitch", () => {
  const pitch = createPitch(100, sequence([0, 0.9, 0.3, 0.5, 0.5]));
  assert.equal(pitch.name, "FOUR-SEAM");
  assert.equal(pitch.start, 100);
  assert.ok(pitch.targetX >= STRIKE_ZONE.left);
  assert.ok(pitch.targetY <= STRIKE_ZONE.bottom);
});

test("test_aim_movement_beyond_zone_clamps_to_playable_bounds", () => {
  const state = createGameState();
  const moved = moveAim(moveAim(state, -999, -999), 999, 999);
  assert.equal(moved.aimX, STRIKE_ZONE.right + 10);
  assert.equal(moved.aimY, STRIKE_ZONE.bottom + 10);
});

test("test_taken_pitch_inside_zone_adds_called_strike", () => {
  const state = {
    ...createGameState(),
    phase: "pitching",
    pitch: { name: "TEST", start: 0, duration: 100, targetX: 128, targetY: 155, breakX: 0, breakY: 0 },
  };
  const result = resolveTakenPitch(state, 100);
  assert.equal(result.strikes, 1);
  assert.equal(result.result, "CALLED STRIKE");
});

test("test_taken_pitch_outside_zone_adds_ball", () => {
  const state = {
    ...createGameState(),
    phase: "pitching",
    pitch: { name: "TEST", start: 0, duration: 100, targetX: 88, targetY: 155, breakX: 0, breakY: 0 },
  };
  const result = resolveTakenPitch(state, 100);
  assert.equal(result.balls, 1);
  assert.equal(result.result, "BALL — GOOD EYE");
});

test("test_swing_with_large_timing_error_records_strike", () => {
  const pitching = beginPitch(createGameState(), 0, sequence([0, 0.9, 0, 0.5, 0.5]));
  const result = resolveSwing(pitching, 50, () => 0.5);
  assert.equal(result.strikes, 1);
  assert.match(result.result, /TIMING/);
});

test("test_perfect_barrel_contact_records_extra_base_hit", () => {
  const state = {
    ...createGameState(),
    phase: "pitching",
    aimX: 128,
    aimY: 159,
    pitch: { name: "TEST", start: 0, duration: 1000, targetX: 128, targetY: 165.4, breakX: 0, breakY: 0 },
  };
  const result = resolveSwing(state, 880, () => 0.8);
  assert.equal(result.hits, 1);
  assert.match(result.result, /HOME RUN|THREE BASES|DOUBLE/);
});

test("test_home_run_with_loaded_bases_scores_four_runs", () => {
  const state = { ...createGameState(), bases: [true, true, true] };
  const result = applyOutcome(state, { kind: "homer", label: "HOME RUN" });
  assert.equal(result.runs, 4);
  assert.deepEqual(result.bases, [false, false, false]);
  assert.equal(result.hits, 1);
});

test("test_walk_with_unforced_runner_preserves_runner_position", () => {
  const state = { ...createGameState(), balls: 3, bases: [false, true, false] };
  const result = applyOutcome(state, { kind: "ball" });
  assert.equal(result.runs, 0);
  assert.deepEqual(result.bases, [true, true, false]);
  assert.equal(result.balls, 0);
});

test("test_walk_with_loaded_bases_forces_home_one_run", () => {
  const state = { ...createGameState(), balls: 3, bases: [true, true, true] };
  const result = applyOutcome(state, { kind: "ball" });
  assert.equal(result.runs, 1);
  assert.deepEqual(result.bases, [true, true, true]);
});

test("test_third_strike_records_out_and_resets_count", () => {
  const state = { ...createGameState(), balls: 2, strikes: 2 };
  const result = applyOutcome(state, { kind: "strike" });
  assert.equal(result.outs, 1);
  assert.equal(result.balls, 0);
  assert.equal(result.strikes, 0);
});

test("test_third_out_advances_inning_and_clears_bases", () => {
  const state = { ...createGameState(), outs: 2, bases: [true, false, true] };
  const result = applyOutcome(state, { kind: "out", label: "OUT" });
  assert.equal(result.inning, 2);
  assert.equal(result.outs, 0);
  assert.deepEqual(result.bases, [false, false, false]);
});
