using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.MasterData.Positions.Models
{
    public sealed class PositionWriteValues
    {
        public Guid? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public void Normalize()
        {
            Name = Name.Trim();
            Description = NormalizeOptional(Description);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
