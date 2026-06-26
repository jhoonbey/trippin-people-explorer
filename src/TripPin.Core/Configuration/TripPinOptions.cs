using System.ComponentModel.DataAnnotations;

namespace TripPin.Core.Configuration;

public sealed class TripPinOptions
{
    public const string SectionName = "TripPin";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 100)]
    public int DefaultPageSize { get; init; } = 10;

    [Range(0, 60)]
    public int CacheTtlMinutes { get; init; } = 5;

    [Range(0, 10)]
    public int RetryCount { get; init; } = 3;

    [Range(5, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}
