using System;
using UnityEngine;

namespace DiamondDynasty
{
    /// <summary>Serializable definition for one pitch type.</summary>
    [Serializable]
    public sealed class PitchDefinition
    {
        public string Name = "FOUR-SEAM";
        public int SpeedMph = 95;
        public float DurationSeconds = 0.76f;
        public float BreakX = 1f;
        public float BreakY = -1f;
    }

    /// <summary>Data-driven tuning for the at-bat rules and presentation.</summary>
    [CreateAssetMenu(menuName = "Diamond Dynasty/At-Bat Config", fileName = "AtBatConfig")]
    public sealed class AtBatConfig : ScriptableObject
    {
        [Header("Native frame")]
        public int NativeWidth = 256;
        public int NativeHeight = 224;

        [Header("Strike zone")]
        public float StrikeLeft = 109f;
        public float StrikeRight = 147f;
        public float StrikeTop = 140f;
        public float StrikeBottom = 178f;
        public float AimPadding = 10f;
        public float AimStep = 5f;

        [Header("Pitch")]
        [Range(0f, 1f)] public float ContactProgress = 0.88f;
        [Range(0f, 1f)] public float TemptingBallChance = 0.27f;
        public PitchDefinition[] Pitches = Array.Empty<PitchDefinition>();

        [Header("Role flow")]
        public float BatterAutoPitchDelaySeconds = 1.2f;

        [Header("Pitcher control")]
        public float PitchTargetPadding = 16f;
        public float PitchTargetStep = 3f;
        public float PitchChargeSeconds = 1f;
        public float MinPitchSpeedMultiplier = 0.95f;
        public float MaxPitchSpeedMultiplier = 1.06f;
        public float MinPitchMissRadius = 1.5f;
        public float MaxPitchMissRadius = 13f;
        public float PitchAccuracyExponent = 2f;

        [Header("CPU batter")]
        [Range(0f, 1f)] public float CpuStrikeSwingChance = 0.76f;
        [Range(0f, 1f)] public float CpuChaseChance = 0.18f;
        public float CpuTimingJitter = 0.12f;
        public float CpuAimErrorPixels = 18f;

        [Header("Contact")]
        public float TimingWindow = 0.23f;
        public float SpatialWindow = 25f;
        public float FoulThreshold = 0.34f;
        public float WeakThreshold = 0.49f;
        public float SingleThreshold = 0.68f;
        public float DoubleThreshold = 0.84f;
        public float TripleThreshold = 0.94f;

        [Header("Presentation")]
        public float ResultHoldSeconds = 1.45f;
        public float BatterFrameSeconds = 0.075f;

        /// <summary>Creates an in-memory configuration matching the approved prototype.</summary>
        public static AtBatConfig CreatePrototypeDefaults()
        {
            var config = CreateInstance<AtBatConfig>();
            config.Pitches = new[]
            {
                new PitchDefinition { Name = "FOUR-SEAM", SpeedMph = 95, DurationSeconds = 0.76f, BreakX = 1f, BreakY = -1f },
                new PitchDefinition { Name = "CURVEBALL", SpeedMph = 79, DurationSeconds = 1.02f, BreakX = -17f, BreakY = 10f },
                new PitchDefinition { Name = "CHANGEUP", SpeedMph = 84, DurationSeconds = 0.93f, BreakX = 5f, BreakY = 5f },
            };
            return config;
        }
    }
}
