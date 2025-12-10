#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
INFRA_COMPOSE="${SCRIPT_DIR}/../docker-compose.yml"
APP_COMPOSE="${SCRIPT_DIR}/../docker-compose.app.yml"
ENGINE="${CONTAINER_ENGINE:-podman}"
PROJECT_NAME="mynewlittlebank"

echo "Starting Infrastructure..."
"${ENGINE}" compose -f "${INFRA_COMPOSE}" -p "${PROJECT_NAME}" up -d

echo "Waiting for infrastructure to be ready..."
# Simple sleep or check. For now, trusting healthchecks in verify step or assume compose up -d waits a bit if depends_on is set (but they are separate compose files).
sleep 5

echo "Starting Application Services..."
"${ENGINE}" compose -f "${APP_COMPOSE}" -p "${PROJECT_NAME}" up -d --build

echo "All services started."
"${ENGINE}" compose -f "${INFRA_COMPOSE}" -f "${APP_COMPOSE}" -p "${PROJECT_NAME}" ps
