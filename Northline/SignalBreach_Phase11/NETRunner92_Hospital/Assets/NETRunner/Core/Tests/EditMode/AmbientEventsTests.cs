using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the timed atmosphere events, stepped every 1/60 s.
    /// The numbers come from docs/systems/atmosphere.md: the first flash
    /// after the given delay; a flash of 34·e^(−22t) + 17·e^(−65|t − 0.19|)
    /// lasting 0.7 s; the next flash 20–39 s after one ends; thunder once per
    /// flash, 0.65–1.5 s after the strike; the monitor first after 4.5 s, then
    /// every 7–16 s, heard only above blend 0.45, 3–6.5 m to the right; a
    /// creak first after 8 s, then every 10–23 s.
    /// </summary>
    public sealed class AmbientEventsTests
    {
        // What happened, and when, in one run.
        private sealed class Record
        {
            public List<float> Strikes { get; } = new List<float>();
            public List<float> FlashEnds { get; } = new List<float>();
            public List<float> Thunder { get; } = new List<float>();
            public List<float> Beeps { get; } = new List<float>();
            public List<float> BeepSides { get; } = new List<float>();
            public List<float> Creaks { get; } = new List<float>();
            public float FirstLightning { get; set; }

            // How many of the strikes came early enough for their flash and thunder to be over by the end.
            public int FinishedStrikes { get; set; }
        }

        private const float Frame = 1f / 60f;

        // Times measured on a 1/60 s clock land up to one frame after the moment they were timed for.
        private const float Late = Frame + 1e-4f;

        [Test]
        public void EventsNeedARandomStream()
        {
            Assert.Throws<ArgumentNullException>(() => new AmbientEvents(null, 0f, 15f));
        }

        [Test]
        public void AFlashIsThirtyFourAtTheStrikeFadesFastAndFlickersAgainAfterNineteenHundredths()
        {
            Assert.That(AmbientEvents.FlashIntensity(0f), Is.EqualTo(34.0000736f).Within(1e-3f));
            Assert.That(AmbientEvents.FlashIntensity(0.1f), Is.EqualTo(3.8162657f).Within(1e-3f));
            Assert.That(AmbientEvents.FlashIntensity(0.19f), Is.EqualTo(17.5201493f).Within(1e-3f));
            Assert.That(AmbientEvents.FlashIntensity(0.5f), Is.EqualTo(0.000568f).Within(1e-5f));
        }

        [Test]
        public void ThereIsNoFlashBeforeTheStrikeOrAfterSevenTenthsOfASecond()
        {
            Assert.That(AmbientEvents.FlashIntensity(-0.01f), Is.EqualTo(0f));
            Assert.That(AmbientEvents.FlashIntensity(0.7f), Is.EqualTo(0f));
            Assert.That(AmbientEvents.FlashIntensity(3f), Is.EqualTo(0f));
        }

        [Test]
        public void TheFirstFlashStrikesAfterTheDelay()
        {
            // Started at 100 s with a 15 s delay.
            var record = Run(new AmbientEvents(new Random(2087), 100f, 15f), 100f, 20f, 0f);

            Assert.That(record.Strikes[0], Is.EqualTo(115f).Within(Late));
            Assert.That(record.FirstLightning, Is.EqualTo(34f).Within(0.01f));
        }

        [Test]
        public void EachFlashLastsSevenTenthsOfASecond()
        {
            var record = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 300f, 0f);

            Assert.That(record.FinishedStrikes, Is.GreaterThan(4));
            for (int i = 0; i < record.FinishedStrikes; i++)
            {
                Assert.That(record.FlashEnds[i] - record.Strikes[i], Is.EqualTo(0.7f).Within(Late));
            }
        }

        [Test]
        public void TheNextFlashComesTwentyToThirtyNineSecondsAfterOneEnds()
        {
            var record = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 300f, 0f);

            for (int i = 1; i < record.Strikes.Count; i++)
            {
                Assert.That(record.Strikes[i] - record.FlashEnds[i - 1], Is.InRange(20f, 39f + Late));
            }
        }

        [Test]
        public void ThunderFollowsEachStrikeOnceAfterSixtyFiveHundredthsToOneAndAHalfSeconds()
        {
            var record = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 300f, 0f);

            Assert.That(record.Thunder[0], Is.GreaterThan(record.Strikes[0]), "Thunder before the first strike");
            for (int i = 0; i < record.FinishedStrikes; i++)
            {
                float next = i + 1 < record.Strikes.Count ? record.Strikes[i + 1] : float.PositiveInfinity;
                var claps = record.Thunder.FindAll(time => time >= record.Strikes[i] && time < next);
                Assert.That(claps.Count, Is.EqualTo(1), $"Thunderclaps after the strike at {record.Strikes[i]} s");
                Assert.That(claps[0] - record.Strikes[i], Is.InRange(0.65f, 1.5f + Late));
            }
        }

        [Test]
        public void InsideTheMonitorBeepsFirstAfterFourAndAHalfSecondsThenEverySevenToSixteen()
        {
            var record = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 200f, 1f);

            Assert.That(record.Beeps[0], Is.EqualTo(4.5f).Within(Late));
            Assert.That(record.Beeps.Count, Is.GreaterThan(10));
            for (int i = 1; i < record.Beeps.Count; i++)
            {
                Assert.That(record.Beeps[i] - record.Beeps[i - 1], Is.InRange(7f, 16f + Late));
            }
        }

        [Test]
        public void TheMonitorBeepsThreeToSixAndAHalfMetresToTheRight()
        {
            var record = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 200f, 1f);

            foreach (float side in record.BeepSides)
            {
                Assert.That(side, Is.InRange(3f, 6.5f));
            }
        }

        [Test]
        public void TheMonitorIsHeardOnlyAboveBlendFortyFiveHundredths()
        {
            var atThreshold = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 200f, 0.45f);
            var justAbove = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 200f, 0.46f);

            Assert.That(atThreshold.Beeps, Is.Empty);
            Assert.That(justAbove.Beeps, Is.Not.Empty);
        }

        [Test]
        public void TheStructureCreaksFirstAfterEightSecondsThenEveryTenToTwentyThreeInsideOrOut()
        {
            foreach (float blend in new[] { 0f, 1f })
            {
                var record = Run(new AmbientEvents(new Random(2087), 0f, 15f), 0f, 200f, blend);

                Assert.That(record.Creaks[0], Is.EqualTo(8f).Within(Late));
                Assert.That(record.Creaks.Count, Is.GreaterThan(7));
                for (int i = 1; i < record.Creaks.Count; i++)
                {
                    Assert.That(record.Creaks[i] - record.Creaks[i - 1], Is.InRange(10f, 23f + Late));
                }
            }
        }

        // Steps the events every 1/60 s for the given seconds and notes when each thing happened.
        private static Record Run(AmbientEvents events, float start, float seconds, float blend)
        {
            var record = new Record();
            bool flashing = false;
            int frames = (int)(seconds * 60f);
            for (int i = 0; i <= frames; i++)
            {
                float now = start + i / 60f;
                events.Step(now, blend);
                if (events.Lightning > 0f && !flashing)
                {
                    record.Strikes.Add(now);
                    if (record.Strikes.Count == 1)
                    {
                        record.FirstLightning = events.Lightning;
                    }
                }
                else if (events.Lightning == 0f && flashing)
                {
                    record.FlashEnds.Add(now);
                }

                flashing = events.Lightning > 0f;
                if (events.ThunderStarts)
                {
                    record.Thunder.Add(now);
                }

                if (events.MonitorBeeps)
                {
                    record.Beeps.Add(now);
                    record.BeepSides.Add(events.MonitorSide);
                }

                if (events.CreakSounds)
                {
                    record.Creaks.Add(now);
                }
            }

            // A flash, and its thunder, are over within 2 s of the strike; one struck later may still be going.
            float cutoff = start + seconds - 2f;
            record.FinishedStrikes = record.Strikes.FindAll(time => time <= cutoff).Count;
            return record;
        }
    }
}
