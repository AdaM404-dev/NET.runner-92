using NUnit.Framework;

namespace NetRunner.Core.Tests
{
    /// <summary>
    /// EditMode tests of the benchmark statistics. Expected values follow the
    /// nearest-rank definition: the p-th percentile of n measurements is the
    /// ceil(n × p)-th smallest; an empty set gives -1 (docs/systems/performance.md).
    /// </summary>
    public sealed class SampleStatisticsTests
    {
        private static readonly double[] OneToTwenty =
        {
            7, 3, 20, 1, 14, 9, 18, 5, 11, 16, 2, 13, 19, 4, 8, 17, 6, 12, 10, 15,
        };

        [Test]
        public void The95thPercentileOfTwentyIsTheNineteenthSmallest()
        {
            Assert.That(SampleStatistics.Percentile(OneToTwenty, .95), Is.EqualTo(19));
        }

        [Test]
        public void The99thPercentileOfTwentyIsTheLargest()
        {
            Assert.That(SampleStatistics.Percentile(OneToTwenty, .99), Is.EqualTo(20));
        }

        [Test]
        public void A95thPercentileRoundsTheRankUp()
        {
            // ceil(10 × 0.95) = ceil(9.5) = 10: the largest of ten.
            Assert.That(SampleStatistics.Percentile(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, .95), Is.EqualTo(10));
        }

        [Test]
        public void OneMeasurementIsEveryPercentile()
        {
            Assert.That(SampleStatistics.Percentile(new double[] { 16.7 }, .95), Is.EqualTo(16.7));
        }

        [Test]
        public void NoMeasurementsGiveMinusOne()
        {
            Assert.That(SampleStatistics.Percentile(new double[0], .95), Is.EqualTo(-1));
            Assert.That(SampleStatistics.Mean(new double[0]), Is.EqualTo(-1));
        }

        [Test]
        public void TheMeanIsTheAverage()
        {
            Assert.That(SampleStatistics.Mean(new double[] { 1, 2, 3, 4 }), Is.EqualTo(2.5));
        }
    }
}
