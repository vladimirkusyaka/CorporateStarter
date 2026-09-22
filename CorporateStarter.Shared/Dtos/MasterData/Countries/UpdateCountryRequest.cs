using System.ComponentModel.DataAnnotations;
using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Countries
{
    public sealed class UpdateCountryRequest
    {
        [Required(ErrorMessage = "Code is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Code must contain exactly two Latin letters.")]
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? NativeName { get; set; }

        public string? PhoneCode { get; set; }

        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsActive { get; set; }
    }
}
