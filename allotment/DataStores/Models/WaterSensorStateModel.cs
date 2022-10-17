using Allotment.Machine.Monitoring.Models;

namespace Allotment.DataStores.Models
{
    public record WaterSensorStateModel
    {
        public WaterLevelReadingModel ?LastReading { get; set; }
        public List<WaterLevelReadingModel> KnownReadings { get; set; } = new List<WaterLevelReadingModel>();
    }
}
