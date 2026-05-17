using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlatform.Application.Common.Cqrs;
using TrainingPlatform.Application.Services;

namespace TrainingPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddScoped<IQuestionEvaluationService, QuestionEvaluationService>();
        services.AddScoped<IRevisionEngine, RevisionEngine>();
        services.AddScoped<IDailyStudyPlanService, DailyStudyPlanService>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        RegisterHandlers(services, Assembly.GetExecutingAssembly());

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaces = new[]
        {
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        foreach (var implementationType in assembly.GetTypes().Where(type => type is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var handlerInterface in implementationType.GetInterfaces().Where(@interface =>
                         @interface.IsGenericType &&
                         handlerInterfaces.Contains(@interface.GetGenericTypeDefinition())))
            {
                services.AddScoped(handlerInterface, implementationType);
            }
        }
    }
}
