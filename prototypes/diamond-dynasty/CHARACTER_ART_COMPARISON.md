<!-- PROTOTYPE - NOT FOR PRODUCTION -->

# Character art comparison

## Outcome

The rejected procedural batter and pitcher have been replaced by sixteen
original alpha sprites: nine depth-foreshortened three-quarter batting frames
and seven pitching frames.
Each frame is a curated native-scale reduction of the original concept sheets
in `art/concepts/`, built reproducibly by
`tools/build-player-sprites.sh`. The figures now use coherent full-body
silhouettes, shaped shoulders and joints, readable hands, thigh/knee/calf
volume, uniform folds, equipment, and frame-specific foreshortening. No sprite,
palette, logo, uniform, name, or player likeness was extracted from a
commercial game.

![Side-by-side camera comparison](screenshots/character-comparison-v3.png)

The left side is a reference-only gameplay capture from *Ken Griffey Jr.
Presents Major League Baseball* (1994). It is included only for visual critique
and is not loaded by the prototype. Source:
[Retro Gaming Stores screenshot](https://www.retrogamingstores.com/image/cache/data/snes/ken_griffey_jr_mlb_screenshot_1-800x800.png).
[MobyGames' screenshot index](https://www.mobygames.com/game/26316/ken-griffey-jr-presents-major-league-baseball/screenshots/)
independently identifies the title and its native SNES screenshots. The revised
stance also uses the ready-pose sequence around `2:00` in
[10min Gameplay's footage](https://www.youtube.com/watch?v=Fs00R8iX948&t=120s)
as motion-composition evidence: the near/far legs overlap in screen space
instead of forming a broad horizontal silhouette.

## Animation evidence

![Nine camera-facing batter and seven pitcher animation states](screenshots/character-animation-sheet-v3.png)

![Looping batter animation at corrected gameplay placement](screenshots/batter-animation.gif)

`art-review.html` provides synchronized field and 4× close views, game-speed
and slow playback, pause/step controls, and direct access to each labeled pose.

The first pass's self-assigned `9.1/10` score is withdrawn. Human review
correctly identified that the torso and equipment had more detail than the
constant-width arms and legs. This revision uses visual evidence and concrete
checks rather than assigning itself a replacement score.

| Acceptance check | Evidence |
|---|---|
| Joint anatomy | Separate shoulder, elbow, wrist, hip, knee, calf, and ankle silhouettes remain readable through all sixteen poses. |
| Limb volume | Arms and legs use highlight, base, shadow, and crease clusters rather than constant-width procedural segments. |
| Frame-specific motion | The batter's overlapped near/far legs establish depth toward the pitcher; weight transfer, stride, bat extension, and follow-through alter the complete silhouette. |
| Uniform and equipment | Fictional cream, navy, and coral uniform; belt, cuffs, socks, cleats, batting gloves, helmet, bat, ball, and glove. |
| Pixel integrity | Sixteen alpha PNGs are fixed at `120×112` or `76×58`, rendered on integer coordinates with smoothing disabled; Pixel-Bench 0.1.0 reports all sixteen as native 1x pixel art. |
| Originality | Original AI-assisted concept and fictional character; no traced or extracted commercial sprite pixels. |

The reference uses licensed team presentation and dense blue pinstripes;
Diamond Dynasty uses a fictional navy, cream, and coral identity with broader
shadow clusters. The intended comparison is anatomical detail density and
animation readability, not a trace or one-to-one replica.

## Animation inventory

- Batter: ready, load, stride, plant, swing, contact, extension, high finish,
  controlled reset, with the high finish held before reset
- Pitcher: set, leg lift, balance, stride, release, follow-through, recover
- Gameplay staging: a large, depth-foreshortened three-quarter batter stands
  beside the left batter's box with his near/far legs overlapping toward the
  smaller downfield pitcher, then carries the bat through the plate into a held
  finish; the catcher is omitted

## Concept-art pass

The built-in image generation tool produced
`art/concepts/original-batter-depth-stance-concept-v7.png` using the existing
character sheet as its style and identity reference. The selected concept was
chroma-keyed, cropped, nearest-neighbour reduced, and curated into the sixteen
runtime PNGs in `art/sprites/`.

Final prompt, condensed only for line wrapping:

> Rebuild the same fictional batter in nine depth-foreshortened three-quarter
> poses. In ready and load, overlap the near and far legs with a narrow diagonal
> foot separation so the body aims toward the downfield pitcher rather than
> broadside; carry that perspective through stride, contact, extension, a high
> follow-through, and reset. Preserve the cream, navy, and coral identity, hard
> 16-bit pixel clusters, 3x3 magenta-key sheet, and uncropped full-body poses;
> no real likeness, copied commercial sprite, logo, text, catcher, or field.
