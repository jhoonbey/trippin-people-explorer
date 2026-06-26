using TripPin.Core.Common;
using TripPin.Core.Models;

namespace TripPin.Core.Interfaces;

public interface ITripPinApiClient
{
    Task<Result<ODataCollectionResponse<T>>> GetCollectionAsync<T>(string relativePath, CancellationToken ct = default);

    Task<Result<T>> GetEntityAsync<T>(string relativePath, CancellationToken ct = default);
}
