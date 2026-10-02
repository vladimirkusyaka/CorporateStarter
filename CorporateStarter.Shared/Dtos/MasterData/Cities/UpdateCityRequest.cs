using System.ComponentModel.DataAnnotations;
using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Cities
{
    public sealed class UpdateCityRequest
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Region { get; set; }

        public Guid CountryId { get; set; }

        public bool IsActive { get; set; }
    }
}
