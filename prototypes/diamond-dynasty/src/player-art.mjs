// Original character art for Diamond Dynasty '94.
// The runtime prefers native alpha sprites and retains procedural art only as
// a load-failure fallback; no commercial sprite, likeness, or uniform is used.

export const PLAYER_PALETTE = Object.freeze({
  outline: "#07101f",
  outlineSoft: "#18263a",
  creamLight: "#fff4cf",
  cream: "#eadfb9",
  creamShadow: "#a99d80",
  creamDeep: "#6f6b64",
  navyLight: "#31516d",
  navy: "#102e4a",
  navyDeep: "#07192d",
  coralLight: "#f27a58",
  coral: "#c83d43",
  skinLight: "#efb07a",
  skin: "#c77a55",
  skinShadow: "#844535",
  skinDeep: "#512b29",
  leatherLight: "#bc7842",
  leather: "#7c442c",
  leatherDeep: "#46251e",
  woodLight: "#f1c06a",
  wood: "#c58a43",
  woodDeep: "#754527",
  white: "#fffbed",
  shadow: "#2d211c",
});

const P = PLAYER_PALETTE;

export const BATTER_SPRITE_PATHS = Object.freeze(
  Array.from({ length: 9 }, (_, frame) => `./art/sprites/batter-${frame}.png`),
);

export const PITCHER_SPRITE_PATHS = Object.freeze(
  Array.from({ length: 7 }, (_, frame) => `./art/sprites/pitcher-${frame}.png`),
);

function pixel(ctx, color, x, y, width = 1, height = 1) {
  ctx.fillStyle = color;
  ctx.fillRect(Math.round(x), Math.round(y), Math.round(width), Math.round(height));
}

// Canvas polygon edges are antialiased. This scanline fill keeps every edge on
// a hard one-pixel stair step so the characters remain true native pixel art.
function polygon(ctx, color, points) {
  const minY = Math.ceil(Math.min(...points.map((point) => point[1])));
  const maxY = Math.floor(Math.max(...points.map((point) => point[1])));
  ctx.fillStyle = color;

  for (let y = minY; y <= maxY; y += 1) {
    const sampleY = y + 0.5;
    const intersections = [];
    for (let index = 0; index < points.length; index += 1) {
      const a = points[index];
      const b = points[(index + 1) % points.length];
      if ((a[1] <= sampleY && b[1] > sampleY) || (b[1] <= sampleY && a[1] > sampleY)) {
        intersections.push(a[0] + ((sampleY - a[1]) / (b[1] - a[1])) * (b[0] - a[0]));
      }
    }
    intersections.sort((a, b) => a - b);
    for (let index = 0; index < intersections.length; index += 2) {
      const start = Math.ceil(intersections[index] - 0.5);
      const end = Math.floor(intersections[index + 1] - 0.5);
      if (end >= start) ctx.fillRect(start, y, end - start + 1, 1);
    }
  }
}

function pixelLine(ctx, color, from, to, thickness = 1) {
  let x0 = Math.round(from[0]);
  let y0 = Math.round(from[1]);
  const x1 = Math.round(to[0]);
  const y1 = Math.round(to[1]);
  const dx = Math.abs(x1 - x0);
  const sx = x0 < x1 ? 1 : -1;
  const dy = -Math.abs(y1 - y0);
  const sy = y0 < y1 ? 1 : -1;
  let error = dx + dy;
  const offset = Math.floor(thickness / 2);

  while (true) {
    pixel(ctx, color, x0 - offset, y0 - offset, thickness, thickness);
    if (x0 === x1 && y0 === y1) break;
    const doubled = error * 2;
    if (doubled >= dy) { error += dy; x0 += sx; }
    if (doubled <= dx) { error += dx; y0 += sy; }
  }
}

function segmentGeometry(from, to, startRadius, endRadius) {
  const dx = to[0] - from[0];
  const dy = to[1] - from[1];
  const length = Math.max(1, Math.hypot(dx, dy));
  const normal = [-dy / length, dx / length];
  return {
    normal,
    points: [
      [from[0] + normal[0] * startRadius, from[1] + normal[1] * startRadius],
      [to[0] + normal[0] * endRadius, to[1] + normal[1] * endRadius],
      [to[0] - normal[0] * endRadius, to[1] - normal[1] * endRadius],
      [from[0] - normal[0] * startRadius, from[1] - normal[1] * startRadius],
    ],
  };
}

function drawTaperedSegment(ctx, from, to, startRadius, endRadius, colors) {
  const outline = segmentGeometry(from, to, startRadius + 1.6, endRadius + 1.35);
  const body = segmentGeometry(from, to, startRadius, endRadius);
  polygon(ctx, P.outline, outline.points);
  polygon(ctx, colors.base, body.points);

  // A broad lower-right shadow plane and a broken upper-left highlight make the
  // segment read as muscle/cloth volume instead of a constant-width stick.
  const [nx, ny] = body.normal;
  polygon(ctx, colors.shadow, [
    [from[0] + nx * startRadius * 0.28, from[1] + ny * startRadius * 0.28],
    [to[0] + nx * endRadius * 0.28, to[1] + ny * endRadius * 0.28],
    [to[0] + nx * endRadius, to[1] + ny * endRadius],
    [from[0] + nx * startRadius, from[1] + ny * startRadius],
  ]);
  pixelLine(
    ctx,
    colors.highlight,
    [from[0] - nx * startRadius * 0.48, from[1] - ny * startRadius * 0.48],
    [to[0] - nx * endRadius * 0.48, to[1] - ny * endRadius * 0.48],
    1,
  );
}

function drawJoint(ctx, at, radius, colors) {
  const [x, y] = at;
  polygon(ctx, P.outline, [
    [x - radius, y - radius * 0.45], [x - radius * 0.45, y - radius],
    [x + radius * 0.45, y - radius], [x + radius, y - radius * 0.35],
    [x + radius, y + radius * 0.35], [x + radius * 0.35, y + radius],
    [x - radius * 0.45, y + radius], [x - radius, y + radius * 0.35],
  ]);
  polygon(ctx, colors.base, [
    [x - radius + 1, y - 1], [x - 1, y - radius + 1],
    [x + radius - 1, y - 1], [x + 1, y + radius - 1],
    [x - radius + 1, y + 1],
  ]);
  pixel(ctx, colors.highlight, x - 1, y - radius + 1, 2, 1);
  pixel(ctx, colors.shadow, x - radius + 1, y + 1, Math.max(2, radius), 2);
  pixel(ctx, P.outlineSoft, x - 1, y + radius - 1, 2, 1);
}

function shadedLimb(ctx, from, joint, to, width, colors) {
  const upperRadius = width * 0.58;
  const jointRadius = width * 0.52;
  const lowerRadius = width * 0.43;
  const endRadius = width * 0.31;
  drawTaperedSegment(ctx, from, joint, upperRadius, jointRadius, colors);
  drawTaperedSegment(ctx, joint, to, jointRadius, endRadius, colors);
  drawJoint(ctx, joint, Math.max(2.5, jointRadius), colors);

  // Joint-side creases are deliberately short, broken clusters like the best
  // 16-bit sports sprites; they preserve articulation at animation speed.
  const lower = segmentGeometry(joint, to, jointRadius, endRadius);
  const [nx, ny] = lower.normal;
  pixelLine(
    ctx,
    P.outlineSoft,
    [joint[0] - nx * jointRadius * 0.55, joint[1] - ny * jointRadius * 0.55],
    [joint[0] + nx * jointRadius * 0.25, joint[1] + ny * jointRadius * 0.25],
    1,
  );
}

function drawSleevedArm(ctx, shoulder, elbow, hand, width, sleeveColors, cuffColor = P.coral) {
  const sleeveRadius = width * 0.58;
  const elbowRadius = width * 0.46;
  const wristRadius = width * 0.28;
  const skinColors = {
    base: P.skin,
    shadow: P.skinShadow,
    highlight: P.skinLight,
  };
  drawTaperedSegment(ctx, shoulder, elbow, sleeveRadius, elbowRadius, sleeveColors);
  drawTaperedSegment(ctx, elbow, hand, elbowRadius, wristRadius, skinColors);
  drawJoint(ctx, elbow, Math.max(2.5, elbowRadius), skinColors);

  const upper = segmentGeometry(shoulder, elbow, sleeveRadius, elbowRadius);
  const [nx, ny] = upper.normal;
  pixelLine(
    ctx,
    cuffColor,
    [elbow[0] - nx * elbowRadius * 0.7, elbow[1] - ny * elbowRadius * 0.7],
    [elbow[0] + nx * elbowRadius * 0.7, elbow[1] + ny * elbowRadius * 0.7],
    1,
  );
  pixel(ctx, P.skinDeep, elbow[0] - 1, elbow[1] + 1, 2, 1);
}

function drawCleat(ctx, ankle, facing = 1, scale = 1) {
  const x = ankle[0];
  const y = ankle[1];
  const direction = facing >= 0 ? 1 : -1;
  polygon(ctx, P.outline, [
    [x - 3 * scale, y - 2 * scale], [x + direction * 3 * scale, y - 2 * scale],
    [x + direction * 8 * scale, y + scale], [x + direction * 8 * scale, y + 4 * scale],
    [x - direction * 4 * scale, y + 4 * scale], [x - 4 * scale, y + scale],
  ]);
  pixel(ctx, P.navy, x - 2 * scale, y - scale, 6 * scale, 4 * scale);
  pixel(ctx, P.navyLight, x + direction * 2 * scale, y, 4 * scale, scale);
  pixel(ctx, P.white, x + direction * 3 * scale, y + 2 * scale, 4 * scale, scale);
  pixel(ctx, P.outlineSoft, x - direction * 3 * scale, y + 4 * scale, 2 * scale, scale);
  pixel(ctx, P.outlineSoft, x + direction * 4 * scale, y + 4 * scale, 2 * scale, scale);
}

function drawGlove(ctx, hand, scale = 1) {
  const [x, y] = hand;
  polygon(ctx, P.leatherDeep, [
    [x - 5 * scale, y - 4 * scale], [x + 2 * scale, y - 5 * scale],
    [x + 5 * scale, y - 2 * scale], [x + 4 * scale, y + 4 * scale],
    [x, y + 6 * scale], [x - 5 * scale, y + 3 * scale],
  ]);
  polygon(ctx, P.leather, [
    [x - 3 * scale, y - 3 * scale], [x + scale, y - 4 * scale],
    [x + 3 * scale, y - scale], [x + 2 * scale, y + 3 * scale],
    [x, y + 4 * scale], [x - 3 * scale, y + 2 * scale],
  ]);
  pixelLine(ctx, P.leatherLight, [x - 2 * scale, y - 2 * scale], [x + scale, y + scale], scale);
  pixelLine(ctx, P.leatherDeep, [x - scale, y + 2 * scale], [x + 2 * scale, y], scale);
}

function drawBattingHands(ctx, backHand, frontHand) {
  const center = [
    Math.round((backHand[0] + frontHand[0]) / 2),
    Math.round((backHand[1] + frontHand[1]) / 2),
  ];
  polygon(ctx, P.outline, [
    [center[0] - 6, center[1] - 4], [center[0] + 3, center[1] - 5],
    [center[0] + 7, center[1] - 1], [center[0] + 5, center[1] + 5],
    [center[0] - 5, center[1] + 4], [center[0] - 7, center[1]],
  ]);
  polygon(ctx, P.navy, [
    [center[0] - 4, center[1] - 3], [center[0] + 2, center[1] - 3],
    [center[0] + 5, center[1] - 1], [center[0] + 3, center[1] + 3],
    [center[0] - 4, center[1] + 2],
  ]);
  pixel(ctx, P.white, center[0] - 4, center[1] - 2, 7, 2);
  pixel(ctx, P.coralLight, center[0] - 2, center[1] + 1, 5, 1);
  pixel(ctx, P.outlineSoft, center[0] + 2, center[1] - 2, 1, 5);
  pixel(ctx, P.outlineSoft, center[0] - 1, center[1] - 2, 1, 4);
  pixel(ctx, P.coral, center[0] - 6, center[1] - 1, 2, 3);
}

function drawHead(ctx, x, y, scale = 1, facing = -1) {
  const direction = facing >= 0 ? 1 : -1;
  polygon(ctx, P.outline, [
    [x - 5 * scale, y - 8 * scale], [x + 4 * scale, y - 8 * scale],
    [x + 6 * scale * direction, y - 3 * scale], [x + 4 * scale, y + 5 * scale],
    [x - 2 * scale, y + 7 * scale], [x - 6 * scale, y + 2 * scale],
  ]);
  polygon(ctx, P.skinShadow, [
    [x - 4 * scale, y - 5 * scale], [x + 3 * scale, y - 5 * scale],
    [x + 4 * scale * direction, y - 2 * scale], [x + 3 * scale, y + 4 * scale],
    [x - scale, y + 5 * scale], [x - 4 * scale, y + scale],
  ]);
  pixel(ctx, P.skin, x - 2 * scale, y - 4 * scale, 5 * scale, 7 * scale);
  pixel(ctx, P.skinLight, x - scale, y - 3 * scale, 2 * scale, 3 * scale);
  pixel(ctx, P.skinDeep, x + direction * 3 * scale, y - scale, 2 * scale, scale);
  pixel(ctx, P.outline, x + direction * 2 * scale, y - 3 * scale, scale, scale);
  pixel(ctx, P.skinDeep, x - scale, y + 4 * scale, 4 * scale, scale);

  polygon(ctx, P.outline, [
    [x - 6 * scale, y - 9 * scale], [x + 4 * scale, y - 9 * scale],
    [x + 6 * scale, y - 6 * scale], [x + 5 * scale, y - 3 * scale],
    [x - 5 * scale, y - 3 * scale],
  ]);
  pixel(ctx, P.navy, x - 5 * scale, y - 8 * scale, 9 * scale, 4 * scale);
  pixel(ctx, P.navyLight, x - 3 * scale, y - 7 * scale, 4 * scale, scale);
  pixel(ctx, P.coral, x - scale, y - 6 * scale, 2 * scale, 2 * scale);
  pixel(ctx, P.outline, x + direction * 3 * scale, y - 4 * scale, 6 * scale, 2 * scale);
  pixel(ctx, P.navy, x + direction * 3 * scale, y - 5 * scale, 5 * scale, scale);
}

const BATTER_POSES = Object.freeze([
  { lean: 0, hip: 0, knees: [-12, 10], ankles: [-13, 13], backElbow: [-17, -48], backHand: [-13, -55], frontElbow: [-8, -45], frontHand: [-10, -54], bat: [6, -82] },
  { lean: -2, hip: -1, knees: [-13, 8], ankles: [-14, 12], backElbow: [-19, -50], backHand: [-13, -57], frontElbow: [-7, -47], frontHand: [-9, -56], bat: [3, -84] },
  { lean: -1, hip: -2, knees: [-13, 13], ankles: [-14, 17], backElbow: [-18, -46], backHand: [-10, -47], frontElbow: [-4, -44], frontHand: [-7, -47], bat: [-43, -54] },
  { lean: 1, hip: 1, knees: [-10, 15], ankles: [-12, 19], backElbow: [-10, -43], backHand: [0, -43], frontElbow: [1, -45], frontHand: [5, -44], bat: [47, -48] },
  { lean: 3, hip: 2, knees: [-8, 17], ankles: [-11, 21], backElbow: [1, -46], backHand: [9, -51], frontElbow: [7, -48], frontHand: [12, -53], bat: [39, -76] },
  { lean: 4, hip: 3, knees: [-7, 18], ankles: [-11, 22], backElbow: [4, -51], backHand: [11, -58], frontElbow: [9, -53], frontHand: [13, -60], bat: [27, -88] },
  { lean: 2, hip: 2, knees: [-8, 17], ankles: [-11, 21], backElbow: [1, -49], backHand: [8, -56], frontElbow: [7, -51], frontHand: [11, -58], bat: [22, -86] },
  { lean: 5, hip: 4, knees: [-6, 19], ankles: [-10, 23], backElbow: [7, -54], backHand: [13, -61], frontElbow: [11, -56], frontHand: [15, -63], bat: [14, -92] },
  { lean: 4, hip: 4, knees: [-6, 19], ankles: [-10, 23], backElbow: [6, -53], backHand: [12, -60], frontElbow: [10, -55], frontHand: [14, -62], bat: [8, -90] },
]);

export function batterFrameForElapsed(elapsed) {
  if (elapsed < 0) return 0;
  if (elapsed < 70) return 1;
  if (elapsed < 135) return 2;
  if (elapsed < 195) return 3;
  if (elapsed < 255) return 4;
  if (elapsed < 315) return 5;
  if (elapsed < 385) return 6;
  if (elapsed < 900) return 7;
  return 8;
}

export function batterFrameForSwing(startedAt, now) {
  return startedAt === null ? 0 : batterFrameForElapsed(now - startedAt);
}

function drawBatterLeg(ctx, hip, knee, ankle, isFront) {
  shadedLimb(ctx, hip, knee, ankle, 9, {
    base: P.cream,
    shadow: P.creamShadow,
    highlight: P.creamLight,
  });
  const outside = isFront ? 2 : -2;
  pixelLine(ctx, P.coral, [knee[0] + outside, knee[1] + 2], [ankle[0] + outside, ankle[1] - 4], 1);
  // Trouser tension above the knee plus a shadowed kneecap/calf break.
  pixelLine(ctx, P.creamDeep, [hip[0] + outside, hip[1] + 4], [knee[0], knee[1] - 3], 1);
  pixel(ctx, P.creamLight, knee[0] - 2, knee[1] - 4, 4, 2);
  pixel(ctx, P.creamDeep, knee[0] - 3, knee[1] + 2, 5, 2);
  pixel(ctx, P.creamLight, ankle[0] - outside, ankle[1] - 8, 2, 3);
  pixel(ctx, P.navy, ankle[0] - 3, ankle[1] - 5, 7, 5);
  pixel(ctx, P.coral, ankle[0] - 3, ankle[1] - 4, 7, 1);
  drawCleat(ctx, ankle, isFront ? 1 : -1);
}

// This is a right-handed hitter in a foreshortened home-plate-camera stance.
// Keep him in the left batter's box so the pitcher, plate, pitch path, stride,
// and barrel are all in front of him. Putting this art on the right side makes
// the swing read backwards even though the poses remain in chronological order.
export function drawBatter(ctx, frame, { x = 80, y = 202, sprite = null } = {}) {
  if (sprite?.complete && sprite.naturalWidth > 0) {
    pixel(ctx, P.shadow, x - 31, y + 2, 62, 4);
    pixel(ctx, P.outline, x - 25, y, 50, 2);
    ctx.drawImage(sprite, Math.round(x - 60), Math.round(y - 108));
    return;
  }

  const pose = BATTER_POSES[Math.max(0, Math.min(BATTER_POSES.length - 1, frame))];
  const hipCenter = [x + pose.hip, y - 29];
  const backHip = [hipCenter[0] - 5, hipCenter[1]];
  const frontHip = [hipCenter[0] + 5, hipCenter[1]];
  const backKnee = [x + pose.knees[0], y - 16];
  const frontKnee = [x + pose.knees[1], y - 15];
  const backAnkle = [x + pose.ankles[0], y - 5];
  const frontAnkle = [x + pose.ankles[1], y - 5];
  const shoulderBack = [x - 10 + pose.lean, y - 51];
  const shoulderFront = [x + 9 + pose.lean, y - 50];
  const backElbow = [x + pose.backElbow[0], y + pose.backElbow[1]];
  const backHand = [x + pose.backHand[0], y + pose.backHand[1]];
  const frontElbow = [x + pose.frontElbow[0], y + pose.frontElbow[1]];
  const frontHand = [x + pose.frontHand[0], y + pose.frontHand[1]];
  const batEnd = [x + pose.bat[0], y + pose.bat[1]];

  pixel(ctx, P.shadow, x - 23, y + 3, 45, 4);
  pixel(ctx, P.outline, x - 18, y + 1, 34, 2);

  // Bat sits behind the hands and body through the load, then comes forward.
  pixelLine(ctx, P.outline, frontHand, batEnd, 5);
  pixelLine(ctx, P.woodDeep, frontHand, batEnd, 3);
  pixelLine(ctx, P.wood, [frontHand[0], frontHand[1] - 1], [batEnd[0], batEnd[1] - 1], 2);
  pixelLine(ctx, P.woodLight, [frontHand[0] + 1, frontHand[1] - 1], [batEnd[0], batEnd[1] - 1], 1);
  pixel(ctx, P.outline, batEnd[0] - 2, batEnd[1] - 2, 4, 4);
  pixel(ctx, P.woodLight, batEnd[0] - 1, batEnd[1] - 1, 2, 2);

  drawBatterLeg(ctx, backHip, backKnee, backAnkle, false);
  drawBatterLeg(ctx, frontHip, frontKnee, frontAnkle, true);

  polygon(ctx, P.outline, [
    [x - 15 + pose.lean, y - 55], [x + 14 + pose.lean, y - 54],
    [x + 12 + pose.hip, y - 28], [x - 11 + pose.hip, y - 28],
  ]);
  polygon(ctx, P.cream, [
    [x - 12 + pose.lean, y - 52], [x + 11 + pose.lean, y - 51],
    [x + 9 + pose.hip, y - 31], [x - 8 + pose.hip, y - 31],
  ]);
  polygon(ctx, P.creamShadow, [
    [x - 11 + pose.lean, y - 51], [x - 4 + pose.lean, y - 51],
    [x - 3 + pose.hip, y - 32], [x - 8 + pose.hip, y - 31],
  ]);
  pixelLine(ctx, P.creamLight, [x + 7 + pose.lean, y - 49], [x + 6 + pose.hip, y - 34], 2);
  pixelLine(ctx, P.coral, [x - 9 + pose.lean, y - 49], [x - 6 + pose.hip, y - 34], 2);
  pixel(ctx, P.navy, x - 8 + pose.hip, y - 33, 17, 3);
  pixel(ctx, P.outline, x - 8 + pose.hip, y - 30, 17, 2);
  pixel(ctx, P.coralLight, x - 1 + pose.hip, y - 33, 3, 2);
  // Original wave crest: three tiny pixels, deliberately not a letter or logo.
  pixel(ctx, P.navy, x + 1 + pose.lean, y - 45, 6, 4);
  pixel(ctx, P.coral, x + 2 + pose.lean, y - 44, 2, 1);
  pixel(ctx, P.creamLight, x + 4 + pose.lean, y - 43, 2, 1);

  drawSleevedArm(ctx, shoulderBack, backElbow, backHand, 8, {
    base: P.navy, shadow: P.navyDeep, highlight: P.navyLight,
  });
  drawSleevedArm(ctx, shoulderFront, frontElbow, frontHand, 8, {
    base: P.cream, shadow: P.creamShadow, highlight: P.creamLight,
  }, P.navy);
  drawBattingHands(ctx, backHand, frontHand);

  drawHead(ctx, x + pose.lean + 1, y - 64, 1, 1);
  // Ear flap and jaw guard make the batting helmet read separately from a cap.
  pixel(ctx, P.navyDeep, x + pose.lean + 5, y - 64, 3, 8);
  pixel(ctx, P.navyLight, x + pose.lean + 5, y - 63, 1, 4);
  pixel(ctx, P.skinLight, x + pose.lean - 3, y - 62, 1, 3);
}

const PITCHER_POSES = Object.freeze([
  { body: [0, 0], head: [0, -34], throwElbow: [4, -24], throwHand: [1, -28], gloveElbow: [-5, -24], gloveHand: [-1, -28], backKnee: [-5, -8], backAnkle: [-7, 0], frontKnee: [6, -8], frontAnkle: [8, 0] },
  { body: [0, -1], head: [0, -35], throwElbow: [5, -26], throwHand: [1, -31], gloveElbow: [-5, -26], gloveHand: [-1, -31], backKnee: [-4, -9], backAnkle: [-6, 0], frontKnee: [8, -16], frontAnkle: [4, -20] },
  { body: [-1, -2], head: [-1, -36], throwElbow: [6, -25], throwHand: [1, -30], gloveElbow: [-7, -25], gloveHand: [-2, -30], backKnee: [-5, -10], backAnkle: [-8, 0], frontKnee: [9, -17], frontAnkle: [8, -9] },
  { body: [-1, 0], head: [-2, -34], throwElbow: [7, -28], throwHand: [10, -24], gloveElbow: [-9, -22], gloveHand: [-12, -18], backKnee: [-6, -7], backAnkle: [-10, 0], frontKnee: [8, -7], frontAnkle: [15, 0] },
  { body: [1, 1], head: [0, -32], throwElbow: [9, -23], throwHand: [15, -20], gloveElbow: [-7, -19], gloveHand: [-11, -17], backKnee: [-7, -6], backAnkle: [-12, 0], frontKnee: [9, -5], frontAnkle: [17, 0] },
  { body: [3, 3], head: [2, -29], throwElbow: [4, -17], throwHand: [-3, -13], gloveElbow: [-4, -16], gloveHand: [-7, -12], backKnee: [-4, -2], backAnkle: [-13, -1], frontKnee: [10, -5], frontAnkle: [17, 0] },
  { body: [1, 1], head: [1, -32], throwElbow: [5, -20], throwHand: [1, -16], gloveElbow: [-5, -20], gloveHand: [-8, -16], backKnee: [-5, -7], backAnkle: [-9, 0], frontKnee: [7, -7], frontAnkle: [12, 0] },
]);

export function pitcherFrameForProgress(progress) {
  if (progress < 0.12) return 0;
  if (progress < 0.27) return 1;
  if (progress < 0.43) return 2;
  if (progress < 0.59) return 3;
  if (progress < 0.72) return 4;
  if (progress < 0.88) return 5;
  return 6;
}

export function drawPitcher(ctx, frame, { x = 128, y = 123, sprite = null } = {}) {
  if (sprite?.complete && sprite.naturalWidth > 0) {
    pixel(ctx, P.shadow, x - 17, y + 3, 34, 3);
    pixel(ctx, P.outline, x - 13, y + 1, 26, 2);
    ctx.drawImage(sprite, Math.round(x - 38), Math.round(y - 58));
    return;
  }

  const pose = PITCHER_POSES[Math.max(0, Math.min(PITCHER_POSES.length - 1, frame))];
  const bodyX = x + pose.body[0];
  const bodyY = y + pose.body[1];
  const point = ([px, py]) => [x + px, y + py];
  const hip = [bodyX, bodyY - 12];
  const leftHip = [hip[0] - 4, hip[1]];
  const rightHip = [hip[0] + 4, hip[1]];
  const leftKnee = point(pose.backKnee);
  const leftAnkle = point(pose.backAnkle);
  const rightKnee = point(pose.frontKnee);
  const rightAnkle = point(pose.frontAnkle);

  pixel(ctx, P.shadow, x - 15, y + 3, 31, 3);
  pixel(ctx, P.outline, x - 11, y + 1, 23, 2);

  shadedLimb(ctx, leftHip, leftKnee, leftAnkle, 6, {
    base: P.creamShadow, shadow: P.creamDeep, highlight: P.cream,
  });
  shadedLimb(ctx, rightHip, rightKnee, rightAnkle, 6, {
    base: P.cream, shadow: P.creamShadow, highlight: P.creamLight,
  });
  pixelLine(ctx, P.coral, [rightKnee[0] + 1, rightKnee[1]], [rightAnkle[0] + 1, rightAnkle[1] - 2], 1);
  pixel(ctx, P.creamLight, rightKnee[0] - 2, rightKnee[1] - 3, 4, 2);
  pixel(ctx, P.creamDeep, leftKnee[0] - 2, leftKnee[1] + 1, 4, 2);
  drawCleat(ctx, leftAnkle, -1, 0.75);
  drawCleat(ctx, rightAnkle, 1, 0.75);

  polygon(ctx, P.outline, [
    [bodyX - 9, bodyY - 27], [bodyX + 9, bodyY - 26],
    [bodyX + 7, bodyY - 11], [bodyX - 7, bodyY - 11],
  ]);
  polygon(ctx, P.cream, [
    [bodyX - 7, bodyY - 25], [bodyX + 7, bodyY - 24],
    [bodyX + 5, bodyY - 13], [bodyX - 5, bodyY - 13],
  ]);
  polygon(ctx, P.creamShadow, [
    [bodyX - 7, bodyY - 25], [bodyX - 2, bodyY - 24],
    [bodyX - 1, bodyY - 13], [bodyX - 5, bodyY - 13],
  ]);
  pixelLine(ctx, P.coral, [bodyX - 5, bodyY - 23], [bodyX - 3, bodyY - 14], 1);
  pixel(ctx, P.navy, bodyX - 6, bodyY - 14, 12, 2);
  pixel(ctx, P.coralLight, bodyX - 1, bodyY - 14, 2, 1);
  pixel(ctx, P.navy, bodyX + 1, bodyY - 21, 4, 3);
  pixel(ctx, P.coral, bodyX + 2, bodyY - 20, 2, 1);

  const throwShoulder = [bodyX + 6, bodyY - 24];
  const gloveShoulder = [bodyX - 6, bodyY - 24];
  const throwElbow = point(pose.throwElbow);
  const throwHand = point(pose.throwHand);
  const gloveElbow = point(pose.gloveElbow);
  const gloveHand = point(pose.gloveHand);
  drawSleevedArm(ctx, throwShoulder, throwElbow, throwHand, 5, {
    base: P.cream, shadow: P.creamShadow, highlight: P.creamLight,
  });
  drawSleevedArm(ctx, gloveShoulder, gloveElbow, gloveHand, 5, {
    base: P.navy, shadow: P.navyDeep, highlight: P.navyLight,
  });
  drawGlove(ctx, gloveHand, 0.75);
  if (frame >= 3 && frame <= 4) {
    pixel(ctx, P.white, throwHand[0] - 1, throwHand[1] - 2, 3, 3);
    pixel(ctx, P.coral, throwHand[0], throwHand[1] - 1, 1, 2);
  }

  const [headX, headY] = point(pose.head);
  drawHead(ctx, headX, headY, 0.75, 1);
}
