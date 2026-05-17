using System.Text.Json.Serialization;
using Serilog;
using TrainingPlatform.Api.Middleware;
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

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await TrainingPlatformSeeder.SeedAsync(app.Services);

app.Run();

public partial class Program
{
}
