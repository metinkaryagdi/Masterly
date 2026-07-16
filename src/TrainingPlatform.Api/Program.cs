using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Serilog;
using TrainingPlatform.Api.Middleware;
using TrainingPlatform.Api.RateLimiting;
using TrainingPlatform.Application;
using TrainingPlatform.Infrastructure;
using TrainingPlatform.Infrastructure.Seeding;

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

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

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

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS must run before HTTPS redirect, otherwise a cross-origin preflight
// from an http://localhost frontend gets redirected to https:// and the
// browser drops the request without seeing the CORS headers.
app.UseCors(DevCorsPolicy);

// HTTPS redirect is fine in production but in dev it forces the frontend
// (which calls http://localhost:5000) onto https and breaks the dev loop.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Training Platform API v1");
});

app.UseAuthentication();
app.UseAuthorization();

// After authentication so per-user partitioning can read the user's identity.
app.UseRateLimiter();

app.MapControllers();

await TrainingPlatformSeeder.SeedAsync(app.Services);

app.Run();

public partial class Program
{
}
