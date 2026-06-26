using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TripPin.Core.Common;
using TripPin.Core.Exceptions;
using TripPin.Core.Interfaces;
using TripPin.Core.Models;

namespace TripPin.Core.Clients;

public sealed class TripPinApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<TripPinApiClient> logger) : ITripPinApiClient
{
    public const string HttpClientName = "TripPin";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Result<ODataCollectionResponse<T>>> GetCollectionAsync<T>(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendGetAsync(relativePath, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            logger.LogWarning("TripPin rejected the filter expression (path={RelativePath})", relativePath);
            return Result<ODataCollectionResponse<T>>.Fail("The service rejected the filter expression.");
        }

        await EnsureSuccessAsync(response, ct).ConfigureAwait(false);

        var data = await response.Content
                       .ReadFromJsonAsync<ODataCollectionResponse<T>>(JsonOptions, ct)
                       .ConfigureAwait(false)
                   ?? throw new TripPinApiException(response.StatusCode, "The service returned an empty collection response.");

        return Result<ODataCollectionResponse<T>>.Ok(data);
    }

    public async Task<Result<T>> GetEntityAsync<T>(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendGetAsync(relativePath, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogDebug("Resource not found (path={RelativePath})", relativePath);
            return Result<T>.Fail("Resource not found.");
        }

        await EnsureSuccessAsync(response, ct).ConfigureAwait(false);

        var entity = await response.Content
                         .ReadFromJsonAsync<T>(JsonOptions, ct)
                         .ConfigureAwait(false)
                     ?? throw new TripPinApiException(response.StatusCode, "The service returned an empty entity response.");

        return Result<T>.Ok(entity);
    }

    private async Task<HttpResponseMessage> SendGetAsync(string relativePath, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        logger.LogDebug("GET {RelativePath}", relativePath);
        
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync(relativePath, ct).ConfigureAwait(false);
        logger.LogDebug("{StatusCode} from {RelativePath} in {ElapsedMs}ms", (int)response.StatusCode, relativePath, sw.ElapsedMilliseconds);
       
        return response;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : Truncate(body, 500);
        var message = $"TripPin API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {detail}";

        logger.LogError("Unexpected response from TripPin: {StatusCode} for {RequestUri} — {Detail}",
            (int)response.StatusCode, response.RequestMessage?.RequestUri, detail);

        throw new TripPinApiException(response.StatusCode, message);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
