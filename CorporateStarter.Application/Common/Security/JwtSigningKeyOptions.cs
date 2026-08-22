using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class JwtSigningKeyOptions
    {
        public const string SectionName = "JwtSigningKeys";

        public string ActiveKeyId { get; set; } = string.Empty;

        public List<JwtSigningKeyDescriptor> Keys { get; set; } = [];
    }

    public sealed class JwtSigningKeyDescriptor
    {
        public string KeyId { get; set; } = string.Empty;

        public string Secret { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTime? NotBeforeUtc { get; set; }

        public DateTime? NotAfterUtc { get; set; }
    }
}
