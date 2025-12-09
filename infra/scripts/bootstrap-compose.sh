#!/usr/bin/env bash
set -euo pipefail

PROFILE="${1:-local}"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${SCRIPT_DIR}/../docker-compose.yml"
ENGINE="${CONTAINER_ENGINE:-podman}"

export COMPOSE_PROJECT_NAME="mynewlittlebank"

if [[ "${PROFILE}" == "ci" ]]; then
  if [[ "${ENGINE}" == "docker" ]]; then
    "${ENGINE}" compose -f "${COMPOSE_FILE}" --profile "${PROFILE}" down -v --remove-orphans || true
  else
    "${ENGINE}" compose -f "${COMPOSE_FILE}" down -v --remove-orphans || true
  fi
fi

if [[ "${ENGINE}" == "docker" ]]; then
  "${ENGINE}" compose -f "${COMPOSE_FILE}" --profile "${PROFILE}" up -d
  "${ENGINE}" compose -f "${COMPOSE_FILE}" --profile "${PROFILE}" ps
elif [[ "${ENGINE}" == "podman" ]]; then
  "${ENGINE}" compose -f "${COMPOSE_FILE}" up -d
  "${ENGINE}" compose -f "${COMPOSE_FILE}" ps
else
  "${ENGINE}" compose -f "${COMPOSE_FILE}" up -d
  "${ENGINE}" compose -f "${COMPOSE_FILE}" ps
fi
