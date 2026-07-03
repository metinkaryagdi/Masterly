using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TrainingPlatform.Application.Abstractions.Execution;

namespace TrainingPlatform.Infrastructure.Execution;

internal sealed class HttpCodeExecutionService(HttpClient httpClient, IOptions<RunnerOptions> options) : ICodeExecutionService
{
    public bool IsEnabled => options.Value.Enabled;

    public async Task<CodeExecutionResult> RunAsync(string solutionCode, string testCode, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("The code execution runner is disabled.");
        }

        var response = await httpClient.PostAsJsonAsync(
            "/run",
            new { solutionCode, testCode },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CodeExecutionResult>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("The code execution runner returned an empty response.");
    }
}
