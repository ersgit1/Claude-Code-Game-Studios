# ADR-002: Role selection and asymmetric at-bat controls

## Status

Accepted for the second Unity vertical-slice milestone.

## Context

The first Unity slice exposed one shared input path: the player could start a
random pitch, aim the batter cursor, and swing. The next milestone must let the
player choose either side of the at-bat. Pitcher control also needs selectable
pitch types, an intended plate location, and a hold/release power mechanic in
which velocity competes with command. Batter control must not reveal the
pitcher's private selection, target, or meter state.

## Decision

- Keep one scene and the existing `AtBatRules` plus `DiamondDynastyController`
  ownership boundary.
- Add player role and role-specific setup/charging phases to deterministic
  `AtBatState`; do not infer role from visible UI.
- Keep current immediate-mode UI for this native-pixel slice so the feature does
  not introduce a second screen-space UI framework or new serialized UI assets.
- Store pitch selection, requested plate target, charge, resolved duration,
  velocity, and error on the active pitch. The requested target always means
  final plate-crossing location, including for breaking pitches.
- Generate location error from injected randomness inside a configurable disk.
  Charge raises both speed and the disk's maximum radius.
- Use the existing randomized CPU pitcher in Batter mode and a small,
  configuration-driven CPU batter in Pitcher mode.
- Treat pitch type, speed, requested target, actual target, charge, and accuracy
  radius as pitcher-only information. Batter mode receives a neutral in-flight
  message.
- Replace the scaled one-pixel ball with four generated native-pixel circular
  textures. Each has transparent corners, a white center, subtle lower-edge
  shading, and seams on the larger frames.

## Consequences

Both roles remain testable without Play Mode because authority stays in the
plain C# rules service. The controller still owns raw Input System polling,
charge-button release detection, GUI rendering, and sprite selection. Immediate
mode UI and runtime-generated ball textures remain deliberate vertical-slice
debt; a later production UI/art pass can replace them without changing the
role, pitch, or outcome contracts.
