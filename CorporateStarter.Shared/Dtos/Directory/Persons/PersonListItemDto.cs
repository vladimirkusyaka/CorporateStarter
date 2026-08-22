namespace CorporateStarter.Shared.Dtos.Directory.Persons
{
    public class PersonListItemDto
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

        public string CompanyName { get; set; } = string.Empty;

        public Guid? PositionId { get; set; }

        public string PositionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
