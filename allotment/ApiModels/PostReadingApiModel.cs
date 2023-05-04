using System.ComponentModel.DataAnnotations;

namespace Allotment.ApiModels
{
    public record PostReadingApiModel
    {
        [Required]
        public int Reading { get; set; }

        [Required]
        public int ReadingTimeUtc { get; set; }
    }
}
