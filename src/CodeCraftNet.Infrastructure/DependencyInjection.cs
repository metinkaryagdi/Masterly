using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using CodeCraftNet.Application.Abstractions.AI;
using CodeCraftNet.Application.Abstractions.Execution;
using CodeCraftNet.Application.Abstractions.Persistence;
using CodeCraftNet.Application.Abstractions.Security;
using CodeCraftNet.Application.Abstractions.Time;
using CodeCraftNet.Infrastructure.AI;
using CodeCraftNet.Infrastructure.Auth;
using CodeCraftNet.Infrastructure.Execution;
using CodeCraftNet.Infrastructure.Persistence;
using CodeCraftNet.Infrastructure.Time;

namespace CodeCraftNet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminAccessOptions>(configuration.GetSection(AdminAccessOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<RunnerOptions>(configuration.GetSection(RunnerOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=codecraftnet_db;Username=postgres;Password=postgres";

        services.AddDbContext<CodeCraftNetDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ICodeCraftNetDbContext>(provider => provider.GetRequiredService<CodeCraftNetDbContext>());
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddHttpClient<ICodeExecutionService, HttpCodeExecutionService>((provider, client) =>
        {
            var runnerOptions = configuration.GetSection(RunnerOptions.SectionName).Get<RunnerOptions>() ?? new RunnerOptions();
            if (Uri.TryCreate(runnerOptions.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Max(10, runnerOptions.TimeoutSeconds));
        });

        services.AddHttpClient<OllamaApiClient>((provider, client) =>
        {
            var ollamaOptions = provider.GetRequiredService<IOptions<OllamaOptions>>().Value;
            if (Uri.TryCreate(ollamaOptions.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Max(10, ollamaOptions.TimeoutSeconds));
        });
        services.AddScoped<IQuestionGenerationService, OllamaQuestionGenerationService>();
        services.AddScoped<IAnswerEvaluationService, OllamaAnswerEvaluationService>();
        services.AddScoped<ICodeFeedbackService, OllamaCodeFeedbackService>();
        services.AddScoped<IScenarioEvaluationService, OllamaScenarioEvaluationService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        // File sink keeps logs on disk (mount a volume at /app/logs in prod) so
        // there is a durable trail even without a log-aggregation stack; rolling
        // + a retained-file cap keeps it from growing unbounded.
        return loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .WriteTo.File(
                "logs/api-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true);
    }
}
