namespace TripPin.Core.Models;

public sealed record Person
{
    public required string UserName { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public PersonGender Gender { get; init; }
    public long? Age { get; init; }
    public IReadOnlyList<string> Emails { get; init; } = [];
    public Feature? FavoriteFeature { get; init; }
    public IReadOnlyList<Location> AddressInfo { get; init; } = [];
    public IReadOnlyList<Trip> Trips { get; init; } = [];

    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {MiddleName} {LastName}".Trim();
}
