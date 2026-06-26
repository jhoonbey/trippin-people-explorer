using Microsoft.Extensions.Options;
using Spectre.Console;
using TripPin.Core.Configuration;
using TripPin.Core.Interfaces;
using TripPin.Core.Models;

namespace TripPin.Console.UI;

public sealed class MenuHandler(
    IPeopleService people,
    IPersonListDisplay listDisplay,
    IPersonDetailDisplay detailDisplay,
    IOptions<TripPinOptions> options)
{
    private const string ListPeople = "List people";
    private const string SearchPeople = "Search people";
    private const string FindByUserName = "View a person by user name";
    private const string Exit = "Exit";

    private readonly int _pageSize = options.Value.DefaultPageSize;

    public async Task RunAsync(CancellationToken ct = default)
    {
        AnsiConsole.Write(new FigletText("TripPin").Color(Color.Aqua));
        AnsiConsole.MarkupLine("[grey]People explorer for the TripPin OData v4 service[/]");
        AnsiConsole.WriteLine();

        while (!ct.IsCancellationRequested)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(ListPeople, SearchPeople, FindByUserName, Exit));

            try
            {
                switch (choice)
                {
                    case ListPeople:
                        await BrowsePeopleAsync(new PeopleQuery(Top: _pageSize), ct);
                        break;
                    case SearchPeople:
                        await SearchAsync(ct);
                        break;
                    case FindByUserName:
                        await ViewByUserNameAsync(ct);
                        break;
                    case Exit:
                        return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                AnsiConsole.Console.Error($"Something went wrong: {ex.Message}");
            }

            AnsiConsole.WriteLine();
        }
    }

    private async Task SearchAsync(CancellationToken ct)
    {
        var firstName = AnsiConsole.Prompt(
            new TextPrompt<string>("First name contains [grey](leave blank to skip)[/]:").AllowEmpty());

        var lastName = AnsiConsole.Prompt(
            new TextPrompt<string>("Last name contains [grey](leave blank to skip)[/]:").AllowEmpty());

        var genderChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Gender [grey](select Any to skip)[/]:")
                .AddChoices(["Any", ..Enum.GetNames<PersonGender>()]));

        var city = AnsiConsole.Prompt(
            new TextPrompt<string>("City contains [grey](leave blank to skip)[/]:").AllowEmpty());

        PersonGender? genderFilter = genderChoice == "Any" ? null : Enum.Parse<PersonGender>(genderChoice);

        if (string.IsNullOrWhiteSpace(firstName)
            && string.IsNullOrWhiteSpace(lastName)
            && genderFilter is null
            && string.IsNullOrWhiteSpace(city))
        {
            AnsiConsole.Console.Warning("No search terms entered.");
            return;
        }

        await BrowsePeopleAsync(
            new PeopleQuery(
                FirstNameFilter: NullIfBlank(firstName),
                LastNameFilter: NullIfBlank(lastName),
                GenderFilter: genderFilter,
                CityFilter: NullIfBlank(city),
                Top: _pageSize),
            ct);
    }

    private async Task ViewByUserNameAsync(CancellationToken ct)
    {
        var userName = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the [aqua]user name[/]:")
                .Validate(value => string.IsNullOrWhiteSpace(value)
                    ? ValidationResult.Error("[red]User name cannot be empty[/]")
                    : ValidationResult.Success()));

        await ShowDetailAsync(userName.Trim(), ct);
    }

    private async Task BrowsePeopleAsync(PeopleQuery query, CancellationToken ct)
    {
        while (true)
        {
            var result = await Fetch("Loading people...", token => people.GetPeopleAsync(query, token), ct);

            if (!result.IsSuccess)
            {
                AnsiConsole.Console.Warning(result.Error);
                return;
            }

            var page = result.Value!;
            if (page.Value.Count == 0)
            {
                AnsiConsole.Console.Warning("No people matched.");
                return;
            }

            listDisplay.Render(page, query.Skip);

            switch (PromptNavigation(query, page.Count ?? page.Value.Count))
            {
                case Navigation.Next:
                    query = query with { Skip = query.Skip + query.Top };
                    break;
                case Navigation.Previous:
                    query = query with { Skip = Math.Max(0, query.Skip - query.Top) };
                    break;
                case Navigation.ViewDetails:
                    var userName = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select a person:")
                            .PageSize(Math.Max(3, _pageSize + 1))
                            .AddChoices(page.Value.Select(p => p.UserName)));
                    await ShowDetailAsync(userName, ct);
                    break;
                case Navigation.Back:
                    return;
            }
        }
    }

    private async Task ShowDetailAsync(string userName, CancellationToken ct)
    {
        var result = await Fetch($"Loading '{userName}'...", token => people.GetPersonAsync(userName, token), ct);

        if (result.IsSuccess)
            detailDisplay.Render(result.Value!);
        else
            AnsiConsole.Console.Warning(result.Error);
    }

    private static Navigation PromptNavigation(PeopleQuery query, int total)
    {
        var choices = new List<Navigation> { Navigation.ViewDetails };
        if (query.Skip + query.Top < total)
            choices.Add(Navigation.Next);
        if (query.Skip > 0)
            choices.Add(Navigation.Previous);
        choices.Add(Navigation.Back);

        return AnsiConsole.Prompt(
            new SelectionPrompt<Navigation>()
                .Title("Navigate:")
                .UseConverter(NavigationLabel)
                .AddChoices(choices));
    }

    private static string NavigationLabel(Navigation navigation) => navigation switch
    {
        Navigation.Next => "Next page",
        Navigation.Previous => "Previous page",
        Navigation.ViewDetails => "View a person's details",
        Navigation.Back => "Back to menu",
        _ => navigation.ToString()
    };

    private static Task<T> Fetch<T>(string message, Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        AnsiConsole.Status().StartAsync(message, _ => action(ct));

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private enum Navigation
    {
        Next,
        Previous,
        ViewDetails,
        Back
    }
}
