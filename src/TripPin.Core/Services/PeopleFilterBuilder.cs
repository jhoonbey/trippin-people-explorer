using TripPin.Core.Models;

namespace TripPin.Core.Services;

internal static class PeopleFilterBuilder
{
    private const string GenderEnumQualifier = "Trippin.PersonGender";

    public static string? Build(PeopleQuery query)
    {
        var clauses = new List<string>(capacity: 4);

        if (!string.IsNullOrWhiteSpace(query.FirstNameFilter))
            clauses.Add(CaseInsensitiveContains(nameof(Person.FirstName), query.FirstNameFilter));

        if (!string.IsNullOrWhiteSpace(query.LastNameFilter))
            clauses.Add(CaseInsensitiveContains(nameof(Person.LastName), query.LastNameFilter));

        if (query.GenderFilter.HasValue)
            clauses.Add($"Gender eq {GenderEnumQualifier}'{query.GenderFilter.Value}'");

        if (!string.IsNullOrWhiteSpace(query.CityFilter))
            clauses.Add(CityContains(query.CityFilter));

        return clauses.Count == 0 ? null : string.Join(" and ", clauses);
    }

    internal static string EscapeLiteral(string value) => value.Replace("'", "''");

    private static string CaseInsensitiveContains(string property, string term) =>
        $"contains(tolower({property}),'{EscapeLiteral(term.Trim().ToLowerInvariant())}')";

    private static string CityContains(string city) =>
        $"AddressInfo/any(a:contains(tolower(a/City/Name),'{EscapeLiteral(city.Trim().ToLowerInvariant())}'))";
}
