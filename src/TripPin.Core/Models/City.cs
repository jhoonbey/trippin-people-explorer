namespace TripPin.Core.Models;

public sealed record City
{
    public string? Name { get; init; }
    public string? CountryRegion { get; init; }
    public string? Region { get; init; }
}
