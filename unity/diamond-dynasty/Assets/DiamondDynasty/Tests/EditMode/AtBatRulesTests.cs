using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DiamondDynasty.Tests
{
    public sealed class AtBatRulesTests
    {
        private AtBatConfig config;

        [SetUp]
        public void SetUp()
        {
            config = AtBatConfig.CreatePrototypeDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BeginPitch_SelectsPitchAndEntersPitchingPhase()
        {
            var rules = CreateRules(0f, 0.5f, 0.5f, 1f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Batter);

            Assert.That(rules.BeginPitch(state, 10f), Is.True);
            Assert.That(state.Phase, Is.EqualTo(AtBatPhase.Pitching));
            Assert.That(state.Pitch.Definition.Name, Is.EqualTo("FOUR-SEAM"));
            Assert.That(state.Pitch.StartTime, Is.EqualTo(10f));
        }

        [Test]
        public void TakenStrike_IncrementsStrikeCount()
        {
            var rules = CreateRules(0f, 0.5f, 0.5f, 1f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Batter);
            rules.BeginPitch(state, 0f);

            Assert.That(rules.ResolveTakenPitch(state, 1f), Is.True);
            Assert.That(state.Strikes, Is.EqualTo(1));
            Assert.That(state.Result, Is.EqualTo("CALLED STRIKE"));
        }

        [Test]
        public void BallFour_WithLoadedBases_ForcesExactlyOneRun()
        {
            var rules = CreateRules();
            var state = rules.CreateState();
            state.Balls = 3;
            state.Bases[0] = true;
            state.Bases[1] = true;
            state.Bases[2] = true;

            rules.ApplyOutcome(state, "ball", "BALL", null);

            Assert.That(state.Runs, Is.EqualTo(1));
            Assert.That(state.Bases, Is.EqualTo(new[] { true, true, true }));
            Assert.That(state.Balls, Is.Zero);
        }

        [Test]
        public void Foul_WithTwoStrikes_DoesNotCreateAnOut()
        {
            var rules = CreateRules();
            var state = rules.CreateState();
            state.Strikes = 2;

            rules.ApplyOutcome(state, "foul", "FOUL BALL", new ContactResult());

            Assert.That(state.Strikes, Is.EqualTo(2));
            Assert.That(state.Outs, Is.Zero);
        }

        [Test]
        public void PerfectContact_ProducesHomeRun()
        {
            var rules = CreateRules(0f, 0.5f, 0.5f, 1f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Batter);
            rules.BeginPitch(state, 0f);
            var contactTime = state.Pitch.Definition.DurationSeconds * config.ContactProgress;
            var sample = rules.SamplePitch(state.Pitch, contactTime);
            state.AimX = sample.X;
            state.AimY = sample.Y;

            Assert.That(rules.ResolveSwing(state, contactTime), Is.True);
            Assert.That(state.LastContact.Kind, Is.EqualTo("homer"));
            Assert.That(state.Runs, Is.EqualTo(1));
            Assert.That(state.Hits, Is.EqualTo(1));
        }

        [Test]
        public void DirectHitOutcome_WithoutPresentationContact_RemainsValid()
        {
            var rules = CreateRules();
            var state = rules.CreateState();

            rules.ApplyOutcome(state, "single", "BASE HIT", null);

            Assert.That(state.LastContact, Is.Not.Null);
            Assert.That(state.LastContact.Kind, Is.EqualTo("single"));
            Assert.That(state.Bases, Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void ThirdOut_AdvancesInningAndClearsBases()
        {
            var rules = CreateRules();
            var state = rules.CreateState();
            state.Outs = 2;
            state.Bases[0] = true;

            rules.ApplyOutcome(state, "out", "FLY OUT", null);

            Assert.That(state.Inning, Is.EqualTo(2));
            Assert.That(state.Outs, Is.Zero);
            Assert.That(state.Bases, Is.EqualTo(new[] { false, false, false }));
            Assert.That(state.Result, Is.EqualTo("SIDE RETIRED — NEXT INNING"));
        }

        [Test]
        public void FreshState_RequiresRoleSelectionBeforeGameplay()
        {
            var rules = CreateRules();
            var state = rules.CreateState();

            Assert.That(state.Phase, Is.EqualTo(AtBatPhase.RoleSelection));
            Assert.That(state.Role, Is.EqualTo(PlayerRole.None));
            Assert.That(rules.BeginPitch(state, 0f), Is.False);
            Assert.That(rules.BeginPitchCharge(state, 0f), Is.False);
        }

        [Test]
        public void SelectRole_EntersRoleSpecificSetupAndVisibility()
        {
            var batterRules = CreateRules();
            var batter = batterRules.CreateState();
            var pitcherRules = CreateRules();
            var pitcher = pitcherRules.CreateState();

            Assert.That(batterRules.SelectRole(batter, PlayerRole.Batter), Is.True);
            Assert.That(pitcherRules.SelectRole(pitcher, PlayerRole.Pitcher), Is.True);

            Assert.That(batter.Phase, Is.EqualTo(AtBatPhase.Ready));
            Assert.That(batter.ShowPitcherDetails, Is.False);
            Assert.That(batter.ShowBatterCursor, Is.True);
            Assert.That(pitcher.Phase, Is.EqualTo(AtBatPhase.PitchSetup));
            Assert.That(pitcher.ShowPitcherDetails, Is.True);
        }

        [Test]
        public void CyclePitch_WrapsInBothDirections()
        {
            var rules = CreateRules();
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Pitcher);

            Assert.That(rules.CyclePitch(state, -1), Is.True);
            Assert.That(state.SelectedPitchIndex, Is.EqualTo(config.Pitches.Length - 1));
            Assert.That(rules.CyclePitch(state, 1), Is.True);
            Assert.That(state.SelectedPitchIndex, Is.Zero);
        }

        [Test]
        public void MovePitchTarget_ClampsToConfiguredPitchableArea()
        {
            var rules = CreateRules();
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Pitcher);

            rules.MovePitchTarget(state, -999f, 999f);

            Assert.That(state.PitchTargetX, Is.EqualTo(config.StrikeLeft - config.PitchTargetPadding));
            Assert.That(state.PitchTargetY, Is.EqualTo(config.StrikeBottom + config.PitchTargetPadding));
        }

        [Test]
        public void PitchCharge_GrowsMonotonicallyAndClamps()
        {
            var rules = CreateRules();
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Pitcher);
            rules.BeginPitchCharge(state, 10f);

            var half = rules.UpdatePitchCharge(state, 10f + config.PitchChargeSeconds * 0.5f);
            var full = rules.UpdatePitchCharge(state, 10f + config.PitchChargeSeconds * 2f);

            Assert.That(half, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(full, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void HigherPower_IsFasterAndCarriesMoreLocationRisk()
        {
            var lowRules = CreateRules(1f, 0f, 0f, 0.5f, 0f, 0f);
            var low = lowRules.CreateState();
            lowRules.SelectRole(low, PlayerRole.Pitcher);
            lowRules.BeginPitchCharge(low, 0f);
            lowRules.ReleasePitch(low, 0f);

            var highRules = CreateRules(1f, 0f, 0f, 0.5f, 0f, 0f);
            var high = highRules.CreateState();
            highRules.SelectRole(high, PlayerRole.Pitcher);
            highRules.BeginPitchCharge(high, 0f);
            highRules.ReleasePitch(high, config.PitchChargeSeconds);

            Assert.That(high.Pitch.DurationSeconds, Is.LessThan(low.Pitch.DurationSeconds));
            Assert.That(high.Pitch.EffectiveSpeedMph, Is.GreaterThan(low.Pitch.EffectiveSpeedMph));
            Assert.That(high.Pitch.MissRadius, Is.GreaterThan(low.Pitch.MissRadius));
            Assert.That(Vector2.Distance(new Vector2(high.Pitch.RequestedPlateX, high.Pitch.RequestedPlateY), new Vector2(high.Pitch.ActualPlateX, high.Pitch.ActualPlateY)),
                Is.GreaterThan(Vector2.Distance(new Vector2(low.Pitch.RequestedPlateX, low.Pitch.RequestedPlateY), new Vector2(low.Pitch.ActualPlateX, low.Pitch.ActualPlateY))));
        }

        [Test]
        public void SamplePitch_EndsAtActualPlateLocationDespiteBreak()
        {
            var rules = CreateRules(1f, 0.25f, 1f, 0.5f, 0f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Pitcher);
            rules.SelectPitch(state, 1);
            rules.BeginPitchCharge(state, 0f);
            rules.ReleasePitch(state, config.PitchChargeSeconds);

            var sample = rules.SamplePitch(state.Pitch, state.Pitch.StartTime + state.Pitch.DurationSeconds);

            Assert.That(sample.X, Is.EqualTo(state.Pitch.ActualPlateX).Within(0.001f));
            Assert.That(sample.Y, Is.EqualTo(state.Pitch.ActualPlateY).Within(0.001f));
        }

        [Test]
        public void BatterPitch_UsesNeutralHiddenInformationMessage()
        {
            var rules = CreateRules(0f, 0.5f, 0.5f, 1f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Batter);

            rules.BeginCpuPitch(state, 0f);

            Assert.That(state.ShowPitcherDetails, Is.False);
            Assert.That(state.Result, Is.EqualTo("PITCH INCOMING"));
            Assert.That(state.Result, Does.Not.Contain(state.Pitch.Definition.Name));
            Assert.That(state.Result, Does.Not.Contain(state.Pitch.EffectiveSpeedMph.ToString()));
        }

        [Test]
        public void CpuBatter_DecidesAtMostOnce()
        {
            var rules = CreateRules(0f, 0f, 0f, 0.5f, 0f, 0f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Pitcher);
            rules.BeginPitchCharge(state, 0f);
            rules.ReleasePitch(state, 0f);
            var decisionTime = state.Pitch.StartTime + state.Pitch.DurationSeconds * state.Pitch.CpuSwingProgress;

            Assert.That(rules.TryResolveCpuBatter(state, decisionTime), Is.True);
            Assert.That(rules.TryResolveCpuBatter(state, decisionTime), Is.False);
        }

        [Test]
        public void CpuBatter_AimsAroundBallAtScheduledSwingTime()
        {
            var rules = CreateRules(0f, 0.5f, 0f, 0f, 0f, 0f);
            var state = rules.CreateState();
            rules.SelectRole(state, PlayerRole.Pitcher);
            rules.SelectPitch(state, 1);
            rules.BeginPitchCharge(state, 0f);
            rules.ReleasePitch(state, 0f);
            var swingTime = state.Pitch.StartTime + state.Pitch.DurationSeconds * state.Pitch.CpuSwingProgress;
            var ball = rules.SamplePitch(state.Pitch, swingTime);

            Assert.That(state.Pitch.CpuAimX, Is.EqualTo(ball.X).Within(0.001f));
            Assert.That(state.Pitch.CpuAimY, Is.EqualTo(ball.Y).Within(0.001f));
        }

        [Test]
        public void ReadyNextPitch_ReturnsToRoleSpecificSetup()
        {
            var batterRules = CreateRules();
            var batter = batterRules.CreateState();
            batterRules.SelectRole(batter, PlayerRole.Batter);
            batterRules.ApplyOutcome(batter, "strike", "STRIKE", null);

            var pitcherRules = CreateRules();
            var pitcher = pitcherRules.CreateState();
            pitcherRules.SelectRole(pitcher, PlayerRole.Pitcher);
            pitcherRules.ApplyOutcome(pitcher, "strike", "STRIKE", null);

            Assert.That(batterRules.ReadyNextPitch(batter), Is.True);
            Assert.That(pitcherRules.ReadyNextPitch(pitcher), Is.True);
            Assert.That(batter.Phase, Is.EqualTo(AtBatPhase.Ready));
            Assert.That(pitcher.Phase, Is.EqualTo(AtBatPhase.PitchSetup));
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(9)]
        public void BallTexture_IsCircularPointFilteredAndOpaqueAtCenter(int diameter)
        {
            var texture = DiamondDynastyController.CreateBallTexture(diameter);
            var center = diameter / 2;

            Assert.That(texture.width, Is.EqualTo(diameter));
            Assert.That(texture.height, Is.EqualTo(diameter));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(texture.GetPixel(0, 0).a, Is.Zero);
            Assert.That(texture.GetPixel(diameter - 1, diameter - 1).a, Is.Zero);
            Assert.That(texture.GetPixel(center, center).a, Is.EqualTo(1f));
            Assert.That(texture.GetPixel(center, center).r, Is.GreaterThan(0.9f));
            Object.DestroyImmediate(texture);
        }

        private AtBatRules CreateRules(params float[] values)
        {
            return new AtBatRules(config, new SequenceRandom(values));
        }

        private sealed class SequenceRandom : IAtBatRandom
        {
            private readonly Queue<float> values;

            public SequenceRandom(IEnumerable<float> values)
            {
                this.values = new Queue<float>(values);
            }

            public float Next01()
            {
                return values.Count > 0 ? values.Dequeue() : 0f;
            }
        }
    }
}
