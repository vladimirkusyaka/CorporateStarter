using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Countries
{
    public sealed class UpdateCountryRequest
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? NativeName { get; set; }

        public string? PhoneCode { get; set; }

        public bool IsActive { get; set; }
    }
}
