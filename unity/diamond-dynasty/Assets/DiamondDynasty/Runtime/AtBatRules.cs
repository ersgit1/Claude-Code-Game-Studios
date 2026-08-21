using System;
using UnityEngine;

namespace DiamondDynasty
{
    /// <summary>Source of deterministic random values for gameplay rules.</summary>
    public interface IAtBatRandom
    {
        /// <summary>Returns a value in the half-open range [0, 1).</summary>
        float Next01();
    }

    /// <summary>Unity-backed random source used during live play.</summary>
    public sealed class UnityAtBatRandom : IAtBatRandom
    {
        /// <inheritdoc />
        public float Next01()
        {
            return UnityEngine.Random.value;
        }
    }

    /// <summary>Current high-level phase of an at-bat.</summary>
    public enum AtBatPhase
    {
        Ready,
        Pitching,
        Resolved,
    }

    /// <summary>Mutable state owned by one at-bat rules instance.</summary>
    public sealed class AtBatState
    {
        public int Inning = 1;
        public int Outs;
        public int Balls;
        public int Strikes;
        public int Runs;
        public int Hits;
        public readonly bool[] Bases = new bool[3];
        public float AimX = 128f;
        public float AimY = 159f;
        public AtBatPhase Phase = AtBatPhase.Ready;
        public ActivePitch Pitch;
        public string Result = "PRESS ENTER TO PITCH";
        public ContactResult LastContact;
    }

    /// <summary>Runtime instance of a selected pitch.</summary>
    public sealed class ActivePitch
    {
        public PitchDefinition Definition;
        public float StartTime;
        public float TargetX;
        public float TargetY;
    }

    /// <summary>Sampled screen position and depth of a live pitch.</summary>
    public struct PitchSample
    {
        public float Progress;
        public float X;
        public float Y;
        public int Radius;
    }

    /// <summary>Contact data retained for presentation after a hit.</summary>
    public sealed class ContactResult
    {
        public string Kind;
        public float Quality;
        public float Spray;
        public float Timing;
        public float Barrel;
    }

    /// <summary>Deterministic at-bat rules independent from Unity frame lifecycle.</summary>
    public sealed class AtBatRules
    {
        private readonly AtBatConfig config;
        private readonly IAtBatRandom random;

        /// <summary>Creates rules with injected tuning and randomness.</summary>
        public AtBatRules(AtBatConfig config, IAtBatRandom random)
        {
            this.config = config != null ? config : throw new ArgumentNullException(nameof(config));
            this.random = random != null ? random : throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Creates a fresh game state.</summary>
        public AtBatState CreateState()
        {
            return new AtBatState();
        }

        /// <summary>Moves and clamps the contact cursor in logical pixels.</summary>
        public void MoveAim(AtBatState state, float deltaX, float deltaY)
        {
            state.AimX = Mathf.Clamp(state.AimX + deltaX, config.StrikeLeft - config.AimPadding, config.StrikeRight + config.AimPadding);
            state.AimY = Mathf.Clamp(state.AimY + deltaY, config.StrikeTop - config.AimPadding, config.StrikeBottom + config.AimPadding);
        }

        /// <summary>Begins a pitch when the state is ready.</summary>
        public bool BeginPitch(AtBatState state, float now)
        {
            if (state.Phase != AtBatPhase.Ready || config.Pitches == null || config.Pitches.Length == 0)
            {
                return false;
            }

            var pitchIndex = Mathf.Min(config.Pitches.Length - 1, Mathf.FloorToInt(random.Next01() * config.Pitches.Length));
            var definition = config.Pitches[pitchIndex];
            var targetX = config.StrikeLeft + 4f + random.Next01() * (config.StrikeRight - config.StrikeLeft - 8f);
            var targetY = config.StrikeTop + 4f + random.Next01() * (config.StrikeBottom - config.StrikeTop - 8f);
            var temptingBall = random.Next01() < config.TemptingBallChance;
            var edge = Mathf.Min(3, Mathf.FloorToInt(random.Next01() * 4f));

            if (temptingBall)
            {
                if (edge == 0) targetX = config.StrikeLeft - 7f - random.Next01() * 8f;
                if (edge == 1) targetX = config.StrikeRight + 7f + random.Next01() * 8f;
                if (edge == 2) targetY = config.StrikeTop - 6f - random.Next01() * 7f;
                if (edge == 3) targetY = config.StrikeBottom + 6f + random.Next01() * 8f;
            }

            state.Pitch = new ActivePitch
            {
                Definition = definition,
                StartTime = now,
                TargetX = targetX,
                TargetY = targetY,
            };
            state.Phase = AtBatPhase.Pitching;
            state.Result = string.Format("{0}  {1} MPH", definition.Name, definition.SpeedMph);
            state.LastContact = null;
            return true;
        }

        /// <summary>Returns normalized progress for a live pitch.</summary>
        public float PitchProgress(ActivePitch pitch, float now)
        {
            return Mathf.Clamp01((now - pitch.StartTime) / pitch.Definition.DurationSeconds);
        }

        /// <summary>Samples the pitch in logical screen coordinates.</summary>
        public PitchSample SamplePitch(ActivePitch pitch, float now)
        {
            var progress = PitchProgress(pitch, now);
            var depth = progress * progress * (3f - 2f * progress);
            var bend = Mathf.Sin(progress * Mathf.PI) * pitch.Definition.BreakX;
            var drop = progress * progress * progress * pitch.Definition.BreakY;
            return new PitchSample
            {
                Progress = progress,
                X = 128f + (pitch.TargetX - 128f) * depth + bend,
                Y = 111f + (pitch.TargetY - 111f) * depth + drop,
                Radius = 1 + Mathf.FloorToInt(progress * 3.2f),
            };
        }

        /// <summary>Resolves a pitch that crossed the plate without a swing.</summary>
        public bool ResolveTakenPitch(AtBatState state, float now)
        {
            if (state.Phase != AtBatPhase.Pitching || PitchProgress(state.Pitch, now) < 1f)
            {
                return false;
            }

            var finalY = state.Pitch.TargetY + state.Pitch.Definition.BreakY;
            var isStrike = IsStrikeLocation(state.Pitch.TargetX, finalY);
            ApplyOutcome(state, isStrike ? "strike" : "ball", isStrike ? "CALLED STRIKE" : "BALL — GOOD EYE", null);
            return true;
        }

        /// <summary>Resolves a swing against the live pitch.</summary>
        public bool ResolveSwing(AtBatState state, float now)
        {
            if (state.Phase != AtBatPhase.Pitching)
            {
                return false;
            }

            var ball = SamplePitch(state.Pitch, now);
            var timingError = Mathf.Abs(ball.Progress - config.ContactProgress);
            var spatialError = Vector2.Distance(new Vector2(state.AimX, state.AimY), new Vector2(ball.X, ball.Y));
            if (timingError > config.TimingWindow || spatialError > config.SpatialWindow)
            {
                ApplyOutcome(state, "strike", timingError > config.TimingWindow ? "SWING AND A MISS — TIMING" : "SWING AND A MISS — CHASED", null);
                return true;
            }

            var timing = Mathf.Clamp01(1f - timingError / config.TimingWindow);
            var barrel = Mathf.Clamp01(1f - spatialError / config.SpatialWindow);
            var quality = timing * 0.58f + barrel * 0.42f;
            var spray = Mathf.Clamp((state.AimX - ball.X) * 2.2f + (ball.Progress - config.ContactProgress) * 210f, -46f, 46f);
            var contact = new ContactResult { Quality = quality, Spray = spray, Timing = timing, Barrel = barrel };

            if (quality < config.FoulThreshold) ApplyOutcome(state, "foul", "FOULED IT BACK", contact);
            else if (quality < config.WeakThreshold) ApplyOutcome(state, random.Next01() < 0.62f ? "out" : "single", "CHOPPER IN PLAY", contact);
            else if (quality < config.SingleThreshold) ApplyOutcome(state, random.Next01() < 0.28f ? "out" : "single", "SOLID LINE DRIVE", contact);
            else if (quality < config.DoubleThreshold) ApplyOutcome(state, "double", "DRIVEN INTO THE GAP — DOUBLE", contact);
            else if (quality < config.TripleThreshold) ApplyOutcome(state, "triple", "OFF THE WALL — THREE BASES", contact);
            else ApplyOutcome(state, "homer", "ABSOLUTELY CRUSHED — HOME RUN!", contact);
            return true;
        }

        /// <summary>Returns a resolved state to its raised ready pose.</summary>
        public bool ReadyNextPitch(AtBatState state)
        {
            if (state.Phase != AtBatPhase.Resolved)
            {
                return false;
            }

            state.Phase = AtBatPhase.Ready;
            state.Pitch = null;
            state.Result = "PRESS ENTER TO PITCH";
            state.LastContact = null;
            return true;
        }

        /// <summary>Applies a named outcome for deterministic tests and presentation.</summary>
        public void ApplyOutcome(AtBatState state, string kind, string label, ContactResult contact)
        {
            if (kind == "strike")
            {
                if (state.Strikes >= 2) AddOut(state, string.IsNullOrEmpty(label) ? "STRIKE THREE" : label);
                else
                {
                    state.Strikes += 1;
                    Resolve(state, label, null);
                }
                return;
            }

            if (kind == "ball")
            {
                if (state.Balls >= 3)
                {
                    ClearCount(state);
                    AwardWalk(state);
                    Resolve(state, "BALL FOUR — TAKE YOUR BASE", null);
                }
                else
                {
                    state.Balls += 1;
                    Resolve(state, label, null);
                }
                return;
            }

            if (kind == "foul")
            {
                state.Strikes = Mathf.Min(2, state.Strikes + 1);
                Resolve(state, label, null);
                return;
            }

            if (kind == "out")
            {
                AddOut(state, label);
                return;
            }

            var basesEarned = kind == "single" ? 1 : kind == "double" ? 2 : kind == "triple" ? 3 : kind == "homer" ? 4 : 0;
            if (basesEarned <= 0)
            {
                return;
            }

            ClearCount(state);
            AdvanceRunners(state, basesEarned);
            state.Hits += 1;
            var resolvedContact = contact ?? new ContactResult();
            resolvedContact.Kind = kind;
            Resolve(state, label, resolvedContact);
        }

        private bool IsStrikeLocation(float x, float y)
        {
            return x >= config.StrikeLeft && x <= config.StrikeRight && y >= config.StrikeTop && y <= config.StrikeBottom;
        }

        private static void Resolve(AtBatState state, string label, ContactResult contact)
        {
            state.Result = label;
            state.LastContact = contact;
            state.Phase = AtBatPhase.Resolved;
        }

        private static void ClearCount(AtBatState state)
        {
            state.Balls = 0;
            state.Strikes = 0;
        }

        private static void AwardWalk(AtBatState state)
        {
            var first = state.Bases[0];
            var second = state.Bases[1];
            var third = state.Bases[2];
            if (first && second && third) state.Runs += 1;
            state.Bases[0] = true;
            state.Bases[1] = second || first;
            state.Bases[2] = third || (first && second);
        }

        private static void AdvanceRunners(AtBatState state, int basesEarned)
        {
            if (basesEarned == 4)
            {
                state.Runs += 1 + (state.Bases[0] ? 1 : 0) + (state.Bases[1] ? 1 : 0) + (state.Bases[2] ? 1 : 0);
                Array.Clear(state.Bases, 0, state.Bases.Length);
                return;
            }

            var nextBases = new bool[3];
            var scored = 0;
            for (var index = 0; index < state.Bases.Length; index += 1)
            {
                if (!state.Bases[index]) continue;
                var destination = index + basesEarned;
                if (destination >= 3) scored += 1;
                else nextBases[destination] = true;
            }
            if (basesEarned >= 3) scored += 1;
            else nextBases[basesEarned - 1] = true;
            state.Runs += scored;
            Array.Copy(nextBases, state.Bases, state.Bases.Length);
        }

        private static void AddOut(AtBatState state, string label)
        {
            ClearCount(state);
            state.Outs += 1;
            Resolve(state, label, null);
            if (state.Outs < 3) return;
            state.Inning += 1;
            state.Outs = 0;
            Array.Clear(state.Bases, 0, state.Bases.Length);
            state.Result = "SIDE RETIRED — NEXT INNING";
        }
    }
}
