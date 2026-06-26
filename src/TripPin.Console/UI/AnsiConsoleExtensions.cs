using Spectre.Console;

namespace TripPin.Console.UI;

internal static class AnsiConsoleExtensions
{
    public static void Warning(this IAnsiConsole console, string message) =>
        console.MarkupLine($"[yellow]{message.EscapeMarkup()}[/]");

    public static void Error(this IAnsiConsole console, string message) =>
        console.MarkupLine($"[red]{message.EscapeMarkup()}[/]");
}
