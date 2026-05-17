FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/TrainingPlatform.Domain/TrainingPlatform.Domain.csproj           src/TrainingPlatform.Domain/
COPY src/TrainingPlatform.Application/TrainingPlatform.Application.csproj src/TrainingPlatform.Application/
COPY src/TrainingPlatform.Infrastructure/TrainingPlatform.Infrastructure.csproj src/TrainingPlatform.Infrastructure/
COPY src/TrainingPlatform.Api/TrainingPlatform.Api.csproj                 src/TrainingPlatform.Api/
RUN dotnet restore src/TrainingPlatform.Api/TrainingPlatform.Api.csproj

COPY src/ src/
RUN dotnet publish src/TrainingPlatform.Api/TrainingPlatform.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "TrainingPlatform.Api.dll"]
