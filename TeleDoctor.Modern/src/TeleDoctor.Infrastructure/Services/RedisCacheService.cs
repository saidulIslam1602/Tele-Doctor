using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TeleDoctor.Infrastructure.Services;

/// <summary>
/// Redis cache service for distributed caching
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
}

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gets a cached value
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(cachedData))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached data for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Sets a cached value
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached data for key: {Key}", key);
        }
    }

    /// <summary>
    /// Removes a cached value
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached data for key: {Key}", key);
        }
    }

    /// <summary>
    /// Gets a cached value or sets it if not found
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await GetAsync<T>(key, cancellationToken);
            
            if (cachedData != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedData;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            var data = await factory();
            
            await SetAsync(key, data, expiration, cancellationToken);
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            // On error, just call the factory
            return await factory();
        }
    }
}

/// <summary>
/// Cache key constants
/// </summary>
public static class CacheKeys
{
    public const string AllDoctors = "doctors:all";
    public const string AllDepartments = "departments:all";
    public const string AllSpecializations = "specializations:all";
    
    public static string DoctorById(int id) => $"doctor:{id}";
    public static string DoctorsByDepartment(int departmentId) => $"doctors:department:{departmentId}";
    public static string DoctorsBySpecialization(string specialization) => $"doctors:specialization:{specialization}";
    public static string PatientById(int id) => $"patient:{id}";
    public static string DepartmentById(int id) => $"department:{id}";
}
