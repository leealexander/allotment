namespace Allotment.Iot.Models
{
    public class CurrentStatus
    {
        public bool DoorsOpening { get; set; }
        public bool DoorsClosing { get; set; }
        public bool WaterOn { get; set; }
        public TempDetails Temp { get; set; }
        public string Textual { get; set; }
    }
}
