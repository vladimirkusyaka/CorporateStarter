using System;
using System.Text;
using System.Collections.Generic;

namespace CorporateStarter.Shared.Dtos.MasterData.Positions
{
    public class UpdatePositionRequest
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
