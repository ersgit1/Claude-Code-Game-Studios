// PROTOTYPE - NOT FOR PRODUCTION
// Question: Can an original browser-native 16-bit baseball at-bat feel readable, skillful, and visually competitive with strong SNES-era sports games?
// Date: 2026-08-20

export class BallparkAudio {
  constructor() { this.context = null; }

  enable() {
    this.context ??= new (window.AudioContext || window.webkitAudioContext)();
    if (this.context.state === "suspended") this.context.resume();
  }

  tone(frequency, duration, type = "square", volume = 0.05, slide = 0) {
    if (!this.context) return;
    const now = this.context.currentTime;
    const oscillator = this.context.createOscillator();
    const gain = this.context.createGain();
    oscillator.type = type;
    oscillator.frequency.setValueAtTime(frequency, now);
    oscillator.frequency.linearRampToValueAtTime(Math.max(40, frequency + slide), now + duration);
    gain.gain.setValueAtTime(volume, now);
    gain.gain.exponentialRampToValueAtTime(0.0001, now + duration);
    oscillator.connect(gain).connect(this.context.destination);
    oscillator.start(now);
    oscillator.stop(now + duration);
  }

  pitch() { this.tone(180, 0.08, "triangle", 0.025, 55); }
  miss() { this.tone(120, 0.11, "square", 0.03, -55); }
  call() { this.tone(220, 0.09, "square", 0.025, -30); }
  contact(quality = 0.5) {
    this.tone(170 + quality * 170, 0.12, "square", 0.06, 80);
    this.tone(75, 0.08, "triangle", 0.05, -20);
  }
  homer() {
    [262, 330, 392, 523].forEach((frequency, index) => setTimeout(() => this.tone(frequency, 0.22, "square", 0.04), index * 95));
  }
}
