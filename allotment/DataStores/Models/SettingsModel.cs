﻿namespace Allotment.DataStores.Models
{
    public record SettingsModel
    {
        public DoorsSettingsModel Doors { get; set; } = new();
        public IrrigationSettingsModel Irrigation { get; set; } = new();
        public string ApiJwtSecret { get; set; } = Guid.NewGuid().ToString();
        public SolarChargerSettingsModel SolarChargerSettingsModel { get; set; } = new();
    }
    public record DoorsSettingsModel
    {
        public TimeSpan ActionPowerOnDuration { get; set; } = TimeSpan.FromSeconds(50);
    }

    public record SolarChargerSettingsModel
    {
        public string SerialAddress { get; set; } = "/dev/ttyACM0";
        public int BaudRate { get; set; } = 115200;
    }

    public record IrrigationSettingsModel
    {
        public TimeSpan WaterOnDuration { get; set; } = TimeSpan.FromMinutes(5);
        public WaterLevelSensorSettingsModel WaterLevelSensor { get; set; } = new();
    }

    public record WaterLevelSensorSettingsModel
    {
        public int WaterSourceMaxDepthCm { get; set; } = 100;

        public TimeSpan PoweredOnDuration { get; set; } = TimeSpan.FromMinutes(3);
        public TimeSpan PeriodicCheckDuration { get; set; } = TimeSpan.FromMinutes(60);
        public int MaxDevianceBetweenReadingsAllowed { get; set; } = 5; // taken from various tests of the sensor
        public int MinReadingsPerSensonOnSession { get; set; } = 5;
    }

}
