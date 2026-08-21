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

- Enter or gamepad Start/South delivers the next pitch.
- Arrow keys or the gamepad D-pad move the contact cursor in fixed native-pixel steps.
- Space, A, or gamepad South swings during a live pitch.
- Pitch type controls duration, visible speed, horizontal break, and vertical drop.
- A taken pitch resolves as a ball or called strike after crossing the plate.
- A swing combines timing error and cursor-to-ball distance into contact quality.
- Outcomes update balls, strikes, outs, hits, runs, bases, and innings.
- The batter returns to the raised ready pose after the result hold.

## Formulas

- `progress = clamp((now - pitchStart) / duration, 0, 1)`
- `depth = progress^2 * (3 - 2 * progress)`
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

## Dependencies

- Unity `6000.5.5f1` for this first slice
- Unity Input System `1.19.0`
- Unity Test Framework `1.7.0`
- Coplay Unity MCP `10.1.0`
- Original stadium and sixteen original player PNG frames from the approved prototype

## Tuning Knobs

Pitch duration, break, target-ball probability, cursor step and bounds, contact
progress, timing window, spatial window, contact thresholds, result hold time,
animation frame cadence, and native resolution are serialized in `AtBatConfig`.

## Acceptance Criteria

- The Unity scene opens and plays without compile errors.
- Stadium and all player textures import with point filtering and no compression.
- Pitcher and batter animate through their complete frame sets.
- Keyboard and gamepad can pitch, aim, and swing.
- Counts, walks, hits, outs, runs, bases, and innings match deterministic tests.
- Unity EditMode tests pass in batch mode.
- A real Game-view screenshot demonstrates the native composition.
