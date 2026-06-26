using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripPin.Core.Clients;
using TripPin.Core.Models;
using TripPin.Core.Services;

namespace TripPin.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PeopleServiceIntegrationTests
{
    private const string ServiceUrl = "https://services.odata.org/TripPinRESTierService/";

    private static PeopleService CreateService()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(ServiceUrl) };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var apiClient = new TripPinApiClient(factory.Object, NullLogger<TripPinApiClient>.Instance);
        return new PeopleService(apiClient, NullLogger<PeopleService>.Instance);
    }

    [Fact]
    public async Task GetPeopleAsync_ReturnsAtLeastOnePerson()
    {
        var result = await CreateService().GetPeopleAsync(new PeopleQuery(Top: 5));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPeopleAsync_WithFirstNameFilter_ReturnsOnlyMatchingPeople()
    {
        var result = await CreateService().GetPeopleAsync(new PeopleQuery(FirstNameFilter: "Scott", Top: 10));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().NotBeEmpty()
            .And.OnlyContain(p => p.FirstName.Contains("scott", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPeopleAsync_WithGenderFilter_ReturnsOnlyMatchingGender()
    {
        var result = await CreateService().GetPeopleAsync(new PeopleQuery(GenderFilter: PersonGender.Female, Top: 10));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().NotBeEmpty()
            .And.OnlyContain(p => p.Gender == PersonGender.Female);
    }

    [Fact]
    public async Task GetPersonAsync_ForKnownUser_ReturnsPerson()
    {
        var result = await CreateService().GetPersonAsync("russellwhyte");

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserName.Should().Be("russellwhyte");
        result.Value.FirstName.Should().Be("Russell");
    }

    [Fact]
    public async Task GetPersonAsync_ForUnknownUser_ReturnsFailureResult()
    {
        var result = await CreateService().GetPersonAsync("thisuserdoesnotexist99999");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
