using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of reading and writing the upscaler a benchmark run uses
    /// (<c>-net92-upscaler</c>, #36 round 3): the names default, catmull-rom
    /// and fsr1, nothing given means the pipeline's default, and an unknown
    /// name is refused.
    /// </summary>
    public sealed class UpscalerListTests
    {
        [TestCase("default", Upscaler.Default)]
        [TestCase("catmull-rom", Upscaler.CatmullRom)]
        [TestCase("fsr1", Upscaler.Fsr1)]
        public void EachUpscalerHasItsOwnName(string name, Upscaler expected)
        {
            Assert.That(UpscalerList.TryParse(name, out Upscaler upscaler), Is.True);
            Assert.That(upscaler, Is.EqualTo(expected));
        }

        [Test]
        public void NamesIgnoreLetterCaseAndTheSpacesAroundThem()
        {
            Assert.That(UpscalerList.TryParse(" FSR1 ", out Upscaler upscaler), Is.True);
            Assert.That(upscaler, Is.EqualTo(Upscaler.Fsr1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void NoNameMeansThePipelinesDefault(string name)
        {
            Assert.That(UpscalerList.TryParse(name, out Upscaler upscaler), Is.True);
            Assert.That(upscaler, Is.EqualTo(Upscaler.Default));
        }

        [Test]
        public void AnUnknownNameIsRefusedAndKeepsTheDefault()
        {
            Assert.That(UpscalerList.TryParse("fsr2", out Upscaler upscaler), Is.False);
            Assert.That(upscaler, Is.EqualTo(Upscaler.Default));
        }

        [TestCase(Upscaler.Default, "default")]
        [TestCase(Upscaler.CatmullRom, "catmull-rom")]
        [TestCase(Upscaler.Fsr1, "fsr1")]
        public void EachUpscalerIsWrittenAsTheNameThatReadsIt(Upscaler upscaler, string name)
        {
            Assert.That(UpscalerList.Format(upscaler), Is.EqualTo(name));
        }
    }
}
