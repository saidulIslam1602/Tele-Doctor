using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeleDoctor.Norwegian.Integration.Services;

namespace TeleDoctor.Norwegian.Integration.Extensions;

/// <summary>
/// Extension methods for configuring Norwegian healthcare integration services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Norwegian healthcare integration services to the dependency injection container
    /// Includes Helsenorge, E-Resept, and FHIR integrations
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Updated service collection</returns>
    public static IServiceCollection AddNorwegianIntegrationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register HttpClient for Helsenorge integration
        services.AddHttpClient<IHelsenorgeIntegrationService, HelsenorgeIntegrationService>();

        // Add other Norwegian healthcare integration services
        // services.AddScoped<IEReseptService, EReseptService>();
        // services.AddScoped<IFHIRService, FHIRService>();

        return services;
    }
}
