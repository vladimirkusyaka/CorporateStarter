namespace CorporateStarter.Shared.Dtos.Directory.Persons
{
    public sealed class PersonDetailsDto
    {
        public Guid Id { get; set; }

        public string? Code { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public Guid? CompanyId { get; set; }

        public string? CompanyCode { get; set; }

        public string? CompanyName { get; set; }

        public Guid? PositionId { get; set; }

        public string? PositionName { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
