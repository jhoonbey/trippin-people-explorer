using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spectre.Console;
using TripPin.Console.Extensions;
using TripPin.Console.UI;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddTripPinServices(builder.Configuration);

using var host = builder.Build();

if (!AnsiConsole.Profile.Capabilities.Interactive)
{
    AnsiConsole.Console.Error("TripPin needs an interactive terminal - run it directly, not piped or redirected.");
    return 1;
}

try
{
    host.Services.GetRequiredService<IStartupValidator>().Validate();
}
catch (OptionsValidationException ex)
{
    AnsiConsole.Console.Error("Configuration error - the app cannot start:");
    foreach (var failure in ex.Failures)
    {
        AnsiConsole.Console.Error($"  * {failure}");
    }
    return 1;
}

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

await host.Services.GetRequiredService<MenuHandler>().RunAsync(cts.Token);
return 0;
