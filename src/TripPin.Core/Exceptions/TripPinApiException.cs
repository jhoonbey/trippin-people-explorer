using System.Net;

namespace TripPin.Core.Exceptions;

public sealed class TripPinApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
