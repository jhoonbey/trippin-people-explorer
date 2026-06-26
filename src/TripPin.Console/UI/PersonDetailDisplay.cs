using System.Globalization;
using Spectre.Console;
using TripPin.Core.Models;

namespace TripPin.Console.UI;

public sealed class PersonDetailDisplay : IPersonDetailDisplay
{
    private const string Dash = "-";

    public void Render(Person person)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
        grid.AddColumn();

        grid.AddRow("[grey]User name[/]", person.UserName.EscapeMarkup());
        grid.AddRow("[grey]Name[/]", person.FullName.EscapeMarkup());
        grid.AddRow("[grey]Gender[/]", person.Gender.ToString());
        grid.AddRow("[grey]Age[/]", person.Age?.ToString(CultureInfo.InvariantCulture) ?? Dash);
        grid.AddRow("[grey]Favourite[/]", person.FavoriteFeature?.ToString() ?? Dash);
        grid.AddRow("[grey]Emails[/]", person.Emails.Count == 0
            ? "[grey]none[/]"
            : string.Join("\n", person.Emails.Select(e => e.EscapeMarkup())));

        AnsiConsole.Write(
            new Panel(grid)
                .Header($"[aqua]{person.FullName.EscapeMarkup()}[/]")
                .Border(BoxBorder.Rounded)
                .Expand());

        RenderAddresses(person);
        RenderTrips(person);
    }

    private static void RenderAddresses(Person person)
    {
        if (person.AddressInfo.Count == 0)
            return;

        var table = new Table().Border(TableBorder.Minimal).Title("[aqua]Addresses[/]");
        table.AddColumn("Address");
        table.AddColumn("City");
        table.AddColumn("Region");
        table.AddColumn("Country");

        foreach (var location in person.AddressInfo)
        {
            table.AddRow(
                (location.Address ?? Dash).EscapeMarkup(),
                (location.City?.Name ?? Dash).EscapeMarkup(),
                (location.City?.Region ?? Dash).EscapeMarkup(),
                (location.City?.CountryRegion ?? Dash).EscapeMarkup());
        }

        AnsiConsole.Write(table);
    }

    private static void RenderTrips(Person person)
    {
        if (person.Trips.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No trips on record.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Minimal).Title($"[aqua]Trips ({person.Trips.Count})[/]");
        table.AddColumn("Name");
        table.AddColumn("Dates");
        table.AddColumn(new TableColumn("Budget").RightAligned());

        foreach (var trip in person.Trips)
        {
            var dates = trip.StartsAt is { } start
                ? $"{start:yyyy-MM-dd} to {trip.EndsAt:yyyy-MM-dd}"
                : Dash;

            table.AddRow(
                (trip.Name ?? Dash).EscapeMarkup(),
                dates,
                trip.Budget?.ToString("N0", CultureInfo.InvariantCulture) ?? Dash);
        }

        AnsiConsole.Write(table);
    }
}
