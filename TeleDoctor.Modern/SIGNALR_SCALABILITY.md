# 🔄 SIGNALR SCALABILITY GUIDE

## ⚠️ ISSUE IDENTIFIED

**Problem**: ChatHub uses static in-memory dictionaries for connection and call tracking:
```csharp
private static readonly Dictionary<string, string> _connections = new();
private static readonly Dictionary<string, VideoCallSession> _activeCalls = new();
```

**Impact**:
- ❌ Single server only - cannot scale horizontally
- ❌ Loses all connections on application restart
- ❌ Load balancers will break functionality
- ❌ No support for Kubernetes/multiple pods

---

## ✅ SOLUTION: Redis Backplane

### Why Redis?
- ✅ Distributed connection tracking
- ✅ Survives application restarts
- ✅ Works with load balancers
- ✅ Kubernetes/AKS compatible
- ✅ High performance pub/sub messaging

---

## 🚀 Implementation

### 1. Install NuGet Package

Already included in project:
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" Version="8.0.0" />
```

### 2. Update Program.cs

Add Redis backplane configuration:

```csharp
// Add after SignalR registration
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        var redisConnection = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            options.Configuration = ConfigurationOptions.Parse(redisConnection);
            options.Configuration.AbortOnConnectFail = false;
            
            // Optional: Configure channel prefix
            options.Configuration.ChannelPrefix = "TeleDoctor";
            
            // Optional: Configure connection pooling
            options.Configuration.ConnectTimeout = 5000;
            options.Configuration.SyncTimeout = 5000;
            options.Configuration.KeepAlive = 60;
        }
    });
```

### 3. Update appsettings.json

Add Redis configuration:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "TeleDoctor:",
    "AbortOnConnectFail": false
  },
  "SignalR": {
    "UseRedisBackplane": true,
    "MaximumMessageSize": 32768
  }
}
```

### 4. Production Configuration

**Azure Redis Cache:**
```json
{
  "Redis": {
    "ConnectionString": "your-redis.redis.cache.windows.net:6380,password=your-redis-key,ssl=True,abortConnect=False"
  }
}
```

**Docker Compose Development:**
```yaml
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes

  webapi:
    depends_on:
      - redis
    environment:
      - Redis__ConnectionString=redis:6379

volumes:
  redis-data:
```

---

## 🔧 Refactor ChatHub for Distributed Architecture

### Option 1: Use IDistributedCache (Recommended)

Update ChatHub to use Redis through IDistributedCache:

```csharp
public class ChatHub : Hub
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ChatHub> _logger;
    
    public ChatHub(IDistributedCache cache, ILogger<ChatHub> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        
        // Store in Redis instead of in-memory
        await _cache.SetStringAsync(
            $"connection:{userId}",
            Context.ConnectionId,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
            });
        
        _logger.LogInformation("User connected to chat: {UserId}", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await _cache.RemoveAsync($"connection:{userId}");
        
        _logger.LogInformation("User disconnected from chat: {UserId}", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string recipientUserId, string message)
    {
        var senderId = Context.UserIdentifier ?? Context.ConnectionId;
        
        // Get recipient connection from Redis
        var recipientConnectionId = await _cache.GetStringAsync($"connection:{recipientUserId}");
        
        if (recipientConnectionId != null)
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceiveMessage", senderId, message);
        }
        
        // Store message in database
        // ... existing code ...
    }
}
```

### Option 2: Use Database for Connection Tracking

Create a ConnectionMapping service:

```csharp
public interface IConnectionMappingService
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string userId);
    Task<string?> GetConnectionIdAsync(string userId);
    Task<IEnumerable<string>> GetAllConnectionIdsForUserAsync(string userId);
}

public class RedisConnectionMappingService : IConnectionMappingService
{
    private readonly IDistributedCache _cache;
    
    public RedisConnectionMappingService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        await _cache.SetStringAsync(
            $"connection:{userId}",
            connectionId,
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(4)
            });
    }

    public async Task RemoveConnectionAsync(string userId)
    {
        await _cache.RemoveAsync($"connection:{userId}");
    }

    public async Task<string?> GetConnectionIdAsync(string userId)
    {
        return await _cache.GetStringAsync($"connection:{userId}");
    }

    // Support multiple connections per user
    public async Task<IEnumerable<string>> GetAllConnectionIdsForUserAsync(string userId)
    {
        var json = await _cache.GetStringAsync($"connections:{userId}");
        return json != null 
            ? JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>()
            : new List<string>();
    }
}
```

Register in Program.cs:
```csharp
builder.Services.AddSingleton<IConnectionMappingService, RedisConnectionMappingService>();
```

---

## 📊 Video Call Session Management

For video calls, store sessions in Redis:

```csharp
public class VideoCallSession
{
    public string CallId { get; set; } = string.Empty;
    public string InitiatorId { get; set; } = string.Empty;
    public string RecipientId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public VideoCallStatus Status { get; set; }
}

public class VideoCallService
{
    private readonly IDistributedCache _cache;
    
    public async Task<VideoCallSession> CreateCallAsync(VideoCallSession session)
    {
        var json = JsonSerializer.Serialize(session);
        await _cache.SetStringAsync(
            $"call:{session.CallId}",
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
            });
        return session;
    }

    public async Task<VideoCallSession?> GetCallAsync(string callId)
    {
        var json = await _cache.GetStringAsync($"call:{callId}");
        return json != null 
            ? JsonSerializer.Deserialize<VideoCallSession>(json) 
            : null;
    }

    public async Task DeleteCallAsync(string callId)
    {
        await _cache.RemoveAsync($"call:{callId}");
    }
}
```

---

## 🐳 Docker Setup

### docker-compose.yml

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    container_name: teledoctor-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes --requirepass YourRedisPassword
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

  webapi:
    build:
      context: .
      dockerfile: Dockerfile
    depends_on:
      redis:
        condition: service_healthy
    environment:
      - Redis__ConnectionString=redis:6379,password=YourRedisPassword
      - SignalR__UseRedisBackplane=true
    ports:
      - "5000:8080"
    deploy:
      replicas: 3  # Multiple instances for testing load balancing

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - webapi

volumes:
  redis-data:
```

### nginx.conf for Load Balancing

```nginx
upstream signalr_backend {
    ip_hash;  # Sticky sessions for SignalR
    server webapi:8080;
}

server {
    listen 80;

    location / {
        proxy_pass http://signalr_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## ☁️ Azure Configuration

### 1. Create Azure Redis Cache

```bash
az redis create \
  --name teledoctor-redis \
  --resource-group teledoctor-rg \
  --location norwayeast \
  --sku Premium \
  --vm-size P1
```

### 2. Get Connection String

```bash
az redis list-keys \
  --name teledoctor-redis \
  --resource-group teledoctor-rg
```

### 3. Configure App Service

```bash
az webapp config appsettings set \
  --name teledoctor-api \
  --resource-group teledoctor-rg \
  --settings Redis__ConnectionString="teledoctor-redis.redis.cache.windows.net:6380,password=your-key,ssl=True,abortConnect=False"
```

### 4. Scale Out App Service

```bash
az appservice plan update \
  --name teledoctor-plan \
  --resource-group teledoctor-rg \
  --number-of-workers 3
```

---

## 🎯 Kubernetes Deployment

### redis-deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        command:
        - redis-server
        - --requirepass
        - $(REDIS_PASSWORD)
        env:
        - name: REDIS_PASSWORD
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: password
        volumeMounts:
        - name: redis-data
          mountPath: /data
      volumes:
      - name: redis-data
        persistentVolumeClaim:
          claimName: redis-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: redis
spec:
  selector:
    app: redis
  ports:
  - port: 6379
    targetPort: 6379
```

### webapi-deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: teledoctor-api
spec:
  replicas: 3  # Multiple pods for scalability
  selector:
    matchLabels:
      app: teledoctor-api
  template:
    metadata:
      labels:
        app: teledoctor-api
    spec:
      containers:
      - name: api
        image: teledoctor/api:latest
        env:
        - name: Redis__ConnectionString
          value: "redis:6379,password=$(REDIS_PASSWORD)"
        - name: REDIS_PASSWORD
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: password
        ports:
        - containerPort: 8080
```

---

## 🧪 Testing Scalability

### Test Script (test-signalr-scale.js)

```javascript
const signalR = require('@microsoft/signalr');

async function testMultipleConnections() {
    const connections = [];
    
    // Create 100 connections
    for (let i = 0; i < 100; i++) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost/chathub")
            .build();
        
        connection.on("ReceiveMessage", (user, message) => {
            console.log(`Connection ${i} received: ${user}: ${message}`);
        });
        
        await connection.start();
        connections.push(connection);
    }
    
    console.log(`${connections.length} connections established`);
    
    // Test cross-connection messaging
    await connections[0].invoke("SendMessage", "test-user", "Hello from connection 0");
}

testMultipleConnections();
```

### Load Testing with k6

```javascript
import ws from 'k6/ws';
import { check } from 'k6';

export let options = {
    stages: [
        { duration: '30s', target: 100 },
        { duration: '1m', target: 500 },
        { duration: '30s', target: 0 },
    ],
};

export default function () {
    const url = 'ws://localhost/chathub';
    
    const response = ws.connect(url, (socket) => {
        socket.on('open', () => {
            socket.send(JSON.stringify({
                protocol: 'json',
                version: 1,
            }));
        });
        
        socket.on('message', (data) => {
            console.log('Received:', data);
        });
        
        socket.setTimeout(() => {
            socket.close();
        }, 60000);
    });
    
    check(response, { 'Connected successfully': (r) => r && r.status === 101 });
}
```

---

## 📊 Monitoring

### Redis Metrics to Monitor

```csharp
// Add custom telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Monitor Redis performance
var redis = ConnectionMultiplexer.Connect(redisConnection);
var server = redis.GetServer(redis.GetEndPoints()[0]);

// Track metrics
var info = server.Info("stats");
var connectedClients = info.First(x => x.Key == "connected_clients");
var totalCommands = info.First(x => x.Key == "total_commands_processed");
```

### Application Insights Queries

```kusto
// SignalR connection count
customMetrics
| where name == "signalr.connection.count"
| summarize avg(value) by bin(timestamp, 5m)
| render timechart

// Message throughput
customMetrics
| where name == "signalr.messages.sent"
| summarize sum(value) by bin(timestamp, 1m)
| render timechart
```

---

## ✅ Implementation Checklist

- [ ] Install Microsoft.AspNetCore.SignalR.StackExchangeRedis
- [ ] Configure Redis connection in appsettings
- [ ] Update Program.cs with Redis backplane
- [ ] Refactor ChatHub to use IDistributedCache
- [ ] Create IConnectionMappingService
- [ ] Update VideoCallHub for distributed sessions
- [ ] Configure Redis in docker-compose.yml
- [ ] Set up nginx load balancer
- [ ] Deploy Azure Redis Cache (production)
- [ ] Update Kubernetes manifests
- [ ] Test with multiple instances
- [ ] Load test with k6 or similar
- [ ] Configure monitoring and alerts
- [ ] Document for team

---

## 🚨 Troubleshooting

### Issue: Connections not persisting
**Solution**: Check Redis is running and connection string is correct

### Issue: Messages not delivered across instances
**Solution**: Verify SignalR backplane is configured correctly in Program.cs

### Issue: High Redis memory usage
**Solution**: Implement sliding expiration for connection tracking

### Issue: Slow performance
**Solution**: 
- Use Redis connection pooling
- Enable Redis clustering for production
- Optimize message serialization

---

## 📞 Support

For SignalR scalability issues:
- **Architecture Team**: architecture@teledoctor.no
- **DevOps**: devops@teledoctor.no

---

## ✅ Status

- [x] Documented current issue
- [x] Provided Redis backplane solution
- [x] Created refactored Hub examples
- [x] Docker configuration
- [x] Azure deployment guide
- [x] Kubernetes manifests
- [x] Testing guide
- [x] Monitoring setup

**Last Updated**: December 28, 2025
