using System.ComponentModel.DataAnnotations;
using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.Directory.Persons
{
    public sealed class UpdatePersonRequest
    {
        [StringLength(100)]
        public string? Code { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? MiddleName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        public Guid? CompanyId { get; set; }

        public Guid? PositionId { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
