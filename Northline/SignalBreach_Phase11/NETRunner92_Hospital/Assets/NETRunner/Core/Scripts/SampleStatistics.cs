using System;
using System.Collections.Generic;
using System.Linq;

namespace NetRunner.Core
{
    /// <summary>
    /// The figures the benchmark modes write for a set of measurements, such
    /// as frame times: the mean and nearest-rank percentiles. An empty set
    /// gives <see cref="NoSamples"/>, which the CSV files show as -1 (see
    /// SampleStatisticsTests).
    /// </summary>
    public static class SampleStatistics
    {
        /// <summary>What an empty set of measurements gives.</summary>
        public const double NoSamples = -1;

        /// <summary>The average of <paramref name="samples"/>.</summary>
        public static double Mean(IEnumerable<double> samples)
        {
            double[] values = samples.ToArray();
            return values.Length == 0 ? NoSamples : values.Average();
        }

        /// <summary>
        /// The nearest-rank percentile: the smallest measurement that at least
        /// <paramref name="fraction"/> of all measurements do not exceed, that
        /// is the ceil(n × fraction)-th smallest of n. 0.95 gives the 95th percentile.
        /// </summary>
        public static double Percentile(IEnumerable<double> samples, double fraction)
        {
            double[] sorted = samples.OrderBy(value => value).ToArray();
            if (sorted.Length == 0)
            {
                return NoSamples;
            }

            int rank = (int)Math.Ceiling(sorted.Length * fraction);
            return sorted[Math.Min(sorted.Length, Math.Max(rank, 1)) - 1];
        }
    }
}
