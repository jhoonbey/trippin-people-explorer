using TripPin.Core.Models;

namespace TripPin.Console.UI;

public interface IPersonListDisplay
{
    void Render(ODataCollectionResponse<Person> page, int skip);
}
