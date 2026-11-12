using System.Collections.Generic;
using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the west lift's car. Expected behaviour comes from
    /// docs/systems/doors-and-interaction.md: only floors the stairs reached;
    /// to its own floor it opens, to another it shuts, travels and opens
    /// there; doors stay open 7 s, longer while someone stands in the
    /// doorway, and a shutting door opens again for them; travel up to
    /// 2.25 m/s at 2.1 m/s², never slower than 0.15 m/s; a cabin button
    /// needs a passenger.
    /// </summary>
    public sealed class LiftCarTests
    {
        private const float Frame = 1f / 60f;
        private const float FloorHeight = 4.2f;

        private LiftCar car;
        private List<bool> commands;

        [SetUp]
        public void Build()
        {
            car = new LiftCar();
            commands = new List<bool>();
            car.DoorsCommanded += open => commands.Add(open);
        }

        [Test]
        public void ItStartsClosedOnL1()
        {
            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Closed));
            Assert.That(car.Floor, Is.EqualTo(0));
        }

        [Test]
        public void OnlyFloorsTheStairsReachedCanBeChosen()
        {
            Assert.That(car.Request(1, 0, false, false, false), Is.False);
            Assert.That(LiftCar.IsReached(0, 0), Is.True);
            Assert.That(LiftCar.IsReached(2, 2), Is.True);
            Assert.That(LiftCar.IsReached(3, 2), Is.False);
            Assert.That(LiftCar.IsReached(-1, 3), Is.False);
        }

        [Test]
        public void CalledToItsOwnFloorItOpensItsDoors()
        {
            bool accepted = car.Request(0, 0, false, false, false);

            Assert.That(accepted, Is.True);
            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Opening));
            Assert.That(commands, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void TheDoorsStayOpenSevenSecondsThenShut()
        {
            car.Request(0, 0, false, false, false);
            car.DoorsMoved(true, false);

            Wait(6.9f, false);
            LiftCar.State stillOpen = car.Current;
            Wait(.2f, false);

            Assert.That(stillOpen, Is.EqualTo(LiftCar.State.Open));
            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Closing));
            Assert.That(commands, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void SomeoneInTheDoorwayKeepsTheDoorsOpen()
        {
            car.Request(0, 0, false, false, false);
            car.DoorsMoved(true, false);

            Wait(6f, false);
            Wait(Frame, true);
            Wait(6.5f, false);

            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Open));
        }

        [Test]
        public void ShutDoorsOnItsOwnFloorLeaveItStandingClosed()
        {
            car.Request(0, 0, false, false, false);
            car.DoorsMoved(true, false);
            Wait(7.1f, false);

            car.DoorsMoved(false, true);

            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Closed));
        }

        [Test]
        public void ToAnotherFloorItShutsTravelsAndOpensThere()
        {
            bool accepted = car.Request(1, 1, false, false, false);
            LiftCar.State shutting = car.Current;
            car.DoorsMoved(false, true);
            LiftCar.State leaving = car.Current;
            float seconds = Travel(0f);

            Assert.That(accepted, Is.True);
            Assert.That(shutting, Is.EqualTo(LiftCar.State.Closing));
            Assert.That(leaving, Is.EqualTo(LiftCar.State.Moving));
            Assert.That(car.Floor, Is.EqualTo(1));
            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Opening));
            Assert.That(commands, Is.EqualTo(new[] { false, true }));

            // About 2.8 s for one floor: 1.07 s to reach 2.25 m/s and 0.8 s at that speed; slowing down
            // takes a little less than 1.07 s, because the speed sheds at most 2.1 m/s² and so trails
            // just above the braking curve.
            Assert.That(seconds, Is.InRange(2.7f, 2.95f));
        }

        [Test]
        public void ItNeverTravelsFasterThanTwoAndAQuarterMetresPerSecond()
        {
            car.Request(3, 3, false, false, false);
            car.DoorsMoved(false, true);
            float height = 0f;
            float fastest = 0f;
            for (int frame = 0; frame < 2000 && car.Current == LiftCar.State.Moving; frame++)
            {
                float next = car.Travel(height, Frame);
                fastest = System.Math.Max(fastest, (next - height) / Frame);
                height = next;
            }

            Assert.That(height, Is.EqualTo(3 * FloorHeight).Within(1e-4f));
            Assert.That(fastest, Is.EqualTo(2.25f).Within(1e-3f));
        }

        [Test]
        public void WhileShuttingOrTravellingItTakesNoRequests()
        {
            car.Request(1, 1, false, false, false);

            Assert.That(car.Busy, Is.True);
            Assert.That(car.Request(0, 1, false, false, false), Is.False);
            car.DoorsMoved(false, true);
            Assert.That(car.Busy, Is.True);
            Assert.That(car.Request(0, 1, false, false, false), Is.False);
        }

        [Test]
        public void ItDoesNotLeaveWhileSomeoneStandsInTheDoorwayOrTheDoorsAreStillOpening()
        {
            Assert.That(car.Request(1, 1, false, false, true), Is.False);
            car.Request(0, 1, false, false, false);
            Assert.That(car.Request(1, 1, false, false, false), Is.False);
        }

        [Test]
        public void ADoorShuttingOnSomeoneOpensAgainAndTheCarStays()
        {
            car.Request(1, 1, false, false, false);

            car.Reopen();

            Assert.That(car.Current, Is.EqualTo(LiftCar.State.Opening));
            Assert.That(car.Destination, Is.EqualTo(0));
            Assert.That(commands, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void ACabinButtonWorksOnlyForAPassenger()
        {
            Assert.That(car.Request(0, 0, true, false, false), Is.False);
            Assert.That(car.Request(0, 0, true, true, false), Is.True);
        }

        [Test]
        public void TheButtonsSayWhatEDoes()
        {
            Assert.That(LiftCar.ButtonPrompt(2, false, false, false), Is.EqualTo("L3  Reach this level by stairs"));
            Assert.That(LiftCar.ButtonPrompt(0, true, true, true), Is.EqualTo("Lift in transit"));
            Assert.That(LiftCar.ButtonPrompt(1, true, false, true), Is.EqualTo("E  Select level 2"));
            Assert.That(LiftCar.ButtonPrompt(1, true, false, false), Is.EqualTo("E  Call lift to level 2"));
        }

        [Test]
        public void ThePassengerStandsOnTheCabinFloor()
        {
            Assert.That(LiftCar.Carries(0f, 0f, 0f), Is.True);
            Assert.That(LiftCar.Carries(1.27f, .21f, 1.51f), Is.True);
            Assert.That(LiftCar.Carries(1.29f, 0f, 0f), Is.False);
            Assert.That(LiftCar.Carries(0f, 0f, -1.35f), Is.False);
            Assert.That(LiftCar.Carries(0f, .23f, 0f), Is.False);
        }

        private void Wait(float seconds, bool doorwayBlocked)
        {
            car.Wait(seconds, doorwayBlocked);
        }

        // Travels from the height given until the car arrives, and returns the seconds it took.
        private float Travel(float height)
        {
            float seconds = 0f;
            for (int frame = 0; frame < 2000 && car.Current == LiftCar.State.Moving; frame++)
            {
                height = car.Travel(height, Frame);
                seconds += Frame;
            }

            return seconds;
        }
    }
}
