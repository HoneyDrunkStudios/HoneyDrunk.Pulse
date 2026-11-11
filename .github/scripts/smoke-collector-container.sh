#!/usr/bin/env bash
# Validate the deployable image without credentials or network access to providers.
set -euo pipefail

IMAGE="${1:?Pass the locally built Collector image}"
CONTAINER="pulse-smoke-${GITHUB_RUN_ID:-local}-${GITHUB_RUN_ATTEMPT:-1}"
CREATED=false
cleanup() {
  local result=$?
  if [[ "$CREATED" == true ]]; then
    docker logs --tail 100 "$CONTAINER" || true
    docker rm --force "$CONTAINER" >/dev/null || true
  fi
  return "$result"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

# No published ports or provider routes. /tmp is the only writable filesystem.
docker create --name "$CONTAINER" \
  --network none --read-only --tmpfs /tmp:rw,nosuid,noexec,size=64m \
  --cap-drop ALL --security-opt no-new-privileges \
  --cpus 1 --memory 512m --pids-limit 128 \
  --env HOME=/tmp --env DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  --env VSTEST_TELEMETRY_OPTEDIN=0 --env POWERSHELL_TELEMETRY_OPTOUT=1 \
  --env DOTNET_ROLL_FORWARD_TO_PRERELEASE=0 \
  --env ASPNETCORE_ENVIRONMENT=Development --env DOTNET_ENVIRONMENT=Development \
  --env ASPNETCORE_URLS=http://127.0.0.1:8080 \
  --env HoneyDrunk__Pulse__Collector__TransportAdapter=InMemory \
  --env HoneyDrunk__Pulse__Collector__EnableTransportPublishing=true \
  "$IMAGE" >/dev/null
CREATED=true
docker start "$CONTAINER" >/dev/null

[[ "$(docker exec "$CONTAINER" id -u)" == 1654 ]]
RUNTIMES=$(docker exec "$CONTAINER" dotnet --list-runtimes)
printf '%s\n' "$RUNTIMES"
grep -Eq '^Microsoft.NETCore.App 11\.0\.[0-9]+ ' <<< "$RUNTIMES"
grep -Eq '^Microsoft.AspNetCore.App 11\.0\.[0-9]+ ' <<< "$RUNTIMES"

# Use the image's Bash TCP support; no curl installation or helper image is needed.
probe() {
  docker exec --interactive "$CONTAINER" timeout 5 bash -s -- "$@" <<'HTTP'
set -euo pipefail
method=$1 path=$2 expected=$3 body=${4:-}
exec 3<>/dev/tcp/127.0.0.1/8080
printf '%s %s HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: %s\r\n\r\n%s' "$method" "$path" "${#body}" "$body" >&3
IFS= read -r status <&3
cat <&3 >/dev/null
[[ "$status" == "HTTP/1.1 $expected "* ]]
HTTP
}

READY=false
for ((attempt = 0; attempt < 30; attempt++)); do
  [[ "$(docker inspect --format '{{.State.Running}}' "$CONTAINER")" == true ]]
  if probe GET /health/ready 200 2>/dev/null; then
    READY=true
    break
  fi
  sleep 1
done
[[ "$READY" == true ]]
probe GET /health 200
probe GET /health/live 200
probe POST /otlp/v1/traces 200 '{}'
probe POST /otlp/v1/metrics 200 '{}'
probe POST /otlp/v1/logs 200 '{}'
probe POST /otlp/v1/analytics 200 '{"events":[{"eventName":"container.smoke","distinctId":"synthetic-test"}],"sourceService":"ContainerSmoke"}'
probe POST /otlp/v1/analytics 400 '{"events":[]}'

# Exercise the genuine Transport lifecycle, including orderly host disposal.
docker stop --time 20 "$CONTAINER" >/dev/null
[[ "$(docker inspect --format '{{.State.ExitCode}}' "$CONTAINER")" == 0 ]]
[[ "$(docker inspect --format '{{.State.OOMKilled}}' "$CONTAINER")" == false ]]
LOGS=$(docker logs "$CONTAINER" 2>&1)
if grep -Eq 'ObjectDisposedException|Unhandled exception' <<< "$LOGS"; then
  echo 'Collector shutdown contained an unhandled lifecycle failure.' >&2
  exit 1
fi
echo 'Collector image passed GA runtime, non-root, readiness, ingestion and shutdown checks.'
