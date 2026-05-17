using TrainingPlatform.Domain.Common.Enumerations;

namespace TrainingPlatform.Application.Features.Topics;

public sealed record TopicDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    TopicDifficulty Difficulty,
    double DecayRate,
    IReadOnlyCollection<Guid> DependencyIds);
