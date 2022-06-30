using UnitsNet;

namespace Allotment.Iot.Models
{
    public record TempDetails
    {
        public DateTime TimeTakenUtc { get; set; }
        public Temperature Temperature { get; set; }

        public RelativeHumidity Humidity { get; set; }
    }
}
