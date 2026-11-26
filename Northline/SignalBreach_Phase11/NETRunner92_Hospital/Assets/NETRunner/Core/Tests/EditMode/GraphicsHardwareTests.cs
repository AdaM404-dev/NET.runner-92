using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of telling integrated graphics from a separate card by
    /// name. The names are the ones Intel's, AMD's and NVIDIA's drivers report
    /// on Windows, and Mesa's on Linux; the expected values follow from what
    /// each part is (Intel UHD, Iris and Arc Graphics sit in the processor,
    /// Arc A770 and B580 are cards; Radeon "… Graphics" sit in Ryzen
    /// processors, Radeon RX are cards).
    /// </summary>
    public sealed class GraphicsHardwareTests
    {
        [TestCase("Intel(R) UHD Graphics 630")]
        [TestCase("Intel(R) HD Graphics 520")]
        [TestCase("Intel(R) Iris(R) Xe Graphics")]
        [TestCase("Intel(R) Arc(TM) Graphics")]
        [TestCase("Mesa Intel(R) UHD Graphics 630 (CFL GT2)")]
        [TestCase("AMD Radeon(TM) Graphics")]
        [TestCase("AMD Radeon(TM) Vega 8 Graphics")]
        [TestCase("AMD Radeon(TM) RX Vega 11 Graphics")]
        [TestCase("AMD Radeon 780M Graphics")]
        [TestCase("AMD Radeon Graphics (RADV RENOIR)")]
        public void GraphicsBuiltIntoTheProcessorAreIntegrated(string name)
        {
            Assert.That(GraphicsHardware.IsIntegrated(name), Is.True);
        }

        [TestCase("NVIDIA GeForce RTX 4050 Laptop GPU")]
        [TestCase("NVIDIA GeForce RTX 3060 Ti")]
        [TestCase("NVIDIA GeForce GTX 1060 6GB")]
        [TestCase("Intel(R) Arc(TM) A770 Graphics")]
        [TestCase("Intel(R) Arc(TM) B580 Graphics")]
        [TestCase("AMD Radeon RX 6600")]
        [TestCase("AMD Radeon RX 7800 XT")]
        [TestCase("Radeon RX 580 Series")]
        [TestCase("AMD Radeon RX 6600 (RADV NAVI23)")]
        public void SeparateGraphicsCardsAreNotIntegrated(string name)
        {
            Assert.That(GraphicsHardware.IsIntegrated(name), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Microsoft Basic Render Driver")]
        public void AnUnknownNameCountsAsASeparateCard(string name)
        {
            Assert.That(GraphicsHardware.IsIntegrated(name), Is.False);
        }
    }
}
