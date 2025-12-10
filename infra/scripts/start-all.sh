#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
INFRA_COMPOSE="${SCRIPT_DIR}/../docker-compose.yml"
APP_COMPOSE="${SCRIPT_DIR}/../docker-compose.app.yml"
ENGINE="${CONTAINER_ENGINE:-podman}"
PROJECT_NAME="mynewlittlebank"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
MOCK_TRANSACTIONS_ENABLED="${MOCK_TRANSACTIONS_ENABLED:-true}"
MOCK_TRANSACTIONS_PID=""

cleanup() {
  if [ -n "${MOCK_TRANSACTIONS_PID:-}" ] && kill -0 "${MOCK_TRANSACTIONS_PID}" 2>/dev/null; then
    echo "Stopping Mock.Transactions (pid ${MOCK_TRANSACTIONS_PID})..."
    kill "${MOCK_TRANSACTIONS_PID}" 2>/dev/null || true
    wait "${MOCK_TRANSACTIONS_PID}" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "Starting Infrastructure..."
"${ENGINE}" compose -f "${INFRA_COMPOSE}" -p "${PROJECT_NAME}" up -d

echo "Waiting for infrastructure to be ready..."
# Simple sleep or check. For now, trusting healthchecks in verify step or assume compose up -d waits a bit if depends_on is set (but they are separate compose files).
sleep 5

echo "Starting Application Services..."
"${ENGINE}" compose -f "${APP_COMPOSE}" -p "${PROJECT_NAME}" up -d --build --scale mock-transactions=0

if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
  echo "Starting Mock.Transactions locally..."
  cd "${REPO_ROOT}" && dotnet run --project src/Mock.Transactions/Mock.Transactions.csproj --configuration Debug > "${SCRIPT_DIR}/mock-transactions.log" 2>&1 &
  MOCK_TRANSACTIONS_PID=$!
  echo "Mock.Transactions running (pid ${MOCK_TRANSACTIONS_PID}). Logs: ${SCRIPT_DIR}/mock-transactions.log"
else
  echo "Mock.Transactions disabled (MOCK_TRANSACTIONS_ENABLED=${MOCK_TRANSACTIONS_ENABLED})."
fi

echo "All services started."
"${ENGINE}" compose -f "${INFRA_COMPOSE}" -f "${APP_COMPOSE}" -p "${PROJECT_NAME}" ps
