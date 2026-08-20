<!-- PROTOTYPE - NOT FOR PRODUCTION
Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
Date: 2026-08-20 -->

## Prototype Report: Diamond Dynasty '94

### Hypothesis

An original native-resolution browser game can deliver a satisfying at-bat,
clear pitch reading, expressive batter and pitcher animation, and premium
16-bit presentation without reusing protected art, code, audio, branding, or
player likenesses from commercial baseball games.

### Approach

Built a dependency-free Canvas 2D slice at a logical `256×224` resolution. The
slice combines a 32-color original stadium background with native-grid,
scanline-rasterized batter, pitcher, and catcher art plus the ball, HUD, hit
effects, and synthesized audio. Gameplay uses a
timing-plus-location contact model, three differentiated pitches, count and out
rules, base advancement, scoring, innings, keyboard controls, and touch controls.

Shortcuts: one fictional team, one stadium, no fielding camera, no roster or
substitution model, synthesized rather than sampled audio, and original
procedural sprite drawings rather than a production sprite atlas.

### Result

The at-bat loop is immediately readable. Pitch depth is communicated by ball
growth and perspective travel; curve and changeup trajectories differ from the
four-seam; the contact cursor makes plate coverage intentional; and timing or
location misses produce distinct feedback. Batter and pitcher silhouettes stay
legible over the field at native resolution. Desktop and `390×844` mobile QA
both preserve the 8:7 game screen, usable controls, and scoreboard without
horizontal overflow.

The rebuilt character set scores 9.1/10 on the internal detail rubric in
`CHARACTER_ART_COMPARISON.md`: comparable native scale, articulated anatomy,
multi-ramp shading, detailed fictional uniforms and equipment, and seven-state
delivery and swing animation. This is not an external review score. The
prototype should not be represented as a 9/10 complete game against a finished
SNES title: fielding, defence, team depth, broadcast transitions, and a complete
game loop remain.

### Metrics

- Native gameplay canvas: `256×224`
- Final background asset: indexed PNG, 32-color target, 20 KB
- Pixel-Bench 0.1.0 validation: `all images look like native 1x pixel art`
- Automated gameplay and animation model: 13/13 tests passed
- Desktop rendered canvas observed: `752×656`, no horizontal overflow
- Mobile QA viewport: `390×844`, no console errors or warnings
- Pitch repertoire: 3 pitches, 79–95 MPH, distinct duration and break
- Batter animation: 7 frame states; pitcher animation: 7 frame states
- Character-detail rubric: 9.1/10 internal target score; no external rating yet
- Outcome set: ball, called strike, swinging strike, foul, out, single, double,
  triple, home run, walk, strikeout, inning turnover
- External player rating: not yet collected

### Recommendation: PROCEED

The slice validates the central interaction and visual direction. Rebuild the
approved mechanics as a Unity 2D vertical slice using Coplay Unity MCP, with a
real sprite atlas and animation controller. Keep the browser prototype as the
reference for cadence and visual staging; do not migrate its throwaway code
directly into production.

### If Proceeding

- Re-author batter, pitcher, catcher, umpire, and fielders as native sprite
  sheets with 6–10 frames per major action and handedness variants.
- Add a fielding camera, defensive AI, throwing, runner decisions, errors, and
  tag/force-out rules.
- Add controller mapping, difficulty levels, batting practice, pause, settings,
  and reduced-flash accessibility options.
- Build a fictional roster and venue data model; keep all identity and audio
  original.
- Run the complete production sprite corpus through Pixel-Bench's distortion
  suite and add pixel-grid checks to asset import CI.
- Target a 3–5 minute replayable vertical slice before expanding to seasons,
  management, or multiplayer.

### Lessons Learned

- Pixel-Bench is useful for grid and reconstruction integrity, not aesthetic or
  animation scoring.
- A native 256×224 canvas enforces strong silhouettes and creates convincing
  console-era density when browser scaling remains nearest-neighbour.
- Pitch readability needs simultaneous depth, speed, break, and ball-size cues;
  speed alone is not enough.
- A 9/10 visual target is credible for a focused at-bat, but full-title quality
  depends on fielding, transitions, content breadth, and external playtesting.
