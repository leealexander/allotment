using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Allotment.AppSettingsConfig
{
    public record AllotmentConfig
    {
        [Required]
        public AuthConfig Auth { get; set; } = null!;
    }

    public record AuthConfig
    {
        [Required]
        public Uri SiteUrl { get; set; } = null!;

        [Required]
        public Uri AuthCallbackUrl { get; set; } = null!;
    }
}
