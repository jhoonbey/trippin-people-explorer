using System.Text.Json.Serialization;

namespace TripPin.Core.Models;

public sealed record ODataCollectionResponse<T>
{
    [JsonPropertyName("value")]
    public IReadOnlyList<T> Value { get; init; } = [];

    [JsonPropertyName("@odata.count")]
    public int? Count { get; init; }
}
