namespace TripPin.Core.Models;

public sealed record Location
{
    public string? Address { get; init; }
    public City? City { get; init; }
}
