using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Serilog;
using CodeCraftNet.Api.HealthChecks;
using CodeCraftNet.Api.Middleware;
using CodeCraftNet.Api.RateLimiting;
using CodeCraftNet.Application;
using CodeCraftNet.Infrastructure;
using CodeCraftNet.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    loggerConfiguration.ConfigureSerilog(context.Configuration));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApiRateLimiting();

// "live" = process is up, no dependency checks (liveness probe).
// "ready" = also confirms the database is reachable (readiness probe).
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// In production the api container sits behind the nginx TLS terminator and is
// not published to the host itself (see docker-compose.prod.yml), so nginx is
// the only thing that can set these headers — trusting them unconditionally
// here is safe for that topology.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

const string AppCorsPolicy = "AppCors";
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

// Outside Development, an allow-any-origin fallback combined with credentials is a
// CSRF/credential-theft hole, so fail fast at startup instead of silently opening up.
if (!builder.Environment.IsDevelopment() && (configuredCorsOrigins is null || configuredCorsOrigins.Length == 0))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must list at least one explicit origin outside Development.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppCorsPolicy, policy =>
    {
        if (configuredCorsOrigins is { Length: > 0 })
        {
            policy.WithOrigins(configuredCorsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else
        {
            // Development-only convenience: no explicit origins configured.
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

// Swagger exposes the full API surface; only serve it in Development or when
// explicitly opted into (e.g. a staging environment) via Swagger:Enabled.
var swaggerEnabled = builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Training Platform API",
            Version = "v1",
            Description = "Adaptive training platform: topics, questions, daily study plans, spaced-repetition revisions, and AI-assisted feedback."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT obtained from /api/auth/login."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
}

var app = builder.Build();

// Must run before anything that inspects scheme/remote IP (CORS, https
// redirect, request logging), otherwise those see the nginx hop, not the client.
app.UseForwardedHeaders();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS must run before HTTPS redirect, otherwise a cross-origin preflight
// from an http://localhost frontend gets redirected to https:// and the
// browser drops the request without seeing the CORS headers.
app.UseCors(AppCorsPolicy);

// HTTPS redirect is fine in production but in dev it forces the frontend
// (which calls http://localhost:5000) onto https and breaks the dev loop.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Training Platform API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

// After authentication so per-user partitioning can read the user's identity.
app.UseRateLimiter();

app.MapControllers();

// Liveness: process is up, no dependency checks. Readiness: also probes the database.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

await CodeCraftNetSeeder.SeedAsync(app.Services);

app.Run();

public partial class Program
{
}
