using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Countries
{
    public sealed class CreateCountryRequest
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? NativeName { get; set; }

        public string? PhoneCode { get; set; }
    }
}
