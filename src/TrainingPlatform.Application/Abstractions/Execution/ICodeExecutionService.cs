namespace TrainingPlatform.Application.Abstractions.Execution;

public sealed record CodeExecutionResult(
    bool Compiled,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    string Output,
    long DurationMs);

/// <summary>
/// Runs a learner's submitted code against a challenge's xUnit test suite in
/// an isolated environment and reports the outcome.
/// </summary>
public interface ICodeExecutionService
{
    /// <summary>False when no runner is configured — submissions then stay pending review.</summary>
    bool IsEnabled { get; }

    Task<CodeExecutionResult> RunAsync(string solutionCode, string testCode, CancellationToken cancellationToken);
}
