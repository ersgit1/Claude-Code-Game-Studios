# At-Bat Vertical Slice

## Overview

Rebuild the approved Diamond Dynasty browser at-bat as an original Unity 2D
vertical slice. The first milestone preserves the native `256x224` view,
pitch delivery, aim-and-timing swing, baseball count, runner advancement, and
fictional presentation while leaving the browser prototype unchanged as a
behavioral reference.

## Player Fantasy

The player reads a pitch out of the hand, tracks its speed and break, moves the
batting cursor, and commits to a swing that feels immediate and skillful. Strong
contact should look and sound decisive; taking a close ball should feel earned.

## Detailed Rules

- The opening menu requires the player to choose Batter or Pitcher before gameplay begins.
- Batter mode uses a CPU pitcher. Arrow keys or the gamepad D-pad move the contact cursor, and Space, A, or gamepad South swings during a live pitch.
- Batter mode never exposes the CPU pitch type, speed, intended target, actual target, or power.
- Pitcher mode uses Q/E, 1-3, or gamepad shoulder buttons to choose a pitch and arrows or the D-pad to place a pretarget locator.
- Holding Enter, Space, or gamepad South charges pitch power; releasing delivers the pitch. Higher power increases velocity and location error.
- Pitcher mode uses a CPU batter that can take, chase, miss, foul, make an out, or reach base through the existing contact rules.
- Pitch type controls duration, visible speed, horizontal break, and vertical drop.
- A taken pitch resolves as a ball or called strike after crossing the plate.
- A swing combines timing error and cursor-to-ball distance into contact quality.
- Outcomes update balls, strikes, outs, hits, runs, bases, and innings.
- The batter returns to the raised ready pose after the result hold.

## Formulas

- `progress = clamp((now - pitchStart) / duration, 0, 1)`
- `depth = progress^2 * (3 - 2 * progress)`
- `speedScale = lerp(minSpeedScale, maxSpeedScale, power)`
- `duration = baseDuration / speedScale`
- `missRadius = lerp(minMiss, maxMiss, power^accuracyExponent)`
- `missDistance = sqrt(random01) * missRadius`
- `timing = clamp(1 - abs(progress - contactProgress) / timingWindow, 0, 1)`
- `barrel = clamp(1 - spatialError / spatialWindow, 0, 1)`
- `quality = timing * 0.58 + barrel * 0.42`

All values are supplied by `AtBatConfig`, not embedded in the rules engine.

## Edge Cases

- A foul with two strikes does not produce strike three.
- Ball four advances only forced runners and scores only when the bases are loaded.
- A third out clears the bases and begins the next inning.
- Inputs outside a live pitch cannot produce duplicate outcomes.
- A resolved play returns to ready exactly once.
- Role-confirm input cannot also begin a pitch or swing in the same frame.
- Pitch selection and target lock when charging begins.
- Losing application focus cancels an incomplete charge instead of leaving the meter stuck.
- The requested locator is the final plate-crossing point; break never changes its meaning.
- Returning to role selection resets the game and all transient input state.

## Dependencies

- Unity `6000.5.5f1` for this first slice
- Unity Input System `1.19.0`
- Unity Test Framework `1.7.0`
- Coplay Unity MCP `10.1.0`
- Original stadium and sixteen original player PNG frames from the approved prototype

## Tuning Knobs

Pitch duration, break, target-ball probability, cursor step and bounds, automatic
CPU-pitch delay, pitch-target bounds and step, charge duration, speed scale,
miss-radius curve, CPU swing and aim behavior, contact progress, timing window,
spatial window, contact thresholds, result hold time, animation frame cadence,
and native resolution are serialized in `AtBatConfig`.

## Acceptance Criteria

- The Unity scene opens and plays without compile errors.
- Stadium and all player textures import with point filtering and no compression.
- Pitcher and batter animate through their complete frame sets.
- Keyboard and gamepad can pitch, aim, and swing.
- Role selection is the first interactive screen and cleanly enters either Batter or Pitcher mode.
- Batter mode hides all pitcher-only information through every phase.
- Pitcher mode supports all three pitch types, a pretarget locator, and a hold/release meter whose power and error both increase monotonically.
- The CPU-controlled opponent completes at-bats in either role.
- The ball remains a white circular pixel silhouette with transparent corners at every depth size.
- Counts, walks, hits, outs, runs, bases, and innings match deterministic tests.
- Unity EditMode tests pass in batch mode.
- A real Game-view screenshot demonstrates the native composition.
