# Judge service: compiles learner submissions against per-challenge xUnit
# tests and reports pass counts. The final image keeps the full SDK because
# it runs `dotnet test` for every submission.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/TrainingPlatform.Runner/TrainingPlatform.Runner.csproj src/TrainingPlatform.Runner/
RUN dotnet restore src/TrainingPlatform.Runner/TrainingPlatform.Runner.csproj
COPY src/TrainingPlatform.Runner/ src/TrainingPlatform.Runner/
RUN dotnet publish src/TrainingPlatform.Runner/TrainingPlatform.Runner.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/sdk:8.0
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_UI_LANGUAGE=en \
    RUNNER_TEMPLATE_DIR=/template
WORKDIR /app
COPY --from=build /app/publish .
COPY src/TrainingPlatform.Runner/template /template

# Warm the NuGet cache and MSBuild outputs with one full test run so
# per-submission runs are fast and fully offline (--no-restore).
RUN dotnet test /template/TestRunner.csproj --nologo --logger "trx;LogFileName=warmup.trx" \
    && rm -rf /template/TestResults

ENTRYPOINT ["dotnet", "TrainingPlatform.Runner.dll"]
