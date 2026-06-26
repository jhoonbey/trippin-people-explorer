using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripPin.Core.Common;
using TripPin.Core.Configuration;
using TripPin.Core.Interfaces;
using TripPin.Core.Models;

namespace TripPin.Core.Services;

public sealed class CachingPeopleService(
    IPeopleService inner,
    IMemoryCache cache,
    IOptions<TripPinOptions> options,
    ILogger<CachingPeopleService> logger) : IPeopleService
{
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(options.Value.CacheTtlMinutes);

    public async Task<Result<ODataCollectionResponse<Person>>> GetPeopleAsync(
        PeopleQuery query, CancellationToken ct = default)
    {
        var key = PeopleKey(query);
        if (cache.TryGetValue<Result<ODataCollectionResponse<Person>>>(key, out var cached) && cached is not null)
        {
            logger.LogDebug("Cache hit — people list {Query}", query);
            return cached;
        }

        var result = await inner.GetPeopleAsync(query, ct).ConfigureAwait(false);

        if (result.IsSuccess)
            cache.Set(key, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl,
                Size = 1
            });

        return result;
    }

    public async Task<Result<Person>> GetPersonAsync(string userName, CancellationToken ct = default)
    {
        var key = PersonKey(userName);
        if (cache.TryGetValue<Result<Person>>(key, out var cached) && cached is not null)
        {
            logger.LogDebug("Cache hit — person {UserName}", userName);
            return cached;
        }

        var result = await inner.GetPersonAsync(userName, ct).ConfigureAwait(false);

        if (result.IsSuccess)
            cache.Set(key, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl,
                Size = 1
            });

        return result;
    }

    private static object PeopleKey(PeopleQuery q) => ("people", q);

    private static object PersonKey(string userName) => ("person", userName);
}
