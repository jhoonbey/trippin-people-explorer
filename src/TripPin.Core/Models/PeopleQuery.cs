namespace TripPin.Core.Models;

public sealed record PeopleQuery(
    string? FirstNameFilter = null,
    string? LastNameFilter = null,
    PersonGender? GenderFilter = null,
    string? CityFilter = null,
    int Top = 10,
    int Skip = 0);
