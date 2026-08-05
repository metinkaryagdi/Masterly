FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/CodeCraftNet.Domain/CodeCraftNet.Domain.csproj           src/CodeCraftNet.Domain/
COPY src/CodeCraftNet.Application/CodeCraftNet.Application.csproj src/CodeCraftNet.Application/
COPY src/CodeCraftNet.Infrastructure/CodeCraftNet.Infrastructure.csproj src/CodeCraftNet.Infrastructure/
COPY src/CodeCraftNet.Api/CodeCraftNet.Api.csproj                 src/CodeCraftNet.Api/
RUN dotnet restore src/CodeCraftNet.Api/CodeCraftNet.Api.csproj

COPY src/ src/
RUN dotnet publish src/CodeCraftNet.Api/CodeCraftNet.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is only here so the compose healthcheck can probe /health/live from
# inside the container without publishing extra tooling in the image otherwise.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "CodeCraftNet.Api.dll"]
