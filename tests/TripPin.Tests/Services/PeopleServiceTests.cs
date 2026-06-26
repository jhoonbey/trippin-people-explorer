using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripPin.Core.Common;
using TripPin.Core.Interfaces;
using TripPin.Core.Models;
using TripPin.Core.Services;

namespace TripPin.Tests.Services;

public sealed class PeopleServiceTests
{
    private readonly Mock<ITripPinApiClient> _api = new();

    private PeopleService CreateService() =>
        new(_api.Object, NullLogger<PeopleService>.Instance);

    [Fact]
    public async Task GetPeopleAsync_BuildsFilterPagingAndCountPath_AndReturnsPage()
    {
        string? requestedPath = null;
        var page = new ODataCollectionResponse<Person>
        {
            Count = 1,
            Value = [new Person { UserName = "scottketchum", FirstName = "Scott", LastName = "Ketchum" }]
        };
        _api.Setup(a => a.GetCollectionAsync<Person>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => requestedPath = path)
            .ReturnsAsync(Result<ODataCollectionResponse<Person>>.Ok(page));

        var result = await CreateService()
            .GetPeopleAsync(new PeopleQuery(FirstNameFilter: "Scott", Top: 5, Skip: 10));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().ContainSingle();
        requestedPath.Should().Contain("$top=5").And.Contain("$skip=10").And.Contain("$count=true");
        requestedPath.Should().Contain("tolower").And.Contain("scott");
    }

    [Fact]
    public async Task GetPeopleAsync_WithGenderFilter_IncludesQualifiedEnumLiteral()
    {
        string? requestedPath = null;
        _api.Setup(a => a.GetCollectionAsync<Person>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => requestedPath = path)
            .ReturnsAsync(Result<ODataCollectionResponse<Person>>.Ok(new ODataCollectionResponse<Person> { Value = [] }));

        await CreateService().GetPeopleAsync(new PeopleQuery(GenderFilter: PersonGender.Female));

        requestedPath.Should().Contain("Gender").And.Contain("Female");
    }

    [Fact]
    public async Task GetPeopleAsync_WithCityFilter_IncludesAnyLambdaExpression()
    {
        string? requestedPath = null;
        _api.Setup(a => a.GetCollectionAsync<Person>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => requestedPath = path)
            .ReturnsAsync(Result<ODataCollectionResponse<Person>>.Ok(new ODataCollectionResponse<Person> { Value = [] }));

        await CreateService().GetPeopleAsync(new PeopleQuery(CityFilter: "Seattle"));

        requestedPath.Should().Contain("AddressInfo").And.Contain("any").And.Contain("seattle");
    }

    [Fact]
    public async Task GetPeopleAsync_WhenApiReturnsNull_FailsWithFriendlyMessage()
    {
        _api.Setup(a => a.GetCollectionAsync<Person>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ODataCollectionResponse<Person>>.Fail("rejected"));

        var result = await CreateService().GetPeopleAsync(new PeopleQuery());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetPersonAsync_BuildsKeyedExpandPath_AndReturnsPerson()
    {
        string? requestedPath = null;
        _api.Setup(a => a.GetEntityAsync<Person>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => requestedPath = path)
            .ReturnsAsync(Result<Person>.Ok(new Person { UserName = "russellwhyte", FirstName = "Russell", LastName = "Whyte" }));

        var result = await CreateService().GetPersonAsync("russellwhyte");

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Russell Whyte");
        requestedPath.Should().Contain("People('russellwhyte')").And.Contain("$expand=Trips");
    }

    [Fact]
    public async Task GetPersonAsync_WhenApiReturnsNull_FailsWithUserNameInMessage()
    {
        _api.Setup(a => a.GetEntityAsync<Person>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Person>.Fail("not found"));

        var result = await CreateService().GetPersonAsync("nobody");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("nobody");
    }
}
