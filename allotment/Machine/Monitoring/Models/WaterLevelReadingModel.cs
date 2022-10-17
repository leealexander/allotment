namespace Allotment.Machine.Monitoring.Models
{
    public record WaterLevelReadingModel
    {
        public static readonly IComparer<WaterLevelReadingModel> ReadingsComparer = new ReadingComparer();
        public int? KnownDepthCm { get; set; }
        public int Reading { get; set; }
        public DateTime DateTakenUtc { get; set; }

        private class ReadingComparer : IComparer<WaterLevelReadingModel>
        {
            public int Compare(WaterLevelReadingModel? x, WaterLevelReadingModel? y)
            {
                var xReading = x == null ? 0 : x.Reading;
                var yReading = y == null ? 0 : y.Reading;
                return xReading - yReading;
            }
        }
    }
}
