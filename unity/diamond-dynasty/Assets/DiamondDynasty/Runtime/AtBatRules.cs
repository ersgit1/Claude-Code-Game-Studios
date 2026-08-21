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
        RoleSelection,
        Ready,
        PitchSetup,
        Charging,
        Pitching,
        Resolved,
    }

    /// <summary>Side controlled by the local player.</summary>
    public enum PlayerRole
    {
        None,
        Batter,
        Pitcher,
    }

    /// <summary>Mutable state owned by one at-bat rules instance.</summary>
    public sealed class AtBatState
    {
        public PlayerRole Role;
        public int Inning = 1;
        public int Outs;
        public int Balls;
        public int Strikes;
        public int Runs;
        public int Hits;
        public readonly bool[] Bases = new bool[3];
        public float AimX = 128f;
        public float AimY = 159f;
        public int SelectedPitchIndex;
        public float PitchTargetX = 128f;
        public float PitchTargetY = 159f;
        public float ChargeStartedAt = -1f;
        public float Power01;
        public AtBatPhase Phase = AtBatPhase.RoleSelection;
        public ActivePitch Pitch;
        public string Result = "CHOOSE BATTER OR PITCHER";
        public ContactResult LastContact;

        /// <summary>Whether pitcher-only controls and intent may be shown.</summary>
        public bool ShowPitcherDetails
        {
            get { return Role == PlayerRole.Pitcher && Phase != AtBatPhase.RoleSelection; }
        }

        /// <summary>Whether the human batter's contact cursor may be shown.</summary>
        public bool ShowBatterCursor
        {
            get { return Role == PlayerRole.Batter && (Phase == AtBatPhase.Ready || Phase == AtBatPhase.Pitching); }
        }
    }

    /// <summary>Runtime instance of a selected pitch.</summary>
    public sealed class ActivePitch
    {
        public PitchDefinition Definition;
        public float StartTime;
        public float RequestedPlateX;
        public float RequestedPlateY;
        public float ActualPlateX;
        public float ActualPlateY;
        public float Power01;
        public float MissRadius;
        public float DurationSeconds;
        public int EffectiveSpeedMph;
        public bool CpuWillSwing;
        public bool CpuDecisionMade;
        public float CpuSwingProgress;
        public float CpuAimX;
        public float CpuAimY;
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
        private const string BatterReadyMessage = "AIM: ARROWS/D-PAD  SWING: SPACE/SOUTH";
        private const string PitcherSetupMessage = "Q/E/LB/RB PITCH  AIM: ARROWS/D-PAD";
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

        /// <summary>Selects the human-controlled side from the opening menu.</summary>
        public bool SelectRole(AtBatState state, PlayerRole role)
        {
            if (state.Phase != AtBatPhase.RoleSelection || role == PlayerRole.None)
            {
                return false;
            }

            state.Role = role;
            state.Phase = role == PlayerRole.Batter ? AtBatPhase.Ready : AtBatPhase.PitchSetup;
            state.Result = role == PlayerRole.Batter ? BatterReadyMessage : PitcherSetupMessage;
            state.PitchTargetX = (config.StrikeLeft + config.StrikeRight) * 0.5f;
            state.PitchTargetY = (config.StrikeTop + config.StrikeBottom) * 0.5f;
            return true;
        }

        /// <summary>Moves and clamps the contact cursor in logical pixels.</summary>
        public bool MoveAim(AtBatState state, float deltaX, float deltaY)
        {
            if (state.Role != PlayerRole.Batter || (state.Phase != AtBatPhase.Ready && state.Phase != AtBatPhase.Pitching))
            {
                return false;
            }

            state.AimX = Mathf.Clamp(state.AimX + deltaX, config.StrikeLeft - config.AimPadding, config.StrikeRight + config.AimPadding);
            state.AimY = Mathf.Clamp(state.AimY + deltaY, config.StrikeTop - config.AimPadding, config.StrikeBottom + config.AimPadding);
            return true;
        }

        /// <summary>Cycles the pitcher's selected pitch while setting up.</summary>
        public bool CyclePitch(AtBatState state, int direction)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.PitchSetup || config.Pitches == null || config.Pitches.Length == 0)
            {
                return false;
            }

            var count = config.Pitches.Length;
            state.SelectedPitchIndex = ((state.SelectedPitchIndex + direction) % count + count) % count;
            state.Result = PitcherSetupMessage;
            return true;
        }

        /// <summary>Selects a pitch by index while the pitcher is setting up.</summary>
        public bool SelectPitch(AtBatState state, int pitchIndex)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.PitchSetup || config.Pitches == null || pitchIndex < 0 || pitchIndex >= config.Pitches.Length)
            {
                return false;
            }

            state.SelectedPitchIndex = pitchIndex;
            state.Result = PitcherSetupMessage;
            return true;
        }

        /// <summary>Moves and clamps the pitcher's requested plate location.</summary>
        public bool MovePitchTarget(AtBatState state, float deltaX, float deltaY)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.PitchSetup)
            {
                return false;
            }

            state.PitchTargetX = Mathf.Clamp(state.PitchTargetX + deltaX, config.StrikeLeft - config.PitchTargetPadding, config.StrikeRight + config.PitchTargetPadding);
            state.PitchTargetY = Mathf.Clamp(state.PitchTargetY + deltaY, config.StrikeTop - config.PitchTargetPadding, config.StrikeBottom + config.PitchTargetPadding);
            return true;
        }

        /// <summary>Starts charging a human-controlled pitch.</summary>
        public bool BeginPitchCharge(AtBatState state, float now)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.PitchSetup || config.Pitches == null || config.Pitches.Length == 0)
            {
                return false;
            }

            state.Phase = AtBatPhase.Charging;
            state.ChargeStartedAt = now;
            state.Power01 = 0f;
            state.Result = "HOLD FOR POWER — RELEASE TO PITCH";
            return true;
        }

        /// <summary>Updates and returns normalized pitch power.</summary>
        public float UpdatePitchCharge(AtBatState state, float now)
        {
            if (state.Phase != AtBatPhase.Charging)
            {
                return state.Power01;
            }

            state.Power01 = config.PitchChargeSeconds <= 0f
                ? 1f
                : Mathf.Clamp01((now - state.ChargeStartedAt) / config.PitchChargeSeconds);
            return state.Power01;
        }

        /// <summary>Cancels an interrupted pitch charge and returns to setup.</summary>
        public bool CancelPitchCharge(AtBatState state)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.Charging)
            {
                return false;
            }

            state.Phase = AtBatPhase.PitchSetup;
            state.ChargeStartedAt = -1f;
            state.Power01 = 0f;
            state.Result = PitcherSetupMessage;
            return true;
        }

        /// <summary>Releases a charged human-controlled pitch.</summary>
        public bool ReleasePitch(AtBatState state, float now)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.Charging || config.Pitches == null || config.Pitches.Length == 0)
            {
                return false;
            }

            var power = UpdatePitchCharge(state, now);
            var definition = config.Pitches[Mathf.Clamp(state.SelectedPitchIndex, 0, config.Pitches.Length - 1)];
            var speedMultiplier = Mathf.Lerp(config.MinPitchSpeedMultiplier, config.MaxPitchSpeedMultiplier, power);
            var missRadius = Mathf.Lerp(config.MinPitchMissRadius, config.MaxPitchMissRadius, Mathf.Pow(power, config.PitchAccuracyExponent));
            var missDistance = Mathf.Sqrt(random.Next01()) * missRadius;
            var missAngle = random.Next01() * Mathf.PI * 2f;
            var actualX = state.PitchTargetX + Mathf.Cos(missAngle) * missDistance;
            var actualY = state.PitchTargetY + Mathf.Sin(missAngle) * missDistance;

            state.Pitch = CreateActivePitch(definition, now, state.PitchTargetX, state.PitchTargetY, actualX, actualY, power, missRadius, speedMultiplier);
            ConfigureCpuBatter(state.Pitch);
            state.Phase = AtBatPhase.Pitching;
            state.ChargeStartedAt = -1f;
            state.Result = string.Format("{0}  {1} MPH", definition.Name, state.Pitch.EffectiveSpeedMph);
            state.LastContact = null;
            return true;
        }

        /// <summary>Begins the hidden-information CPU pitch used in batter mode.</summary>
        public bool BeginCpuPitch(AtBatState state, float now)
        {
            if (state.Role != PlayerRole.Batter || state.Phase != AtBatPhase.Ready || config.Pitches == null || config.Pitches.Length == 0)
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

            state.Pitch = CreateActivePitch(definition, now, targetX, targetY, targetX, targetY, 0.5f, 0f, 1f);
            state.Phase = AtBatPhase.Pitching;
            state.Result = "PITCH INCOMING";
            state.LastContact = null;
            return true;
        }

        /// <summary>Compatibility wrapper for the original CPU-pitch entry point.</summary>
        public bool BeginPitch(AtBatState state, float now)
        {
            return BeginCpuPitch(state, now);
        }

        /// <summary>Returns normalized progress for a live pitch.</summary>
        public float PitchProgress(ActivePitch pitch, float now)
        {
            return Mathf.Clamp01((now - pitch.StartTime) / pitch.DurationSeconds);
        }

        /// <summary>Samples the pitch in logical screen coordinates.</summary>
        public PitchSample SamplePitch(ActivePitch pitch, float now)
        {
            var progress = PitchProgress(pitch, now);
            var depth = progress * progress * (3f - 2f * progress);
            var bend = Mathf.Sin(progress * Mathf.PI) * pitch.Definition.BreakX;
            var drop = progress * progress * progress * pitch.Definition.BreakY;
            var trajectoryTargetY = pitch.ActualPlateY - pitch.Definition.BreakY;
            return new PitchSample
            {
                Progress = progress,
                X = 128f + (pitch.ActualPlateX - 128f) * depth + bend,
                Y = 111f + (trajectoryTargetY - 111f) * depth + drop,
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

            var isStrike = IsStrikeLocation(state.Pitch.ActualPlateX, state.Pitch.ActualPlateY);
            ApplyOutcome(state, isStrike ? "strike" : "ball", isStrike ? "CALLED STRIKE" : "BALL — GOOD EYE", null);
            return true;
        }

        /// <summary>Resolves a swing against the live pitch.</summary>
        public bool ResolveSwing(AtBatState state, float now)
        {
            if (state.Role != PlayerRole.Batter)
            {
                return false;
            }

            return ResolveSwingAt(state, now, state.AimX, state.AimY);
        }

        /// <summary>Resolves a swing at an explicit barrel location.</summary>
        public bool ResolveSwingAt(AtBatState state, float now, float aimX, float aimY)
        {
            if (state.Phase != AtBatPhase.Pitching)
            {
                return false;
            }

            var ball = SamplePitch(state.Pitch, now);
            var timingError = Mathf.Abs(ball.Progress - config.ContactProgress);
            var spatialError = Vector2.Distance(new Vector2(aimX, aimY), new Vector2(ball.X, ball.Y));
            if (timingError > config.TimingWindow || spatialError > config.SpatialWindow)
            {
                ApplyOutcome(state, "strike", timingError > config.TimingWindow ? "SWING AND A MISS — TIMING" : "SWING AND A MISS — CHASED", null);
                return true;
            }

            var timing = Mathf.Clamp01(1f - timingError / config.TimingWindow);
            var barrel = Mathf.Clamp01(1f - spatialError / config.SpatialWindow);
            var quality = timing * 0.58f + barrel * 0.42f;
            var spray = Mathf.Clamp((aimX - ball.X) * 2.2f + (ball.Progress - config.ContactProgress) * 210f, -46f, 46f);
            var contact = new ContactResult { Quality = quality, Spray = spray, Timing = timing, Barrel = barrel };

            if (quality < config.FoulThreshold) ApplyOutcome(state, "foul", "FOULED IT BACK", contact);
            else if (quality < config.WeakThreshold) ApplyOutcome(state, random.Next01() < 0.62f ? "out" : "single", "CHOPPER IN PLAY", contact);
            else if (quality < config.SingleThreshold) ApplyOutcome(state, random.Next01() < 0.28f ? "out" : "single", "SOLID LINE DRIVE", contact);
            else if (quality < config.DoubleThreshold) ApplyOutcome(state, "double", "DRIVEN INTO THE GAP — DOUBLE", contact);
            else if (quality < config.TripleThreshold) ApplyOutcome(state, "triple", "OFF THE WALL — THREE BASES", contact);
            else ApplyOutcome(state, "homer", "ABSOLUTELY CRUSHED — HOME RUN!", contact);
            return true;
        }

        /// <summary>Runs the pitcher-mode CPU batter decision at most once.</summary>
        public bool TryResolveCpuBatter(AtBatState state, float now)
        {
            if (state.Role != PlayerRole.Pitcher || state.Phase != AtBatPhase.Pitching || state.Pitch == null || state.Pitch.CpuDecisionMade)
            {
                return false;
            }

            if (PitchProgress(state.Pitch, now) < state.Pitch.CpuSwingProgress)
            {
                return false;
            }

            state.Pitch.CpuDecisionMade = true;
            if (!state.Pitch.CpuWillSwing)
            {
                return false;
            }

            return ResolveSwingAt(state, state.Pitch.StartTime + state.Pitch.DurationSeconds * state.Pitch.CpuSwingProgress, state.Pitch.CpuAimX, state.Pitch.CpuAimY);
        }

        /// <summary>Returns a resolved state to its raised ready pose.</summary>
        public bool ReadyNextPitch(AtBatState state)
        {
            if (state.Phase != AtBatPhase.Resolved)
            {
                return false;
            }

            state.Phase = state.Role == PlayerRole.Pitcher ? AtBatPhase.PitchSetup : AtBatPhase.Ready;
            state.Pitch = null;
            state.Power01 = 0f;
            state.ChargeStartedAt = -1f;
            state.Result = state.Role == PlayerRole.Pitcher ? PitcherSetupMessage : BatterReadyMessage;
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

        private ActivePitch CreateActivePitch(
            PitchDefinition definition,
            float now,
            float requestedX,
            float requestedY,
            float actualX,
            float actualY,
            float power,
            float missRadius,
            float speedMultiplier)
        {
            return new ActivePitch
            {
                Definition = definition,
                StartTime = now,
                RequestedPlateX = requestedX,
                RequestedPlateY = requestedY,
                ActualPlateX = actualX,
                ActualPlateY = actualY,
                Power01 = power,
                MissRadius = missRadius,
                DurationSeconds = definition.DurationSeconds / Mathf.Max(0.01f, speedMultiplier),
                EffectiveSpeedMph = Mathf.RoundToInt(definition.SpeedMph * speedMultiplier),
            };
        }

        private void ConfigureCpuBatter(ActivePitch pitch)
        {
            var swingChance = IsStrikeLocation(pitch.ActualPlateX, pitch.ActualPlateY)
                ? config.CpuStrikeSwingChance
                : config.CpuChaseChance;
            pitch.CpuWillSwing = random.Next01() < swingChance;
            pitch.CpuSwingProgress = Mathf.Clamp(
                config.ContactProgress + (random.Next01() * 2f - 1f) * config.CpuTimingJitter,
                0.05f,
                0.98f);

            var swingTime = pitch.StartTime + pitch.DurationSeconds * pitch.CpuSwingProgress;
            var ballAtSwing = SamplePitch(pitch, swingTime);
            var aimDistance = Mathf.Sqrt(random.Next01()) * config.CpuAimErrorPixels;
            var aimAngle = random.Next01() * Mathf.PI * 2f;
            pitch.CpuAimX = ballAtSwing.X + Mathf.Cos(aimAngle) * aimDistance;
            pitch.CpuAimY = ballAtSwing.Y + Mathf.Sin(aimAngle) * aimDistance;
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
