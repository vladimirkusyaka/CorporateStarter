using CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Persons;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using System.Reflection;
using System.Text.RegularExpressions;
using CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Companies;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using Xunit;

namespace CorporateStarter.Tests.Integration.Directory;

public sealed class CompanyProjectionTests
{
    [Theory]
    [InlineData(typeof(CompanyTableRepository), typeof(CompanyListItemDto))]
    [InlineData(typeof(PersonTableRepository), typeof(PersonListItemDto))]
    public void Compiled_projection_has_quoted_identifiers_for_every_mapped_property(Type repositoryType, Type dtoType)
    {
        // Read the compiled C# value: checking source text missed raw-string delimiters.
        // This property does not access the database.
        var repository = Activator.CreateInstance(repositoryType, new object?[] { null })!;
        var projection = (string)repositoryType
            .GetProperty("Projection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(repository)!;

        Assert.Matches("^\\s*\"[A-Za-z][A-Za-z0-9]*\"(?:\\s*,\\s*\"[A-Za-z][A-Za-z0-9]*\")*\\s*$", projection);
        var columns = Regex.Matches(projection, "\"([A-Za-z][A-Za-z0-9]*)\"")
            .Select(match => match.Groups[1].Value).OrderBy(x => x).ToArray();
        Assert.Equal(dtoType.GetProperties().Select(x => x.Name).OrderBy(x => x), columns);
    }
}
