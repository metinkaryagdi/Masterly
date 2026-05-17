using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TrainingPlatform.Application.Abstractions.AI;
using TrainingPlatform.Application.Abstractions.Persistence;
using TrainingPlatform.Application.Abstractions.Security;
using TrainingPlatform.Application.Abstractions.Time;
using TrainingPlatform.Infrastructure.AI;
using TrainingPlatform.Infrastructure.Auth;
using TrainingPlatform.Infrastructure.Persistence;
using TrainingPlatform.Infrastructure.Time;

namespace TrainingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=training_platform;Username=postgres;Password=postgres";

        services.AddDbContext<TrainingPlatformDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ITrainingPlatformDbContext>(provider => provider.GetRequiredService<TrainingPlatformDbContext>());
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddHttpClient<OllamaApiClient>();
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
        return loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console();
    }
}
