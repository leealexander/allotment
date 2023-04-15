namespace Allotment.Machine.Monitoring.Models
{
    public record SolarReadingModel
    {
        public DateTime DateTakenUtc { get; set; }
        public DeviceStatus DeviceStatus { get; set; } = new();
        public ElectricalVariables SolarPanel { get; set; } = new();
        public ElectricalVariables Load { get; set; } = new();

        public Battery Battery { get; set; } = new();
    }

    public record ElectricalVariables
    {
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Watts { get; set; }
    }

    public record Battery
    {
        public double Temperature { get; set; }
        public ushort StateOfCharge { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
    }
    public record DeviceStatus
    {
        public double Temperature { get; set; }

        public StringStatusValue Charge { get; set; } = new StringStatusValue();
        public StringStatusValue Battery { get; set; } = new StringStatusValue();
        public StringStatusValue Load { get; set; } = new StringStatusValue();
        public StringStatusValue Controller { get; set; } = new StringStatusValue();
        public StringStatusValue SolarPanel { get; set; } = new StringStatusValue();
    }


    public enum Health
    {
        Unknown,
        Good,
        NotApplicable,
        Bad
    }

    public record StringStatusValue
    {
        public override string ToString()
        {
            return $"{Description}>>{Health}";
        }
        public static StringStatusValue Parse(string s)
        {
            var split = s.Split(">>");
            if(split.Length == 2)
            {
                return new StringStatusValue
                {
                    Description = split[0],
                    Health = (Health)Enum.Parse(typeof(Health), split[1])
                };
            }

            return new StringStatusValue();
        }
        public string Description { get; set; } = "Unknown";
        public Health Health { get; set; } = Health.Unknown;
    }
}