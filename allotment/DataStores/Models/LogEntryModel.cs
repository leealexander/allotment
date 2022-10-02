namespace Allotment.DataStores.Models
{
    public class LogEntryModel
    {
        public DateTime ?EventDateUtc { get; set; }
        public string ?Area { get; set; }
        public string ?Message { get; set; }
    }
}
