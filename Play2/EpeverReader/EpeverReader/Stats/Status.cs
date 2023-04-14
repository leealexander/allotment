using EpeverReader.Stats.Helper;

namespace EpeverReader.Stats
{
    public record Status
    {
        public StringStatusValue Charge { get; set; } = new StringStatusValue();
        public StringStatusValue Battery { get; set; } = new StringStatusValue();
        public StringStatusValue Load { get; set; } = new StringStatusValue();
        public StringStatusValue Controller { get; set; } = new StringStatusValue();
        public StringStatusValue SolarPanel { get; set; } = new StringStatusValue();
    }
}
