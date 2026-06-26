using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripPin.Console.UI;
using TripPin.Core.Clients;
using TripPin.Core.Configuration;
using TripPin.Core.Interfaces;
using TripPin.Core.Services;

namespace TripPin.Console.Extensions;

internal static class ServicesExtensions
{
    public static IServiceCollection AddTripPinServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<TripPinOptions>()
            .Bind(configuration.GetSection(TripPinOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services
            .AddApiHttpClient(configuration)
            .AddCoreServices()
            .AddUi();
    }

    private static IServiceCollection AddApiHttpClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TripPinOptions.SectionName);
        var retryCount = section.GetValue<int?>(nameof(TripPinOptions.RetryCount)) ?? 3;
        var timeoutSeconds = section.GetValue<int?>(nameof(TripPinOptions.TimeoutSeconds)) ?? 30;

        services
            .AddHttpClient(TripPinApiClient.HttpClientName, (sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<TripPinOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.MaxRetryAttempts = retryCount;
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            });

        return services;
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddMemoryCache(options => options.SizeLimit = 500);
        services.AddSingleton<ITripPinApiClient, TripPinApiClient>();
        services.AddSingleton<PeopleService>();
        services.AddSingleton<IPeopleService>(sp => new CachingPeopleService(
            sp.GetRequiredService<PeopleService>(),
            sp.GetRequiredService<IMemoryCache>(),
            sp.GetRequiredService<IOptions<TripPinOptions>>(),
            sp.GetRequiredService<ILogger<CachingPeopleService>>()));

        return services;
    }

    private static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<IPersonListDisplay, PersonListDisplay>();
        services.AddSingleton<IPersonDetailDisplay, PersonDetailDisplay>();
        services.AddSingleton<MenuHandler>();

        return services;
    }
}
