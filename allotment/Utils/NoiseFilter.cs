namespace Allotment.Utils
{
    public static class NoiseFilter
    {
        public static int[] RemoveNoise(this IEnumerable<int> readings)
        {
            // Calculate average of all numbers
            var average = readings.Average();

            // Calculate standard deviation of all numbers
            var variance = readings.Select(num => Math.Pow(num - average, 2)).Average();
            var stdev = Math.Sqrt(variance);

            // Remove noise numbers (defined as any number more than 1 standard deviation from the mean)
            return readings.Where(num => Math.Abs(num - average) <= stdev).ToArray();
        }
    }
}
