using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Application.Directory.Persons.Models
{
    public sealed class PersonWriteValues
    {
        public Guid? Id { get; set; }

        public string? Code { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public Guid? CompanyId { get; set; }

        public Guid? PositionId { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public void Normalize()
        {
            Code = NormalizeOptional(Code);
            FirstName = FirstName.Trim();
            MiddleName = NormalizeOptional(MiddleName);
            LastName = NormalizeOptional(LastName);
            Email = NormalizeOptional(Email);
            Phone = NormalizeOptional(Phone);
            Description = NormalizeOptional(Description);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
