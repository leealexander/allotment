namespace EpeverReader.Stats.Helper
{
    public record StringStatusValue
    {
        public string Description { get; set; } = "Unknown";
        public Health Health { get; set; } = Health.Unknown;
    }
}
