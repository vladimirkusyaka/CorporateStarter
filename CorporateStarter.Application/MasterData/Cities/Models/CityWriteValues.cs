using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.MasterData.Cities.Models
{
    public sealed class CityWriteValues
    {
        public Guid? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Region { get; set; }

        public Guid CountryId { get; set; }

        public bool IsActive { get; set; }

        public void Normalize()
        {
            Name = Name.Trim();
            Region = NormalizeOptional(Region);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
