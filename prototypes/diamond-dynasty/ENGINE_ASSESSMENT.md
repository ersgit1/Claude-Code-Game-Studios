<!-- PROTOTYPE - NOT FOR PRODUCTION
Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
Date: 2026-08-20 -->

# Engine, MCP, and pixel pipeline assessment

## Recommendation

Use this dependency-free Canvas 2D build for the first playable proof. If the
at-bat graduates into a full game, rebuild it in **Unity 2D using Coplay Unity
MCP**, not Unreal. Unity is the best fit for deterministic sprite animation,
point-filtered textures, multiplatform controller support, and rapid editor
iteration. This machine already has Coplay Unity MCP 10.1.0 installed and
registered, although its local editor server was offline during this build.

Godot 4 is the strongest open-source alternative and has emerging native and
community MCP servers, but no Godot editor or MCP was installed in this session.
Unreal Engine 5.8 now ships an official MCP, but Epic still labels it
experimental and warns that features are incomplete; its high-end 3D pipeline
adds cost without helping this sprite-first design.

## What Pixel-Bench is useful for

[Pixel-Bench](https://github.com/Retro-Diffusion/pixel-bench) is a reconstruction
benchmark, not an art generator and not a subjective “SNES quality” grader. It
distorts native 1× pixel art and scores whether reconstruction tools recover
resolution, color, pixel placement, palette, and grid alignment. It deliberately
ships no art corpus. For this project it belongs in the asset-QA pipeline after
we have an approved native sprite set; it can catch blur, resampling, JPEG
damage, and broken pixel grids, but it cannot tell us whether animation,
silhouette, staging, or baseball feel is 9/10.

The prototype therefore preserves a native `256×224` gameplay canvas, uses
nearest-neighbour browser scaling, and reduced the generated stadium source to
a 32-color indexed PNG. Pixel-Bench `0.1.0` was installed in an isolated scratch
environment and its `validate` command reported that the final stadium asset
looks like native 1× pixel art. A production sprite corpus should likewise be
validated at 1× and then exercised through the full distortion suite before
release packaging.

## MCP comparison

| Engine | MCP path | Fit for this game | Decision |
| --- | --- | --- | --- |
| Unity 2D | [Coplay Unity MCP](https://github.com/CoplayDev/unity-mcp) | Mature editor automation, sprite/animation workflow, screenshots and play-mode iteration; locally installed | **Production recommendation** |
| Godot 4 | [Godot MCP Native](https://godotengine.org/asset-library/asset/5125) or community Godot MCP | Excellent native 2D and open-source stack, but the MCP ecosystem is newer and not installed locally | Strong alternate |
| Unreal 5.8 | [Epic Unreal MCP](https://dev.epicgames.com/documentation/unreal-engine/unreal-mcp-in-unreal-editor) | Official but experimental/incomplete; serial tool execution; excessive 3D/editor weight for a 2D sports game | Do not use for this title |
| Browser Canvas 2D | Code and browser inspection rather than an engine MCP | Fastest playable proof, exact 256×224 pixels, instant desktop/mobile QA | **Prototype choice** |

## Art provenance

The stadium source was generated specifically for this prototype with the
built-in image-generation tool, then resized and palette-reduced locally. The
batter and pitcher sprite art, ball, HUD, effects, and every animation frame
are original. No ROM, screenshot, commercial sprite, logo, player likeness,
audio, or source code was used.
