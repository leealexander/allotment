using System.ComponentModel.DataAnnotations;

namespace Allotment.ApiModels
{
    public record PostReadingApiModel
    {
        [Required]
        public int Reading { get; set; }

        [Required]
        public DateTime ReadingTimeUtc { get; set; }
    }
}
