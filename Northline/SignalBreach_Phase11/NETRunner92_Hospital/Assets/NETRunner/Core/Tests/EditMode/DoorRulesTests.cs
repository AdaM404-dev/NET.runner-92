using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the hospital doors. Expected values come from
    /// docs/systems/doors-and-interaction.md: swing doors open in 1/1.9 s,
    /// sliding doors in 1/1.55 s; the doorway reaches 0.28 m beyond the
    /// opening, 0.5 m up and down, and 1.32 m (swing) or 0.43 m (slide) deep.
    /// The prompts are the Phase 11 HUD's Hungarian wording; the HUD shows
    /// the key bound to the prompt's action, and no key when it has none.
    /// </summary>
    public sealed class DoorRulesTests
    {
        private const float Width = 1.2f;

        [Test]
        public void ThePromptOffersTheInteractKeyToOpenOrClose()
        {
            Assert.That(DoorRules.Prompt(false, false, false, false),
                Is.EqualTo(((GameAction?)GameAction.Interact, "Ajtó kinyitása")));
            Assert.That(DoorRules.Prompt(false, false, false, true),
                Is.EqualTo(((GameAction?)GameAction.Interact, "Ajtó bezárása")));
        }

        [Test]
        public void LockedLiftAndOfflineDoorsSaySoAndOfferNoKey()
        {
            Assert.That(DoorRules.Prompt(false, false, true, false), Is.EqualTo(((GameAction?)null, "Zárva")));
            Assert.That(DoorRules.Prompt(false, true, false, false),
                Is.EqualTo(((GameAction?)null, "Használd a lift hívógombját")));
            Assert.That(DoorRules.Prompt(true, true, false, false), Is.EqualTo(((GameAction?)null, "A lift üzemen kívül")));
        }

        [Test]
        public void ENeverMovesALiftDoorOrALockedDoor()
        {
            Assert.That(DoorRules.AnswersUse(true, false, false, false), Is.False);
            Assert.That(DoorRules.AnswersUse(false, true, false, false), Is.False);
        }

        [Test]
        public void EDoesNotCloseADoorOnSomeoneInTheDoorwayButOpensIt()
        {
            Assert.That(DoorRules.AnswersUse(false, false, true, true), Is.False);
            Assert.That(DoorRules.AnswersUse(false, false, false, true), Is.True);
            Assert.That(DoorRules.AnswersUse(false, false, true, false), Is.True);
        }

        [Test]
        public void ASwingDoorOpensInJustOverHalfASecond()
        {
            float early = DoorRules.Advance(0f, true, true, .52f);
            float done = DoorRules.Advance(0f, true, true, .53f);

            Assert.That(early, Is.LessThan(1f));
            Assert.That(done, Is.EqualTo(1f));
        }

        [Test]
        public void ASlidingDoorOpensInAboutTwoThirdsOfASecond()
        {
            Assert.That(DoorRules.Advance(0f, true, false, .5f), Is.EqualTo(.775f).Within(1e-5f));
            Assert.That(DoorRules.Advance(0f, true, false, .65f), Is.EqualTo(1f));
        }

        [Test]
        public void ADoorNoLongerAskedToBeOpenCloses()
        {
            Assert.That(DoorRules.Advance(1f, false, true, .25f), Is.EqualTo(1f - .475f).Within(1e-5f));
        }

        [Test]
        public void TheDoorwayIsAsDeepAsTheLeafSwings()
        {
            Assert.That(DoorRules.InDoorway(0f, 0f, 1.3f, Width, true), Is.True);
            Assert.That(DoorRules.InDoorway(0f, 0f, 1.34f, Width, true), Is.False);
            Assert.That(DoorRules.InDoorway(0f, 0f, .42f, Width, false), Is.True);
            Assert.That(DoorRules.InDoorway(0f, 0f, .44f, Width, false), Is.False);
        }

        [Test]
        public void TheDoorwayReachesALittleBeyondTheOpening()
        {
            Assert.That(DoorRules.InDoorway(.87f, 0f, 0f, Width, true), Is.True);
            Assert.That(DoorRules.InDoorway(.89f, 0f, 0f, Width, true), Is.False);
        }

        [Test]
        public void OnAnotherFloorThePlayerIsNotInTheDoorway()
        {
            Assert.That(DoorRules.InDoorway(0f, .49f, 0f, Width, true), Is.True);
            Assert.That(DoorRules.InDoorway(0f, -.51f, 0f, Width, true), Is.False);
        }
    }
}
