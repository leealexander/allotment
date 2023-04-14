namespace EpeverReader.Stats
{
    public record PowerAndTemp
    {
        public ElectricalVariables SolarPanel { get; set; }
        public ElectricalVariables Load { get; set; }

        public Battery Battery { get; set; }
        public Device Device { get; set; }
    }

    public struct ElectricalVariables
    {
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Watts { get; set; }
    }

    public struct Battery
    {
        public double Temperature { get; set; }
        public ushort StateOfCharge { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
    }
    public struct Device
    {
        public double Temperature { get; set; }
    }
}
