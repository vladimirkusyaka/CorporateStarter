using System.ComponentModel.DataAnnotations;
using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Directory.Companies
{
    public sealed class UpdateCompanyRequest
    {
        [StringLength(100)]
        public string? Code { get; set; }

        [Required, StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(256)]
        public string? LegalName { get; set; }

        [StringLength(100)]
        public string? TaxNumber { get; set; }

        [StringLength(100)]
        public string? VatId { get; set; }

        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(256)]
        public string? Website { get; set; }

        [StringLength(256)]
        public string? Street { get; set; }

        [StringLength(50)]
        public string? HouseNumber { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        public Guid? CityId { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
