using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace TeleDoctor.Application.Extensions;

/// <summary>
/// Extension methods for configuring Application layer services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the dependency injection container
    /// Includes MediatR, AutoMapper, FluentValidation, and application services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Updated service collection</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR for CQRS pattern
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Add AutoMapper for object mapping
        services.AddAutoMapper(assembly);

        // Add FluentValidation for input validation
        services.AddValidatorsFromAssembly(assembly);

        // Add application services
        // services.AddScoped<IAppointmentService, AppointmentService>();
        // services.AddScoped<IPrescriptionService, PrescriptionService>();
        // Additional services would be registered here

        return services;
    }
}
