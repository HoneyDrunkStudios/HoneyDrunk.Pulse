# Build context: repository root

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Run as a non-root user. mcr.microsoft.com/dotnet/aspnet:10.0 defines APP_UID, but pinning the
# numeric UID explicitly removes the dependency on that base-image detail and prevents the
# directive from collapsing to USER "" if APP_UID ever becomes unset (CodeQL flags the latter
# as a hardening risk).
USER 1654
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace

COPY ["src/Pulse.Collector/HoneyDrunk.Pulse.Collector.csproj", "src/Pulse.Collector/"]
COPY ["src/HoneyDrunk.Pulse.Contracts/HoneyDrunk.Pulse.Contracts.csproj", "src/HoneyDrunk.Pulse.Contracts/"]
COPY ["src/HoneyDrunk.Telemetry.Abstractions/HoneyDrunk.Telemetry.Abstractions.csproj", "src/HoneyDrunk.Telemetry.Abstractions/"]
COPY ["src/HoneyDrunk.Telemetry.OpenTelemetry/HoneyDrunk.Telemetry.OpenTelemetry.csproj", "src/HoneyDrunk.Telemetry.OpenTelemetry/"]
COPY ["src/HoneyDrunk.Telemetry.Sink.AzureMonitor/HoneyDrunk.Telemetry.Sink.AzureMonitor.csproj", "src/HoneyDrunk.Telemetry.Sink.AzureMonitor/"]
COPY ["src/HoneyDrunk.Telemetry.Sink.Loki/HoneyDrunk.Telemetry.Sink.Loki.csproj", "src/HoneyDrunk.Telemetry.Sink.Loki/"]
COPY ["src/HoneyDrunk.Telemetry.Sink.Mimir/HoneyDrunk.Telemetry.Sink.Mimir.csproj", "src/HoneyDrunk.Telemetry.Sink.Mimir/"]
COPY ["src/HoneyDrunk.Telemetry.Sink.PostHog/HoneyDrunk.Telemetry.Sink.PostHog.csproj", "src/HoneyDrunk.Telemetry.Sink.PostHog/"]
COPY ["src/HoneyDrunk.Telemetry.Sink.Sentry/HoneyDrunk.Telemetry.Sink.Sentry.csproj", "src/HoneyDrunk.Telemetry.Sink.Sentry/"]
COPY ["src/HoneyDrunk.Telemetry.Sink.Tempo/HoneyDrunk.Telemetry.Sink.Tempo.csproj", "src/HoneyDrunk.Telemetry.Sink.Tempo/"]
RUN dotnet restore "src/Pulse.Collector/HoneyDrunk.Pulse.Collector.csproj"

# Recursive copy is bounded by .dockerignore at the build-context root, which
# excludes bin/, obj/, TestResults/, .git/, .vs/, .env*, secrets.json,
# appsettings.Development.json, *.pfx, *.snk, and test projects (Sonar S6470).
COPY . .
WORKDIR "/workspace/src/Pulse.Collector"
RUN dotnet build "HoneyDrunk.Pulse.Collector.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HoneyDrunk.Pulse.Collector.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HoneyDrunk.Pulse.Collector.dll"]