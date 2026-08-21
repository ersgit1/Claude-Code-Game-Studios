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
