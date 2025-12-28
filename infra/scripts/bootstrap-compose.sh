#!/usr/bin/env bash
set -euo pipefail

PROFILE="${1:-local}"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ENGINE="${CONTAINER_ENGINE:-podman}"

export COMPOSE_PROJECT_NAME="mynewlittlebank"

# Detect OS and architecture
OS_TYPE="$(uname -s)"
OS_ARCH="$(uname -m)"

case "${OS_TYPE}" in
    Linux*)     OS_PLATFORM=linux;;
    Darwin*)    OS_PLATFORM=macos;;
    CYGWIN*|MINGW*|MSYS*) OS_PLATFORM=windows;;
    *)          OS_PLATFORM=unknown;;
esac

echo "Detected platform: ${OS_PLATFORM}/${OS_ARCH}"
echo "Using container engine: ${ENGINE}"

# Compose files setup
COMPOSE_BASE="${SCRIPT_DIR}/../docker-compose.yml"
COMPOSE_LINUX="${SCRIPT_DIR}/../docker-compose.linux.yml"
COMPOSE_OVERRIDE="${SCRIPT_DIR}/../docker-compose.override.yml"

COMPOSE_FILES="-f ${COMPOSE_BASE}"

# Add Linux override if on Linux and file exists
if [[ "${OS_PLATFORM}" == "linux" ]] && [[ -f "${COMPOSE_LINUX}" ]]; then
    echo "Using Linux-specific overrides (SELinux)"
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_LINUX}"
fi

# Add local override if exists (gitignored)
if [[ -f "${COMPOSE_OVERRIDE}" ]]; then
    echo "Using local overrides"
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_OVERRIDE}"
fi

if [[ "${PROFILE}" == "ci" ]]; then
  if [[ "${ENGINE}" == "docker" ]]; then
    "${ENGINE}" compose ${COMPOSE_FILES} --profile "${PROFILE}" down -v --remove-orphans || true
  else
    "${ENGINE}" compose ${COMPOSE_FILES} down -v --remove-orphans || true
  fi
fi

if [[ "${ENGINE}" == "docker" ]]; then
  "${ENGINE}" compose ${COMPOSE_FILES} --profile "${PROFILE}" up -d
  "${ENGINE}" compose ${COMPOSE_FILES} --profile "${PROFILE}" ps
elif [[ "${ENGINE}" == "podman" ]]; then
  "${ENGINE}" compose ${COMPOSE_FILES} up -d
  "${ENGINE}" compose ${COMPOSE_FILES} ps
else
  "${ENGINE}" compose ${COMPOSE_FILES} up -d
  "${ENGINE}" compose ${COMPOSE_FILES} ps
fi
