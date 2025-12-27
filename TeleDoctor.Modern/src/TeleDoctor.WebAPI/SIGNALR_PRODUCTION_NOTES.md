# SignalR Configuration Notes

## Current Implementation Status

The ChatHub and VideoCallHub currently use in-memory connection storage:

```csharp
private static readonly Dictionary<string, string> _connections = new();
```

## ⚠️ Important for Production

**This implementation will NOT work in a load-balanced or multi-instance deployment.**

### Production Requirements

For production deployment with multiple instances, you MUST configure a SignalR backplane using Redis:

#### 1. Install Required Package

Add to `TeleDoctor.WebAPI.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="8.0.0" />
```

#### 2. Update Program.cs

```csharp
// Add this after configuring SignalR
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"), options =>
    {
        options.Configuration.ChannelPrefix = "TeleDoctor";
    });
```

#### 3. Add Redis Connection String

Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "Redis": "redis:6379,abortConnect=false"
  }
}
```

#### 4. Docker Compose

Redis is already configured in `docker-compose.yml` - no changes needed there.

### Testing in Development

The current in-memory implementation works fine for:
- Single-instance development
- Local testing
- Demo environments

### Migration Checklist

Before deploying to production:

- [ ] Install `Microsoft.AspNetCore.SignalR.StackExchangeRedis` package
- [ ] Configure Redis backplane in Program.cs
- [ ] Add Redis connection string to configuration
- [ ] Test with multiple API instances
- [ ] Update health checks to include Redis connectivity
- [ ] Document connection string in Key Vault or secrets management

### Alternative: Azure SignalR Service

For Azure deployments, consider using Azure SignalR Service instead of self-hosted Redis:

```csharp
builder.Services.AddSignalR()
    .AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
```

Benefits:
- Fully managed service
- Automatic scaling
- No Redis infrastructure to manage
- Built-in geo-replication

## Additional Resources

- [SignalR Scale-out with Redis](https://docs.microsoft.com/en-us/aspnet/core/signalr/redis-backplane)
- [Azure SignalR Service](https://docs.microsoft.com/en-us/azure/azure-signalr/)
