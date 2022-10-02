namespace Allotment.Machine.Models
{
    public record CurrentStatus
    {
        public bool DoorsOpening { get; set; }
        public bool DoorsClosing { get; set; }
        public bool WaterOn { get; set; }
        public bool WaterSensorOn { get; set; }
        public TempDetails ?Temp { get; set; } = null;
        public string Textual { get; set; } = "";
    }
}
 