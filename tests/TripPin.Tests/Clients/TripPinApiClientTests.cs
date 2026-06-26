using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TripPin.Core.Clients;
using TripPin.Core.Common;
using TripPin.Core.Exceptions;
using TripPin.Core.Models;
using TripPin.Tests.Helpers;

namespace TripPin.Tests.Clients;

public sealed class TripPinApiClientTests
{
    private const string BaseUrl = "https://test.local/TripPinRESTierService/";

    private static TripPinApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return new TripPinApiClient(factory.Object, NullLogger<TripPinApiClient>.Instance);
    }

    [Fact]
    public async Task GetCollectionAsync_OnSuccess_ParsesEnvelopeEnumsAndCount()
    {
        const string json = """
            {
                "@odata.count": 2,
                "value": [
                    { "UserName": "russellwhyte", "FirstName": "Russell", "Gender": "Male" },
                    { "UserName": "scottketchum", "FirstName": "Scott", "Gender": "Female" }
                ]
            }
            """;

        var result = await CreateClient(MockHttpMessageHandler.Json(HttpStatusCode.OK, json))
            .GetCollectionAsync<Person>("People");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(2);
        result.Value.Value.Should().HaveCount(2);
        result.Value.Value[0].Gender.Should().Be(PersonGender.Male);
        result.Value.Value[1].Gender.Should().Be(PersonGender.Female);
    }

    [Fact]
    public async Task GetCollectionAsync_OnBadRequest_ReturnsFailure()
    {
        var result = await CreateClient(MockHttpMessageHandler.Json(HttpStatusCode.BadRequest, "bad filter"))
            .GetCollectionAsync<Person>("People?$filter=bogus");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetEntityAsync_OnSuccess_DeserializesEntity()
    {
        const string json = """{ "UserName": "russellwhyte", "FirstName": "Russell", "LastName": "Whyte" }""";

        var result = await CreateClient(MockHttpMessageHandler.Json(HttpStatusCode.OK, json))
            .GetEntityAsync<Person>("People('russellwhyte')");

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Russell Whyte");
    }

    [Fact]
    public async Task GetEntityAsync_OnNotFound_ReturnsFailure()
    {
        var result = await CreateClient(MockHttpMessageHandler.Json(HttpStatusCode.NotFound, ""))
            .GetEntityAsync<Person>("People('nobody')");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetEntityAsync_OnServerError_ThrowsTripPinApiExceptionWithStatusCode()
    {
        var act = () => CreateClient(MockHttpMessageHandler.Json(HttpStatusCode.InternalServerError, "boom"))
            .GetEntityAsync<Person>("People('russellwhyte')");

        (await act.Should().ThrowAsync<TripPinApiException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
