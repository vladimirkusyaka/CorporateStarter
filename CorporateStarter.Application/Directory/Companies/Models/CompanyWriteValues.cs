namespace CorporateStarter.Application.Directory.Companies.Models
{
    public sealed class CompanyWriteValues
    {
        public Guid? Id { get; set; }

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

        public bool IsActive { get; set; }

        public void Normalize()
        {
            Code = NormalizeOptional(Code);
            Name = Name.Trim();
            LegalName = NormalizeOptional(LegalName);
            TaxNumber = NormalizeOptional(TaxNumber);
            VatId = NormalizeOptional(VatId);
            Email = NormalizeOptional(Email);
            Phone = NormalizeOptional(Phone);
            Website = NormalizeOptional(Website);
            Street = NormalizeOptional(Street);
            HouseNumber = NormalizeOptional(HouseNumber);
            PostalCode = NormalizeOptional(PostalCode);
            Description = NormalizeOptional(Description);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
