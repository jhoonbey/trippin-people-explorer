using TripPin.Core.Common;
using TripPin.Core.Models;

namespace TripPin.Core.Interfaces;

public interface IPeopleService
{
    Task<Result<ODataCollectionResponse<Person>>> GetPeopleAsync(PeopleQuery query, CancellationToken ct = default);

    Task<Result<Person>> GetPersonAsync(string userName, CancellationToken ct = default);
}
