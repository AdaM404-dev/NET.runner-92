using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of cycling through quality levels and choosing the start level.
    /// </summary>
    public sealed class QualityChoiceTests
    {
        [Test]
        public void CyclingMovesToTheNextLevel()
        {
            Assert.That(QualityChoice.Next(0, 3), Is.EqualTo(1));
            Assert.That(QualityChoice.Next(1, 3), Is.EqualTo(2));
        }

        [Test]
        public void CyclingFromTheLastLevelStartsAgainAtTheFirst()
        {
            Assert.That(QualityChoice.Next(2, 3), Is.EqualTo(0));
        }

        [Test]
        public void CyclingFromAnUnknownLevelGoesToTheFirst()
        {
            Assert.That(QualityChoice.Next(-1, 3), Is.EqualTo(0));
            Assert.That(QualityChoice.Next(7, 3), Is.EqualTo(0));
        }

        [Test]
        public void WithNoLevelsCyclingStaysAtZero()
        {
            Assert.That(QualityChoice.Next(0, 0), Is.EqualTo(0));
        }

        [Test]
        public void AValidSavedLevelIsUsedAtStart()
        {
            Assert.That(QualityChoice.StartLevel(2, 3, 1), Is.EqualTo(2));
            Assert.That(QualityChoice.StartLevel(0, 3, 1), Is.EqualTo(0));
        }

        [Test]
        public void WithoutASavedLevelTheFallbackIsUsed()
        {
            Assert.That(QualityChoice.StartLevel(-1, 3, 1), Is.EqualTo(1));
        }

        [Test]
        public void ASavedLevelThatNoLongerExistsFallsBack()
        {
            Assert.That(QualityChoice.StartLevel(5, 3, 1), Is.EqualTo(1));
        }
    }
}
