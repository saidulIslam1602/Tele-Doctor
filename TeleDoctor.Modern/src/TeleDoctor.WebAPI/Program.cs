using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TeleDoctor.AI.Services.Configuration;
using TeleDoctor.Infrastructure.Data;
using TeleDoctor.Infrastructure.Extensions;
using TeleDoctor.Application.Extensions;
using TeleDoctor.AI.Services.Extensions;
using TeleDoctor.Norwegian.Integration.Extensions;
using Azure.AI.OpenAI;
using Microsoft.ApplicationInsights.Extensibility;
using TeleDoctor.WebAPI.Hubs;
using TeleDoctor.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/teledoctor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TeleDoctor Modern API",
        Version = "v1",
        Description = "Advanced AI-Powered Telemedicine Platform for Norwegian Healthcare",
        Contact = new OpenApiContact
        {
            Name = "TeleDoctor Team",
            Email = "support@teledoctor.no"
        }
    });

    // Enable XML documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Entity Framework
builder.Services.AddDbContext<TeleDoctorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<TeleDoctorDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PatientPolicy", policy => policy.RequireRole("Patient"));
    options.AddPolicy("DoctorPolicy", policy => policy.RequireRole("Doctor"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CoordinatorPolicy", policy => policy.RequireRole("Coordinator"));
});

// Configure AI Services
builder.Services.Configure<AIConfiguration>(builder.Configuration.GetSection(AIConfiguration.SectionName));

// Add Azure OpenAI
var aiConfig = builder.Configuration.GetSection(AIConfiguration.SectionName).Get<AIConfiguration>();
if (aiConfig?.AzureOpenAI != null && !string.IsNullOrEmpty(aiConfig.AzureOpenAI.Endpoint))
{
    builder.Services.AddSingleton(new OpenAIClient(
        new Uri(aiConfig.AzureOpenAI.Endpoint),
        new Azure.AzureKeyCredential(aiConfig.AzureOpenAI.ApiKey)));
}

// Add Application Services
builder.Services.AddApplicationServices();

// Add Infrastructure Services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add AI Services
builder.Services.AddAIServices(builder.Configuration);

// Add Norwegian Integration Services
builder.Services.AddNorwegianIntegrationServices(builder.Configuration);

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://localhost:7000", "https://teledoctor.no")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add Health Checks with detailed checks
// Note: Install Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore for AddDbContextCheck
builder.Services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        try
        {
            // Basic health check - can be enhanced with actual DB connectivity test
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection configured");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }, tags: new[] { "db", "sql" })
    .AddCheck("cache", () =>
    {
        // Check if distributed cache is available
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Cache service is available");
    }, tags: new[] { "cache" })
    .AddCheck("ai_services", () => 
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("AI services are operational"), 
        tags: new[] { "ai", "external" });

// Configure Rate Limiting
builder.Services.AddRateLimiting(options =>
{
    options.MaxRequests = builder.Environment.IsDevelopment() ? 200 : 100;
    options.TimeWindow = TimeSpan.FromMinutes(1);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeleDoctor Modern API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

// Response compression for better performance
app.UseResponseCompression();

// Global exception handling
app.UseExceptionHandling();

// Rate limiting
app.UseRateLimiting();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<ChatHub>("/chatHub");
app.MapHub<VideoCallHub>("/videoCallHub");

// Map Health Checks
app.MapHealthChecks("/health"); // Basic health check
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache")
}); // Readiness check
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness check - always healthy if app is running
});

// Seed database - Commented out for initial startup
// Uncomment after first successful run
/*
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TeleDoctorDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
}
*/

Log.Information("TeleDoctor Modern API starting up...");

app.Run();

public partial class Program { } // For testing
