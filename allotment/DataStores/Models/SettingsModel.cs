namespace Allotment.DataStores.Models
{
    public record SettingsModel
    {
        public WaterLevelSensorSettingsModel WaterLevelSensor { get; set; } = new ();
        public IrrigationSettingsModel Irrigation { get; set; } = new();
        public DoorsSettingsModel Doors { get; set; } = new();
    }

    public record WaterLevelSensorSettingsModel
    {
        public TimeSpan PoweredOnDuration { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan PeriodicCheckDuration { get; set; } = TimeSpan.FromMinutes(60);
    }

    public record IrrigationSettingsModel
    {
        public TimeSpan WaterOnDuration { get; set; } = TimeSpan.FromMinutes(5);
    }

    public record DoorsSettingsModel
    {
        public TimeSpan ActionPowerOnDuration { get; set; } = TimeSpan.FromSeconds(50);
    }
}
