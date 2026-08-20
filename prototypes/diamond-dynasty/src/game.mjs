// PROTOTYPE - NOT FOR PRODUCTION
// Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
// Date: 2026-08-20

import {
  STRIKE_ZONE,
  beginPitch,
  createGameState,
  moveAim,
  pitchProgress,
  readyNextPitch,
  resolveSwing,
  resolveTakenPitch,
  samplePitch,
} from "./game-model.mjs";
import { BallparkAudio } from "./audio.mjs";
import {
  BATTER_SPRITE_PATHS,
  PITCHER_SPRITE_PATHS,
  batterFrameForElapsed,
  drawBatter as drawPlayerBatter,
  drawPitcher as drawPlayerPitcher,
  pitcherFrameForProgress,
} from "./player-art.mjs?v=sprite-art-v5";

const canvas = document.querySelector("#game");
const ctx = canvas.getContext("2d", { alpha: false });
ctx.imageSmoothingEnabled = false;

const background = new Image();
background.src = "./assets/stadium.png";
const loadSprite = (src) => {
  const image = new Image();
  image.src = src;
  return image;
};
const playerSprites = {
  batter: BATTER_SPRITE_PATHS.map(loadSprite),
  pitcher: PITCHER_SPRITE_PATHS.map(loadSprite),
};
const audio = new BallparkAudio();
let state = createGameState();
let started = false;
let swingStartedAt = -Infinity;
let resolvedAt = 0;
let flight = null;
let shakeUntil = 0;
let flashUntil = 0;
const held = new Set();

const COLORS = {
  cream: "#fff6cf", gold: "#ffc34d", orange: "#f06a32", red: "#be2947",
  navy: "#071328", ink: "#02050c", teal: "#0f8f8f", mint: "#52e0d0",
  jersey: "#e6e0cf", jerseyShadow: "#8d9bb0", skin: "#d59162", skinShadow: "#8f503f",
};

function rect(color, x, y, width, height) {
  ctx.fillStyle = color;
  ctx.fillRect(Math.round(x), Math.round(y), Math.round(width), Math.round(height));
}

function text(value, x, y, color = COLORS.cream, align = "left", size = 7) {
  ctx.save();
  ctx.font = `bold ${size}px monospace`;
  ctx.textAlign = align;
  ctx.textBaseline = "top";
  ctx.fillStyle = COLORS.ink;
  ctx.fillText(value, x + 1, y + 1);
  ctx.fillStyle = color;
  ctx.fillText(value, x, y);
  ctx.restore();
}

function drawHud() {
  rect(COLORS.navy, 0, 0, 256, 30);
  rect(COLORS.red, 0, 28, 256, 2);
  rect(COLORS.gold, 0, 30, 256, 1);
  text("HARBOR", 6, 4, COLORS.cream, "left", 8);
  text(String(state.runs).padStart(2, "0"), 54, 3, COLORS.gold, "left", 10);
  text(`INN ${state.inning}`, 92, 4, COLORS.cream, "left", 7);
  text(`H ${state.hits}`, 137, 4, COLORS.mint, "left", 7);
  text(`OUT ${state.outs}`, 171, 4, COLORS.cream, "left", 7);
  text(`B ${state.balls}  S ${state.strikes}`, 6, 17, COLORS.cream, "left", 7);

  const baseX = 219;
  [[0, -6], [-8, 2], [8, 2]].forEach(([dx, dy], index) => {
    ctx.save();
    ctx.translate(baseX + dx, 12 + dy);
    ctx.rotate(Math.PI / 4);
    rect(state.bases[index] ? COLORS.gold : "#38435a", -3, -3, 6, 6);
    ctx.restore();
  });
}

function drawStrikeZone() {
  ctx.save();
  ctx.globalAlpha = state.phase === "pitching" ? 0.38 : 0.2;
  ctx.strokeStyle = COLORS.cream;
  ctx.setLineDash([2, 2]);
  ctx.strokeRect(STRIKE_ZONE.left + 0.5, STRIKE_ZONE.top + 0.5, STRIKE_ZONE.right - STRIKE_ZONE.left, STRIKE_ZONE.bottom - STRIKE_ZONE.top);
  ctx.restore();
}

function drawAim() {
  const pulse = state.phase === "pitching" ? Math.sin(performance.now() / 90) > 0 : true;
  ctx.strokeStyle = pulse ? COLORS.mint : COLORS.cream;
  ctx.lineWidth = 1;
  ctx.strokeRect(state.aimX - 5.5, state.aimY - 5.5, 11, 11);
  rect(ctx.strokeStyle, state.aimX - 8, state.aimY, 4, 1);
  rect(ctx.strokeStyle, state.aimX + 5, state.aimY, 4, 1);
  rect(ctx.strokeStyle, state.aimX, state.aimY - 8, 1, 4);
  rect(ctx.strokeStyle, state.aimX, state.aimY + 5, 1, 4);
}

function drawPitcher(now) {
  const progress = state.phase === "pitching" ? pitchProgress(state.pitch, now) : 0;
  const frame = pitcherFrameForProgress(progress);
  drawPlayerPitcher(ctx, frame, { sprite: playerSprites.pitcher[frame] });
}

function drawBatter(now) {
  const elapsed = now - swingStartedAt;
  const frame = batterFrameForElapsed(elapsed);
  drawPlayerBatter(ctx, frame, { sprite: playerSprites.batter[frame] });
}

function drawBall(now) {
  if (state.phase !== "pitching") return;
  const ball = samplePitch(state.pitch, now);
  rect("#5c2b2b", ball.x - ball.radius, ball.y - ball.radius + 1, ball.radius * 2 + 1, ball.radius * 2 + 1);
  rect(COLORS.cream, ball.x - ball.radius, ball.y - ball.radius, ball.radius * 2, ball.radius * 2);
  if (ball.radius >= 3) { rect(COLORS.red, ball.x - 2, ball.y - 1, 1, 3); rect(COLORS.red, ball.x + 1, ball.y - 1, 1, 3); }
}

function drawFlight(now) {
  if (!flight) return;
  const t = Math.min(1, (now - flight.start) / 1100);
  const endX = 128 + flight.spray * 1.8;
  const endY = flight.kind === "homer" ? 43 : flight.kind === "triple" ? 57 : 73;
  const x = 128 + (endX - 128) * t;
  const y = 159 + (endY - 159) * t - Math.sin(t * Math.PI) * (45 + flight.quality * 35);
  rect(COLORS.ink, x - 2, y - 1, 4, 4);
  rect(COLORS.cream, x - 2, y - 2, 4, 4);
  if (flight.kind === "homer" && t > .55) {
    for (let i = 0; i < 14; i += 1) {
      const angle = (i / 14) * Math.PI * 2;
      const radius = (t - .55) * 70;
      rect(i % 2 ? COLORS.gold : COLORS.red, endX + Math.cos(angle) * radius, 55 + Math.sin(angle) * radius, 2, 2);
    }
  }
  if (t >= 1) flight = null;
}

function drawMessage() {
  rect("#071328dd", 18, 198, 220, 20);
  rect(COLORS.gold, 18, 198, 220, 1);
  text(state.result, 128, 204, state.result.includes("HOME RUN") ? COLORS.gold : COLORS.cream, "center", 7);
}

function draw(now) {
  ctx.save();
  if (now < shakeUntil) ctx.translate(Math.round(Math.random() * 4 - 2), Math.round(Math.random() * 3 - 1));
  if (background.complete) ctx.drawImage(background, 0, 0, 256, 224);
  else rect("#0a5d4f", 0, 0, 256, 224);
  drawHud();
  drawStrikeZone();
  drawPitcher(now);
  drawBatter(now);
  drawBall(now);
  drawFlight(now);
  drawAim();
  drawMessage();
  if (now < flashUntil) { ctx.globalAlpha = (flashUntil - now) / 180 * .35; rect("#fff6cf", 0, 0, 256, 224); }
  ctx.restore();
}

function handleOutcome(previous, now) {
  if (state.phase !== "resolved" || previous.phase === "resolved") return;
  resolvedAt = now;
  const contact = state.lastContact;
  if (contact) {
    audio.contact(contact.quality);
    flight = { ...contact, start: now, kind: state.result.includes("HOME RUN") ? "homer" : state.result.includes("THREE BASES") ? "triple" : "hit" };
    if (flight.kind === "homer") { audio.homer(); shakeUntil = now + 420; flashUntil = now + 170; }
  } else if (state.result.includes("MISS")) audio.miss();
  else audio.call();
}

function startPitch() {
  audio.enable();
  if (!started) {
    started = true;
    document.querySelector("#start-card").classList.add("is-hidden");
  }
  if (state.phase === "resolved") state = readyNextPitch(state);
  swingStartedAt = -Infinity;
  const previous = state;
  state = beginPitch(state, performance.now());
  if (state !== previous) audio.pitch();
}

function swing() {
  if (!started) { startPitch(); return; }
  if (state.phase === "ready") { startPitch(); return; }
  if (state.phase !== "pitching") return;
  const now = performance.now();
  const previous = state;
  swingStartedAt = now;
  state = resolveSwing(state, now);
  handleOutcome(previous, now);
}

function aim(dx, dy) { state = moveAim(state, dx, dy); }

document.querySelector("#start-button").addEventListener("click", startPitch);
document.querySelector("#pitch-button").addEventListener("click", startPitch);
document.querySelector("#swing-button").addEventListener("click", swing);
document.querySelectorAll("[data-control]").forEach((button) => {
  const vectors = { up: [0, -5], down: [0, 5], left: [-5, 0], right: [5, 0] };
  const move = () => aim(...vectors[button.dataset.control]);
  button.addEventListener("pointerdown", (event) => { event.preventDefault(); move(); });
});

window.addEventListener("keydown", (event) => {
  const key = event.key.toLowerCase();
  if (["arrowup", "arrowdown", "arrowleft", "arrowright", " "].includes(key)) event.preventDefault();
  if (held.has(key)) return;
  held.add(key);
  if (key === "enter") startPitch();
  if (key === " " || key === "a") swing();
  if (key === "arrowup") aim(0, -5);
  if (key === "arrowdown") aim(0, 5);
  if (key === "arrowleft") aim(-5, 0);
  if (key === "arrowright") aim(5, 0);
});
window.addEventListener("keyup", (event) => held.delete(event.key.toLowerCase()));

function frame(now) {
  if (state.phase === "pitching" && pitchProgress(state.pitch, now) >= 1) {
    const previous = state;
    state = resolveTakenPitch(state, now);
    handleOutcome(previous, now);
  }
  if (state.phase === "resolved" && now - resolvedAt > 1450 && !flight) state = readyNextPitch(state);
  draw(now);
  requestAnimationFrame(frame);
}

requestAnimationFrame(frame);
