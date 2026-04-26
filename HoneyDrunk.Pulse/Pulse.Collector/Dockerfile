# Build context: HoneyDrunk.Pulse/ (solution root)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Pulse.Collector/HoneyDrunk.Pulse.Collector.csproj", "Pulse.Collector/"]
COPY ["HoneyDrunk.Pulse.Contracts/HoneyDrunk.Pulse.Contracts.csproj", "HoneyDrunk.Pulse.Contracts/"]
COPY ["HoneyDrunk.Telemetry.Abstractions/HoneyDrunk.Telemetry.Abstractions.csproj", "HoneyDrunk.Telemetry.Abstractions/"]
COPY ["HoneyDrunk.Telemetry.OpenTelemetry/HoneyDrunk.Telemetry.OpenTelemetry.csproj", "HoneyDrunk.Telemetry.OpenTelemetry/"]
COPY ["HoneyDrunk.Telemetry.Sink.AzureMonitor/HoneyDrunk.Telemetry.Sink.AzureMonitor.csproj", "HoneyDrunk.Telemetry.Sink.AzureMonitor/"]
COPY ["HoneyDrunk.Telemetry.Sink.Loki/HoneyDrunk.Telemetry.Sink.Loki.csproj", "HoneyDrunk.Telemetry.Sink.Loki/"]
COPY ["HoneyDrunk.Telemetry.Sink.Mimir/HoneyDrunk.Telemetry.Sink.Mimir.csproj", "HoneyDrunk.Telemetry.Sink.Mimir/"]
COPY ["HoneyDrunk.Telemetry.Sink.PostHog/HoneyDrunk.Telemetry.Sink.PostHog.csproj", "HoneyDrunk.Telemetry.Sink.PostHog/"]
COPY ["HoneyDrunk.Telemetry.Sink.Sentry/HoneyDrunk.Telemetry.Sink.Sentry.csproj", "HoneyDrunk.Telemetry.Sink.Sentry/"]
COPY ["HoneyDrunk.Telemetry.Sink.Tempo/HoneyDrunk.Telemetry.Sink.Tempo.csproj", "HoneyDrunk.Telemetry.Sink.Tempo/"]
RUN dotnet restore "Pulse.Collector/HoneyDrunk.Pulse.Collector.csproj"

COPY . .
WORKDIR "/src/Pulse.Collector"
RUN dotnet build "HoneyDrunk.Pulse.Collector.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HoneyDrunk.Pulse.Collector.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HoneyDrunk.Pulse.Collector.dll"]