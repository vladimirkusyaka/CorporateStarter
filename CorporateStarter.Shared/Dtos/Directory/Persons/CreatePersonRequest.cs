using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Directory.Persons
{
    public sealed class CreatePersonRequest
    {
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
    }
}
