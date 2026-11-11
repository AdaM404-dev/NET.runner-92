using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the K-7 cannon's timing, from the Phase 11 cannon:
    /// it deploys in 0.48 s, charges 0.85 s on target, fires for 25 damage,
    /// shows the pulse for 0.12 s and recovers for 1.45 s.
    /// </summary>
    public sealed class CannonCycleTests
    {
        [Test]
        public void SeeingThePlayerDeploysItInZeroPointFourEightSeconds()
        {
            var cannon = new CannonCycle();

            cannon.Step(.24f, true);
            float half = cannon.Deployment;
            cannon.Step(.24f, true);

            Assert.That(half, Is.EqualTo(.5f).Within(1e-4f));
            Assert.That(cannon.Deployment, Is.EqualTo(1f));
        }

        [Test]
        public void LosingThePlayerStowsItAtTheSameSpeed()
        {
            var cannon = Deployed();

            cannon.Step(.24f, false);

            Assert.That(cannon.Deployment, Is.EqualTo(.5f).Within(1e-4f));
        }

        [Test]
        public void TheHatchesOpenBeforeTheEmittersSlideOut()
        {
            var cannon = new CannonCycle();

            // 0.26 of the deployment: the hatches are moving, the emitters have not started.
            cannon.Step(.26f * CannonCycle.DeploySeconds, true);

            Assert.That(cannon.HatchOpening, Is.GreaterThan(0f));
            Assert.That(cannon.EmitterExtension, Is.EqualTo(0f));
        }

        [Test]
        public void ItCannotChargeUntilFullyDeployed()
        {
            var cannon = new CannonCycle();
            cannon.Step(.2f, true);

            bool fired = cannon.Charge(1f, true);

            Assert.That(fired, Is.False);
        }

        [Test]
        public void OnTargetItFiresAfterZeroPointEightFiveSeconds()
        {
            var cannon = Deployed();

            bool early = cannon.Charge(.45f, true);
            bool fired = cannon.Charge(.45f, true);

            Assert.That(early, Is.False);
            Assert.That(fired, Is.True);
            Assert.That(cannon.PulseVisible, Is.True);
        }

        [Test]
        public void LosingTheAimDropsTheCharge()
        {
            var cannon = Deployed();
            cannon.Charge(.8f, true);

            cannon.Charge(.01f, false);
            bool fired = cannon.Charge(.1f, true);

            Assert.That(fired, Is.False);
        }

        [Test]
        public void AfterFiringItRecoversForOnePointFourFiveSeconds()
        {
            var cannon = Deployed();
            cannon.Charge(.9f, true);

            cannon.Step(1.4f, true);
            bool stillRecovering = !cannon.CanCharge;
            cannon.Step(.06f, true);

            Assert.That(stillRecovering, Is.True);
            Assert.That(cannon.CanCharge, Is.True);
        }

        [Test]
        public void ThePulseShowsForZeroPointOneTwoSeconds()
        {
            var cannon = Deployed();
            cannon.Charge(.9f, true);

            cannon.Step(.1f, true);
            bool shown = cannon.PulseVisible;
            cannon.Step(.03f, true);

            Assert.That(shown, Is.True);
            Assert.That(cannon.PulseVisible, Is.False);
        }

        [Test]
        public void ABreachStowsItAtOnce()
        {
            var cannon = Deployed();

            cannon.Suspend();

            Assert.That(cannon.Deployment, Is.EqualTo(0f));
            Assert.That(cannon.HatchOpening, Is.EqualTo(0f));
        }

        private static CannonCycle Deployed()
        {
            var cannon = new CannonCycle();
            cannon.Step(1f, true);
            return cannon;
        }
    }
}
