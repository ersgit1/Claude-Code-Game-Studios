import {
  BATTER_SPRITE_PATHS,
  batterFrameForElapsed,
} from "./player-art.mjs?v=sprite-art-v7";

const POSE_NAMES = Object.freeze([
  "READY",
  "LOAD",
  "STRIDE",
  "PLANT",
  "SWING",
  "CONTACT",
  "EXTEND",
  "FINISH",
  "RESET",
]);

const READY_HOLD_MS = 500;
const ACTION_END_MS = 900;
const RECOVERY_HOLD_MS = 450;
const CYCLE_MS = READY_HOLD_MS + ACTION_END_MS + RECOVERY_HOLD_MS;

const liveBatters = [...document.querySelectorAll("[data-live-batter]")];
const poseButtons = [...document.querySelectorAll("[data-frame]")];
const frameStatus = document.querySelector("#frame-status");
const playGameButton = document.querySelector("#play-game");
const playSlowButton = document.querySelector("#play-slow");
const pauseButton = document.querySelector("#pause");
const stepButton = document.querySelector("#step");

let activeFrame = -1;
let playing = true;
let speed = 1;
let cycleStartedAt = performance.now();

function showFrame(frame) {
  const boundedFrame = Math.max(0, Math.min(POSE_NAMES.length - 1, frame));
  if (activeFrame === boundedFrame) return;
  activeFrame = boundedFrame;

  for (const image of liveBatters) {
    image.src = BATTER_SPRITE_PATHS[boundedFrame];
    image.alt = `${POSE_NAMES[boundedFrame].toLowerCase()} batter pose`;
  }

  poseButtons.forEach((button, index) => {
    button.setAttribute("aria-pressed", String(index === boundedFrame));
  });
  frameStatus.textContent = `${boundedFrame + 1} / ${POSE_NAMES.length} — ${POSE_NAMES[boundedFrame]}`;
}

function setPlayback(mode) {
  playing = mode !== "pause";
  speed = mode === "slow" ? 0.35 : 1;
  cycleStartedAt = performance.now();
  playGameButton.setAttribute("aria-pressed", String(mode === "game"));
  playSlowButton.setAttribute("aria-pressed", String(mode === "slow"));
  pauseButton.setAttribute("aria-pressed", String(mode === "pause"));
}

function reviewFrameForElapsed(elapsed) {
  if (elapsed < READY_HOLD_MS) return 0;
  return batterFrameForElapsed(elapsed - READY_HOLD_MS);
}

function animate(now) {
  if (playing) {
    const elapsed = ((now - cycleStartedAt) * speed) % CYCLE_MS;
    showFrame(reviewFrameForElapsed(elapsed));
  }
  requestAnimationFrame(animate);
}

playGameButton.addEventListener("click", () => setPlayback("game"));
playSlowButton.addEventListener("click", () => setPlayback("slow"));
pauseButton.addEventListener("click", () => setPlayback("pause"));
stepButton.addEventListener("click", () => {
  setPlayback("pause");
  showFrame((activeFrame + 1) % POSE_NAMES.length);
});
poseButtons.forEach((button) => {
  button.addEventListener("click", () => {
    setPlayback("pause");
    showFrame(Number(button.dataset.frame));
  });
});

showFrame(0);
if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
  setPlayback("pause");
}
requestAnimationFrame(animate);
