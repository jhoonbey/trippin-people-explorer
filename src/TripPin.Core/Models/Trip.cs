namespace TripPin.Core.Models;

public sealed record Trip
{
    public string? Name { get; init; }
    public double? Budget { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
}
