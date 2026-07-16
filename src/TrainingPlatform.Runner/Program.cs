using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var templateDir = Environment.GetEnvironmentVariable("RUNNER_TEMPLATE_DIR")
                  ?? Path.Combine(AppContext.BaseDirectory, "template");

// dotnet test is CPU-heavy and the container is resource-capped, so only a
// couple of judge runs may execute at once; the rest queue here.
var gate = new SemaphoreSlim(2);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/run", async (RunRequest request, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.SolutionCode) || string.IsNullOrWhiteSpace(request.TestCode))
    {
        return Results.BadRequest(new { error = "solutionCode and testCode are required." });
    }

    if (request.SolutionCode.Length > 200_000 || request.TestCode.Length > 200_000)
    {
        return Results.BadRequest(new { error = "Code payload too large." });
    }

    await gate.WaitAsync(cancellationToken);
    try
    {
        var result = await JudgeRun.ExecuteAsync(templateDir, request, cancellationToken);
        return Results.Ok(result);
    }
    finally
    {
        gate.Release();
    }
});

app.Run();

public sealed record RunRequest(string SolutionCode, string TestCode, int? TimeoutSeconds);

public sealed record RunResponse(
    bool Compiled,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    string Output,
    long DurationMs);

public static class JudgeRun
{
    private static readonly XNamespace TrxNs = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static async Task<RunResponse> ExecuteAsync(string templateDir, RunRequest request, CancellationToken cancellationToken)
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"judge-{Guid.NewGuid():N}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            CopyDirectory(templateDir, workDir);
            await File.WriteAllTextAsync(Path.Combine(workDir, "Solution.cs"), request.SolutionCode, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(workDir, "Tests.cs"), request.TestCode, cancellationToken);

            var staleResults = Path.Combine(workDir, "TestResults");
            if (Directory.Exists(staleResults))
            {
                Directory.Delete(staleResults, recursive: true);
            }

            var timeoutSeconds = Math.Clamp(request.TimeoutSeconds ?? 60, 10, 120);
            var (exitCode, consoleOutput, timedOut) = await RunDotnetTestAsync(workDir, timeoutSeconds, cancellationToken);

            if (timedOut)
            {
                return new RunResponse(false, 0, 0, 0,
                    $"Execution timed out after {timeoutSeconds}s. Check for infinite loops.", stopwatch.ElapsedMilliseconds);
            }

            var trxPath = Directory.Exists(Path.Combine(workDir, "TestResults"))
                ? Directory.EnumerateFiles(Path.Combine(workDir, "TestResults"), "*.trx").FirstOrDefault()
                : null;

            if (trxPath is null)
            {
                // No test results at all — the build failed before tests could run.
                return new RunResponse(false, 0, 0, 0, ExtractCompileErrors(consoleOutput), stopwatch.ElapsedMilliseconds);
            }

            var (total, passed, failed, failureDetails) = ParseTrx(trxPath);
            var output = failed > 0 ? failureDetails : $"{total} testin tamamı geçti.";
            return new RunResponse(true, total, passed, failed, Truncate(output, 4000), stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static async Task<(int ExitCode, string Output, bool TimedOut)> RunDotnetTestAsync(
        string workDir, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("test");
        psi.ArgumentList.Add("TestRunner.csproj");
        psi.ArgumentList.Add("--no-restore");
        psi.ArgumentList.Add("--nologo");
        psi.ArgumentList.Add("--logger");
        psi.ArgumentList.Add("trx;LogFileName=results.trx");
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using var process = new Process { StartInfo = psi };
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            cancellationToken.ThrowIfCancellationRequested();
            return (-1, output.ToString(), true);
        }

        return (process.ExitCode, output.ToString(), false);
    }

    private static (int Total, int Passed, int Failed, string FailureDetails) ParseTrx(string trxPath)
    {
        var doc = XDocument.Load(trxPath);
        var counters = doc.Descendants(TrxNs + "Counters").FirstOrDefault();
        var total = (int?)counters?.Attribute("total") ?? 0;
        var passed = (int?)counters?.Attribute("passed") ?? 0;
        var failed = (int?)counters?.Attribute("failed") ?? 0;

        var failures = doc.Descendants(TrxNs + "UnitTestResult")
            .Where(result => (string?)result.Attribute("outcome") == "Failed")
            .Select(result =>
            {
                var name = (string?)result.Attribute("testName") ?? "bilinmeyen test";
                var message = result.Descendants(TrxNs + "Message").FirstOrDefault()?.Value.Trim() ?? string.Empty;
                return $"BAŞARISIZ {name}\n  {message.Replace("\n", "\n  ")}";
            });

        var details = $"{total} testten {passed} tanesi geçti.\n\n{string.Join("\n\n", failures)}";
        return (total, passed, failed, details);
    }

    private static string ExtractCompileErrors(string consoleOutput)
    {
        var errorLines = consoleOutput
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Contains("error CS", StringComparison.Ordinal)
                        || line.Contains("error MSB", StringComparison.Ordinal))
            .Distinct()
            .Take(20)
            .ToList();

        var body = errorLines.Count > 0
            ? string.Join("\n", errorLines)
            : Truncate(consoleOutput, 2000);

        return $"Derleme başarısız oldu.\n{body}";
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "\n… (truncated)";
}
