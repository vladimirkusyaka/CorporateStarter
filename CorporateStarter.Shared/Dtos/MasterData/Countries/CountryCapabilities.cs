using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Shared.Dtos.MasterData.Countries
{
    public sealed record CountryCapabilities(bool CanCreate, bool CanUpdate, bool CanDelete, bool ViewInactive, bool CanRestore);

}
