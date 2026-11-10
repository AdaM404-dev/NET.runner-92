using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of reading launch options.
    /// </summary>
    public sealed class LaunchArgumentsTests
    {
        [Test]
        public void TheWordAfterTheOptionIsItsValue()
        {
            string[] arguments = { "NET.runner-92.exe", "-net92-benchmark", "-net92-quality", "Low" };

            Assert.That(LaunchArguments.ValueAfter(arguments, "-net92-quality"), Is.EqualTo("Low"));
        }

        [Test]
        public void AMissingOptionHasNoValue()
        {
            string[] arguments = { "NET.runner-92.exe", "-net92-benchmark" };

            Assert.That(LaunchArguments.ValueAfter(arguments, "-net92-quality"), Is.Null);
        }

        [Test]
        public void AnOptionAtTheEndHasNoValue()
        {
            string[] arguments = { "NET.runner-92.exe", "-net92-quality" };

            Assert.That(LaunchArguments.ValueAfter(arguments, "-net92-quality"), Is.Null);
        }

        [Test]
        public void NoArgumentsHaveNoValue()
        {
            Assert.That(LaunchArguments.ValueAfter(null, "-net92-quality"), Is.Null);
        }
    }
}
