using Microsoft.Extensions.Logging;
using TripPin.Core.Common;
using TripPin.Core.Interfaces;
using TripPin.Core.Models;

namespace TripPin.Core.Services;

public sealed class PeopleService(
    ITripPinApiClient api,
    ILogger<PeopleService> logger) : IPeopleService
{
    private const string PeopleEntitySet = "People";

    public async Task<Result<ODataCollectionResponse<Person>>> GetPeopleAsync(
        PeopleQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        logger.LogInformation("GetPeople {Query}", query);

        var path = new ODataQueryBuilder(PeopleEntitySet)
            .Filter(PeopleFilterBuilder.Build(query))
            .Top(query.Top)
            .Skip(query.Skip)
            .Count()
            .Build();

        var result = await api.GetCollectionAsync<Person>(path, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Service rejected the filter for query {Query}", query);
            return Result<ODataCollectionResponse<Person>>.Fail(
                "The service rejected the search request. Please adjust your filter and try again.");
        }

        return result;
    }

    public async Task<Result<Person>> GetPersonAsync(string userName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        logger.LogInformation("GetPerson {UserName}", userName);

        var escapedKey = Uri.EscapeDataString(PeopleFilterBuilder.EscapeLiteral(userName));
        var path = new ODataQueryBuilder($"{PeopleEntitySet}('{escapedKey}')")
            .Expand(nameof(Person.Trips))
            .Build();

        var result = await api.GetEntityAsync<Person>(path, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
            return Result<Person>.Fail($"No person found with user name '{userName}'.");

        return result;
    }
}
