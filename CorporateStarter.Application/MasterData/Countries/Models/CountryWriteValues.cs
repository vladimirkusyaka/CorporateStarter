using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection.Emit;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Application.MasterData.Countries.Models
{
    public sealed class CountryWriteValues
    {
        public Guid? Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? NativeName { get; set; }

        public string? PhoneCode { get; set; }

        public bool IsActive { get; set; }

        public void Normalize()
        {
            Name = Name.Trim();
            Code = Code.Trim();
            NativeName = NormalizeOptional(NativeName);
            PhoneCode = NormalizeOptional(PhoneCode);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
