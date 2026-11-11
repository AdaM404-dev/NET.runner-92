using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the preset-to-level mapping. Expected values come
    /// from Samuel's decision on #55 (2026-11-12): Low, Medium and High select
    /// the levels of the same name with 50, 67 and 100 % render scale, Ultra
    /// uses High, and Custom keeps the level already chosen.
    /// </summary>
    public sealed class QualityPresetTests
    {
        [TestCase(QualityPreset.Low, 0)]
        [TestCase(QualityPreset.Medium, 1)]
        [TestCase(QualityPreset.High, 2)]
        [TestCase(QualityPreset.Ultra, 2)]
        public void EachPresetSelectsItsLevel(int preset, int level)
        {
            Assert.That(QualityPreset.LevelFor(preset, 0), Is.EqualTo(level));
        }

        [Test]
        public void CustomKeepsTheLevelAlreadyChosen()
        {
            Assert.That(QualityPreset.LevelFor(QualityPreset.Custom, 1), Is.EqualTo(1));
        }

        [Test]
        public void CustomNeverSelectsALevelThatDoesNotExist()
        {
            Assert.That(QualityPreset.LevelFor(QualityPreset.Custom, 5), Is.EqualTo(2));
            Assert.That(QualityPreset.LevelFor(QualityPreset.Custom, -1), Is.EqualTo(0));
        }

        [TestCase(QualityPreset.Low, 50)]
        [TestCase(QualityPreset.Medium, 67)]
        [TestCase(QualityPreset.High, 100)]
        [TestCase(QualityPreset.Ultra, 100)]
        public void EachPresetStartsAtTheMeasuredRenderScale(int preset, int percent)
        {
            Assert.That(QualityPreset.RenderScaleFor(preset), Is.EqualTo(percent));
        }

        [Test]
        public void CustomHasNoRenderScaleOfItsOwn()
        {
            Assert.That(QualityPreset.RenderScaleFor(QualityPreset.Custom), Is.EqualTo(-1));
        }

        [Test]
        public void CyclingTheLevelsPicksThePresetOfTheSameName()
        {
            Assert.That(QualityPreset.ForLevel(0), Is.EqualTo(QualityPreset.Low));
            Assert.That(QualityPreset.ForLevel(2), Is.EqualTo(QualityPreset.High));
        }
    }
}
