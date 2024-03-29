﻿using System.ComponentModel.DataAnnotations;
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
        public string AuthCallbackUrl { get; set; } = null!;

        public bool AuthenticationEnabled { get; set; }
    }
}
