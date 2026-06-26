using Spectre.Console;
using TripPin.Core.Models;

namespace TripPin.Console.UI;

public sealed class PersonListDisplay : IPersonListDisplay
{
    public void Render(ODataCollectionResponse<Person> page, int skip)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[aqua]People[/]");

        table.AddColumn("#");
        table.AddColumn("User name");
        table.AddColumn("Name");
        table.AddColumn("Gender");
        table.AddColumn("Emails");

        var index = skip + 1;
        foreach (var person in page.Value)
        {
            table.AddRow(
                index.ToString(),
                person.UserName.EscapeMarkup(),
                person.FullName.EscapeMarkup(),
                person.Gender.ToString(),
                person.Emails.Count == 0
                    ? "[grey]none[/]"
                    : string.Join(", ", person.Emails.Select(e => e.EscapeMarkup())));
            index++;
        }

        AnsiConsole.Write(table);

        if (page.Count is { } total)
            AnsiConsole.MarkupLine($"[grey]Showing {skip + 1}-{skip + page.Value.Count} of {total}[/]");
    }
}
