using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Options;

namespace TeleDoctor.WebAPI.Middleware;

/// <summary>
/// Rate limiting middleware to prevent API abuse
/// Implements token bucket algorithm
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, TokenBucket> _clients = new();
    private readonly RateLimitOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and swagger
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var clientKey = GetClientKey(context);
        var bucket = _clients.GetOrAdd(clientKey, _ => new TokenBucket(_options));

        if (!bucket.TryConsume())
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientKey}", clientKey);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 429,
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = 60
            });
            
            return;
        }

        // Add rate limit headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = _options.MaxRequests.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = bucket.AvailableTokens.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = bucket.ResetTime.ToString("O");
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private string GetClientKey(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }
}

/// <summary>
/// Token bucket implementation for rate limiting
/// </summary>
public class TokenBucket
{
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private int _availableTokens;
    private DateTime _lastRefillTime;
    private readonly object _lock = new();

    public int AvailableTokens => _availableTokens;
    public DateTime ResetTime => _lastRefillTime.Add(_refillInterval);

    public TokenBucket(RateLimitOptions options)
    {
        _maxTokens = options.MaxRequests;
        _refillInterval = options.TimeWindow;
        _availableTokens = _maxTokens;
        _lastRefillTime = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            Refill();

            if (_availableTokens > 0)
            {
                _availableTokens--;
                return true;
            }

            return false;
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var timePassed = now - _lastRefillTime;

        if (timePassed >= _refillInterval)
        {
            _availableTokens = _maxTokens;
            _lastRefillTime = now;
        }
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public int MaxRequests { get; set; } = 100;
    public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Extension method to register rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(
        this IApplicationBuilder app,
        Action<RateLimitOptions>? configure = null)
    {
        var options = new RateLimitOptions();
        configure?.Invoke(options);

        var logger = app.ApplicationServices.GetRequiredService<ILogger<RateLimitingMiddleware>>();
        
        return app.UseMiddleware<RateLimitingMiddleware>(logger, options);
    }
}
