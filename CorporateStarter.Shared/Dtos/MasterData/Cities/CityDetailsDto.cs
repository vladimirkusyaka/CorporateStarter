using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Cities
{
    public sealed class CityDetailsDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Region { get; set; }

        public Guid CountryId { get; set; }

        public string CountryCode { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
