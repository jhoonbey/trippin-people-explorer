using TripPin.Core.Models;

namespace TripPin.Console.UI;

public interface IPersonDetailDisplay
{
    void Render(Person person);
}
