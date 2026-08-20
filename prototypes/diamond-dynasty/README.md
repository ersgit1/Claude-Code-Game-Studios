<!-- PROTOTYPE - NOT FOR PRODUCTION
Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
Date: 2026-08-20 -->

# Diamond Dynasty '94

Status: concluded — recommendation: proceed to a production vertical slice

## Hypothesis

An original 256×224 browser prototype can capture the immediacy, readability,
animation density, and game-feel expected from an excellent 16-bit baseball
at-bat without copying any licensed team, player, sprite, screen, sound, code,
or protected expression from a commercial title.

## Run

From this directory:

```bash
python3 -m http.server 4173
```

Open `http://127.0.0.1:4173`.

Open `http://127.0.0.1:4173/art-review.html` for synchronized field and 4×
close views of the nine-frame batter animation, including game-speed, slow,
pause, step, and direct-pose controls.

Run the deterministic gameplay tests with:

```bash
node --test tests/*.test.mjs
```

## Controls

- Arrow keys: aim the contact cursor
- `Space` or `A`: swing
- `Enter`: deliver the next pitch
- Touch controls mirror the keyboard controls

## Prototype scope

- Three pitch types with speed and break differences
- Balls, strikes, walks, strikeouts, outs, innings, hits, runners, and scoring
- Timing-plus-location contact model with foul, out, single, double, triple,
  and home-run outcomes
- Nine-state original side/back three-quarter batter animation in a tight
  home-plate camera, with the right-facing hitter staged in the left batter's
  box so his stride and barrel travel through the incoming pitch, plus a
  seven-state pitcher delivery
- Dedicated batter animation review with synchronized field/close views and a
  reproducible looping GIF
- Original synthesized arcade sound cues
- Original fictional stadium art reduced to a 32-color, native 256×224 asset

## Findings

The core at-bat is readable and replayable on desktop and mobile, the original
sprite states communicate windup and a complete held follow-through, and the
deterministic model passes all eighteen gameplay, animation, integer-raster,
and sprite-asset tests. The revised character comparison and complete
sixteen-pose animation
sheet are in
`CHARACTER_ART_COMPARISON.md`. The earlier self-assigned art score was withdrawn
after human review; the images are now the acceptance evidence. This prototype
is not yet a complete game comparable feature-for-feature with a finished SNES
cartridge. See `REPORT.md` for the production recommendation.
