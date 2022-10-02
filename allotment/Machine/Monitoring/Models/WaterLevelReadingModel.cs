namespace Allotment.Machine.Monitoring.Models
{
    public record WaterLevelReadingModel
    {
        public int Reading { get; set; }
        public DateTime DateTakenUtc { get; set; }
    }
}
