using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TripPin.Core.Common;
using TripPin.Core.Configuration;
using TripPin.Core.Interfaces;
using TripPin.Core.Models;
using TripPin.Core.Services;

namespace TripPin.Tests.Services;

public sealed class CachingPeopleServiceTests
{
    private readonly Mock<IPeopleService> _inner = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly IOptions<TripPinOptions> DefaultOptions =
        Options.Create(new TripPinOptions { BaseUrl = "https://test.local/", CacheTtlMinutes = 5 });

    private CachingPeopleService CreateService() =>
        new(_inner.Object, _cache, DefaultOptions, NullLogger<CachingPeopleService>.Instance);

    [Fact]
    public async Task GetPeopleAsync_OnSecondCallWithSameQuery_HitsCache()
    {
        _inner.Setup(s => s.GetPeopleAsync(It.IsAny<PeopleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ODataCollectionResponse<Person>>.Ok(new ODataCollectionResponse<Person> { Value = [] }));

        var service = CreateService();
        var query = new PeopleQuery(FirstNameFilter: "Scott");
        await service.GetPeopleAsync(query);
        await service.GetPeopleAsync(query);

        _inner.Verify(s => s.GetPeopleAsync(It.IsAny<PeopleQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPeopleAsync_WhenInnerFails_DoesNotCacheFailure()
    {
        _inner.Setup(s => s.GetPeopleAsync(It.IsAny<PeopleQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ODataCollectionResponse<Person>>.Fail("rejected"));

        var service = CreateService();
        var query = new PeopleQuery();
        await service.GetPeopleAsync(query);
        await service.GetPeopleAsync(query);

        _inner.Verify(s => s.GetPeopleAsync(It.IsAny<PeopleQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPersonAsync_OnSecondCallWithSameUserName_HitsCache()
    {
        _inner.Setup(s => s.GetPersonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Person>.Ok(new Person { UserName = "russellwhyte", FirstName = "Russell", LastName = "Whyte" }));

        var service = CreateService();
        await service.GetPersonAsync("russellwhyte");
        await service.GetPersonAsync("russellwhyte");

        _inner.Verify(s => s.GetPersonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPersonAsync_WhenInnerFails_DoesNotCacheFailure()
    {
        _inner.Setup(s => s.GetPersonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Person>.Fail("not found"));

        var service = CreateService();
        await service.GetPersonAsync("nobody");
        await service.GetPersonAsync("nobody");

        _inner.Verify(s => s.GetPersonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
