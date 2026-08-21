# Technical Preferences

<!-- Populated by /setup-engine. Updated as the user makes decisions throughout development. -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 2D `6000.5.5f1` for the first vertical slice; re-evaluate against Unity 6.3 LTS before production expansion
- **Language**: C#
- **Rendering**: Orthographic 2D, `256x224` logical frame, point-filtered sprites, integer-aligned transforms
- **Physics**: Deterministic gameplay math; no physics dependency for the at-bat slice

## Naming Conventions

- **Classes**: PascalCase
- **Variables**: camelCase; serialized private fields use camelCase
- **Signals/Events**: PascalCase
- **Files**: Match the primary C# type name
- **Scenes/Prefabs**: PascalCase
- **Constants**: PascalCase

## Performance Budgets

- **Target Framerate**: 60 FPS
- **Frame Budget**: 16.67 ms
- **Draw Calls**: Under 50 for the batting view
- **Memory Ceiling**: 256 MB for the first vertical slice

## Testing

- **Framework**: Unity Test Framework with NUnit EditMode tests
- **Minimum Coverage**: Every rules transition and scoring edge case changed by a task
- **Required Tests**: Pitch sampling, swing resolution, counts, walks, outs, runner advancement, and inning transitions

## Forbidden Patterns

<!-- Add patterns that should never appear in this project's codebase -->
- Do not use texture filtering or non-integer sprite placement in the native gameplay camera.
- Do not make gameplay outcomes depend on frame rate or MonoBehaviour lifecycle order.

## Allowed Libraries / Addons

<!-- Add approved third-party dependencies here -->
- Coplay Unity MCP `10.1.0`
- Unity Input System `1.19.0`
- Unity Test Framework `1.7.0`

## Architecture Decisions Log

<!-- Quick reference linking to full ADRs in docs/architecture/ -->
- `docs/architecture/ADR-001-UNITY-AT-BAT-VERTICAL-SLICE.md`
