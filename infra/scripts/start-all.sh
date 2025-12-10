#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${SCRIPT_DIR}/../docker-compose.yml"
ENGINE="${CONTAINER_ENGINE:-podman}"
PROJECT_NAME="mynewlittlebank"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
MOCK_TRANSACTIONS_ENABLED="${MOCK_TRANSACTIONS_ENABLED:-true}"

# Docker-compose handles all process management
cleanup() {
  echo "Docker-compose will handle cleanup"
}
trap cleanup EXIT INT TERM

echo "Starting all services..."

if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
  echo "Mock.Transactions enabled - starting via docker-compose"
  "${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" --profile local up -d --build
else
  echo "Mock.Transactions disabled - scaling to 0"
  "${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" --profile local up -d --build --scale mock-transactions=0
fi

echo "Waiting for services to be ready..."
sleep 5

echo "All services started."
if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
  echo "Mock.Transactions status: ENABLED (running in compose)"
else
  echo "Mock.Transactions status: DISABLED"
fi

echo ""
echo "Service status:"
"${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" --profile local ps
