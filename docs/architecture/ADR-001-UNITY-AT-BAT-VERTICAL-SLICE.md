# ADR-001: Unity at-bat vertical-slice architecture

## Status

Accepted for the first vertical slice.

## Context

The browser prototype validates the interaction and art direction but is
explicitly throwaway code. The Unity rebuild needs deterministic rules that can
be tested without entering Play Mode, while its presentation needs exact pixel
placement and a toolchain already available on the development machine.

## Decision

- Build the Unity project under `unity/diamond-dynasty` so the reference browser
  prototype remains isolated.
- Keep `AtBatRules` as a plain C# service with injected randomness and time
  values. `DiamondDynastyController` owns Unity input, presentation, and lifecycle.
- Store tunable gameplay values in an `AtBatConfig` ScriptableObject.
- Render original PNG sprites with one pixel per unit, point filtering,
  uncompressed textures, and integer-aligned transforms in a `256x224`
  orthographic camera.
- Use the Unity Input System for keyboard and gamepad input.
- Install Coplay Unity MCP in the project, while retaining batch-mode commands as
  reproducible build and test evidence.

## Consequences

The rules can be tested quickly and migrated across presentation changes. Unity
2021.3 LTS was the locally proven Coplay combination but could not activate in
batch mode; the first slice therefore uses the installed and licensed Unity
`6000.5.5f1`. The repository's Unity 6.3 LTS reference remains the stability
baseline to evaluate before expanding production scope. Rendering avoids a
physics or render-pipeline dependency, but later fielding cameras and platform
UI will require follow-up architecture decisions.
