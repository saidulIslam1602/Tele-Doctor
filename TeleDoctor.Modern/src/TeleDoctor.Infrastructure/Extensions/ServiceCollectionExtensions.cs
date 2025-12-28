using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeleDoctor.Core.Interfaces;
using TeleDoctor.Infrastructure.Data;
using TeleDoctor.Infrastructure.Repositories;
using TeleDoctor.Infrastructure.Services;

namespace TeleDoctor.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring Infrastructure layer services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all infrastructure services to the dependency injection container
    /// Includes database context, repositories, and unit of work
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Updated service collection</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext with SQL Server
        services.AddDbContext<TeleDoctorDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });

            // Enable sensitive data logging in development only
            if (configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Cache Service (uses IDistributedCache)
        // For production: use AddStackExchangeRedisCache with Redis connection string
        // For development: uses in-memory cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            // Redis cache (requires Microsoft.Extensions.Caching.StackExchangeRedis package)
            // services.AddStackExchangeRedisCache(options =>
            // {
            //     options.Configuration = redisConnection;
            //     options.InstanceName = "TeleDoctor:";
            // });
        }
        else
        {
            // Fallback to in-memory distributed cache for development
            services.AddDistributedMemoryCache();
        }
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}
