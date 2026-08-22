using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Directory.Companies
{
    public sealed class CreateCompanyRequest
    {
        public string? Code { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? LegalName { get; set; }

        public string? TaxNumber { get; set; }

        public string? VatId { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Website { get; set; }

        public string? Street { get; set; }

        public string? HouseNumber { get; set; }

        public string? PostalCode { get; set; }

        public Guid? CityId { get; set; }

        public string? Description { get; set; }
    }
}
