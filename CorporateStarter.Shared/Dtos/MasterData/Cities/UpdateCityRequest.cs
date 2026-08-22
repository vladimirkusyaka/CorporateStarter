using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Cities
{
    public sealed class UpdateCityRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Region { get; set; }

        public Guid CountryId { get; set; }

        public bool IsActive { get; set; }
    }
}
