// PROTOTYPE - NOT FOR PRODUCTION
// Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
// Date: 2026-08-20

export const STRIKE_ZONE = Object.freeze({ left: 109, right: 147, top: 140, bottom: 178 });
export const CONTACT_PROGRESS = 0.88;

export const PITCHES = Object.freeze([
  { name: "FOUR-SEAM", speed: 95, duration: 760, breakX: 1, breakY: -1, color: "#fff6cf" },
  { name: "CURVEBALL", speed: 79, duration: 1020, breakX: -17, breakY: 10, color: "#52e0d0" },
  { name: "CHANGEUP", speed: 84, duration: 930, breakX: 5, breakY: 5, color: "#ffc34d" },
]);

const clamp = (value, min, max) => Math.max(min, Math.min(max, value));

export function createGameState() {
  return {
    inning: 1,
    outs: 0,
    balls: 0,
    strikes: 0,
    runs: 0,
    hits: 0,
    bases: [false, false, false],
    aimX: 128,
    aimY: 159,
    phase: "ready",
    pitch: null,
    result: "PRESS ENTER TO PITCH",
    lastContact: null,
  };
}

export function moveAim(state, dx, dy) {
  return {
    ...state,
    aimX: clamp(state.aimX + dx, STRIKE_ZONE.left - 10, STRIKE_ZONE.right + 10),
    aimY: clamp(state.aimY + dy, STRIKE_ZONE.top - 10, STRIKE_ZONE.bottom + 10),
  };
}

export function createPitch(now, random = Math.random) {
  const type = PITCHES[Math.floor(random() * PITCHES.length) % PITCHES.length];
  const isTemptingBall = random() < 0.27;
  const edge = Math.floor(random() * 4);
  let targetX = STRIKE_ZONE.left + 4 + random() * (STRIKE_ZONE.right - STRIKE_ZONE.left - 8);
  let targetY = STRIKE_ZONE.top + 4 + random() * (STRIKE_ZONE.bottom - STRIKE_ZONE.top - 8);

  if (isTemptingBall) {
    if (edge === 0) targetX = STRIKE_ZONE.left - 7 - random() * 8;
    if (edge === 1) targetX = STRIKE_ZONE.right + 7 + random() * 8;
    if (edge === 2) targetY = STRIKE_ZONE.top - 6 - random() * 7;
    if (edge === 3) targetY = STRIKE_ZONE.bottom + 6 + random() * 8;
  }

  return { ...type, start: now, targetX, targetY };
}

export function beginPitch(state, now, random = Math.random) {
  if (state.phase !== "ready") return state;
  const pitch = createPitch(now, random);
  return { ...state, phase: "pitching", pitch, result: `${pitch.name}  ${pitch.speed} MPH`, lastContact: null };
}

export function pitchProgress(pitch, now) {
  return clamp((now - pitch.start) / pitch.duration, 0, 1);
}

export function samplePitch(pitch, now) {
  const progress = pitchProgress(pitch, now);
  const depth = progress * progress * (3 - 2 * progress);
  const bend = Math.sin(progress * Math.PI) * pitch.breakX;
  const drop = Math.pow(progress, 3) * pitch.breakY;
  return {
    progress,
    x: 128 + (pitch.targetX - 128) * depth + bend,
    y: 111 + (pitch.targetY - 111) * depth + drop,
    radius: 1 + Math.floor(progress * 3.2),
  };
}

export function isStrikeLocation(x, y) {
  return x >= STRIKE_ZONE.left && x <= STRIKE_ZONE.right && y >= STRIKE_ZONE.top && y <= STRIKE_ZONE.bottom;
}

function clearCount(state) {
  return { ...state, balls: 0, strikes: 0 };
}

function finishHalfInning(state) {
  if (state.outs < 3) return state;
  return { ...state, inning: state.inning + 1, outs: 0, bases: [false, false, false], result: "SIDE RETIRED — NEXT INNING" };
}

function addOut(state, label) {
  return finishHalfInning({ ...clearCount(state), outs: state.outs + 1, result: label, phase: "resolved" });
}

function advanceRunners(state, basesEarned) {
  if (basesEarned === 4) {
    const scored = state.bases.filter(Boolean).length + 1;
    return { ...state, runs: state.runs + scored, bases: [false, false, false] };
  }

  const nextBases = [false, false, false];
  let scored = 0;
  state.bases.forEach((occupied, index) => {
    if (!occupied) return;
    const destination = index + basesEarned;
    if (destination >= 3) scored += 1;
    else nextBases[destination] = true;
  });
  if (basesEarned >= 3) scored += 1;
  else nextBases[basesEarned - 1] = true;
  return { ...state, runs: state.runs + scored, bases: nextBases };
}

function awardWalk(state) {
  const [first, second, third] = state.bases;
  const forcedRun = first && second && third ? 1 : 0;
  return {
    ...state,
    runs: state.runs + forcedRun,
    bases: [true, second || first, third || (first && second)],
  };
}

export function applyOutcome(state, outcome) {
  if (outcome.kind === "strike") {
    if (state.strikes >= 2) return addOut(state, outcome.label ?? "STRIKE THREE — DOWN ON K'S");
    return { ...state, strikes: state.strikes + 1, result: outcome.label ?? "STRIKE", phase: "resolved" };
  }

  if (outcome.kind === "ball") {
    if (state.balls >= 3) {
      const walked = awardWalk(clearCount(state));
      return { ...walked, result: "BALL FOUR — TAKE YOUR BASE", phase: "resolved" };
    }
    return { ...state, balls: state.balls + 1, result: outcome.label ?? "BALL", phase: "resolved" };
  }

  if (outcome.kind === "foul") {
    return { ...state, strikes: Math.min(2, state.strikes + 1), result: outcome.label ?? "FOUL BALL", phase: "resolved" };
  }

  if (outcome.kind === "out") return addOut(state, outcome.label ?? "FLY OUT");

  const basesEarned = { single: 1, double: 2, triple: 3, homer: 4 }[outcome.kind];
  if (basesEarned) {
    const advanced = advanceRunners(clearCount(state), basesEarned);
    return {
      ...advanced,
      hits: state.hits + 1,
      result: outcome.label ?? outcome.kind.toUpperCase(),
      phase: "resolved",
      lastContact: outcome,
    };
  }

  return state;
}

export function resolveTakenPitch(state, now) {
  if (state.phase !== "pitching" || pitchProgress(state.pitch, now) < 1) return state;
  const strike = isStrikeLocation(state.pitch.targetX, state.pitch.targetY + state.pitch.breakY);
  return applyOutcome(state, { kind: strike ? "strike" : "ball", label: strike ? "CALLED STRIKE" : "BALL — GOOD EYE" });
}

export function resolveSwing(state, now, random = Math.random) {
  if (state.phase !== "pitching") return state;
  const ball = samplePitch(state.pitch, now);
  const timingError = Math.abs(ball.progress - CONTACT_PROGRESS);
  const spatialError = Math.hypot(state.aimX - ball.x, state.aimY - ball.y);

  if (timingError > 0.23 || spatialError > 25) {
    return applyOutcome(state, { kind: "strike", label: timingError > 0.23 ? "SWING AND A MISS — TIMING" : "SWING AND A MISS — CHASED" });
  }

  const timing = clamp(1 - timingError / 0.23, 0, 1);
  const barrel = clamp(1 - spatialError / 25, 0, 1);
  const quality = timing * 0.58 + barrel * 0.42;
  const spray = clamp((state.aimX - ball.x) * 2.2 + (ball.progress - CONTACT_PROGRESS) * 210, -46, 46);
  const contact = { quality, spray, timing, barrel };

  if (quality < 0.34) return applyOutcome(state, { kind: "foul", label: "FOULED IT BACK", ...contact });
  if (quality < 0.49) return random() < 0.62
    ? applyOutcome(state, { kind: "out", label: "ROLLER TO SHORT — OUT", ...contact })
    : applyOutcome(state, { kind: "single", label: "CHOPPED THROUGH THE HOLE — SINGLE", ...contact });
  if (quality < 0.68) return random() < 0.28
    ? applyOutcome(state, { kind: "out", label: "LINED RIGHT AT THE FIELDER", ...contact })
    : applyOutcome(state, { kind: "single", label: "SOLID LINE DRIVE — SINGLE", ...contact });
  if (quality < 0.84) return applyOutcome(state, { kind: "double", label: "DRIVEN INTO THE GAP — DOUBLE", ...contact });
  if (quality < 0.94) return applyOutcome(state, { kind: "triple", label: "OFF THE WALL — THREE BASES", ...contact });
  return applyOutcome(state, { kind: "homer", label: "ABSOLUTELY CRUSHED — HOME RUN!", ...contact });
}

export function readyNextPitch(state) {
  if (state.phase !== "resolved") return state;
  return { ...state, phase: "ready", pitch: null, result: "PRESS ENTER TO PITCH", lastContact: null };
}
