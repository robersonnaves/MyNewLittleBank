#!/usr/bin/env bash
set -euo pipefail

# Colors for output
CYAN='\033[0;36m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Mimic PowerShell's try/catch for a final error message.
trap 'echo -e "\n${RED}An error occurred. Exiting.${NC}" >&2' ERR

# Detect OS
OS_TYPE="$(uname -s)"
case "${OS_TYPE}" in
    Linux*)     OS_PLATFORM=linux;;
    Darwin*)    OS_PLATFORM=macos;;
    *)          OS_PLATFORM=unknown;;
esac

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Use realpath to resolve the '..' and get a clean, absolute path
COMPOSE_BASE="$(realpath "${SCRIPT_DIR}/../docker-compose.yml")"
COMPOSE_LINUX="${SCRIPT_DIR}/../docker-compose.linux.yml"
COMPOSE_OVERRIDE="${SCRIPT_DIR}/../docker-compose.override.yml"

COMPOSE_FILES="-f ${COMPOSE_BASE}"

if [[ "${OS_PLATFORM}" == "linux" ]] && [[ -f "${COMPOSE_LINUX}" ]]; then
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_LINUX}"
fi

if [[ -f "${COMPOSE_OVERRIDE}" ]]; then
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_OVERRIDE}"
fi

ENGINE="${CONTAINER_ENGINE:-podman}"
PROJECT_NAME="mynewlittlebank"

echo -e "${CYAN}Stopping all services...${NC}"

"${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" down

echo -e "${CYAN}Stopping observability stack...${NC}"
COMPOSE_OBSERVABILITY="$(realpath "${SCRIPT_DIR}/../docker-compose.observability.yml")"
"${ENGINE}" compose -f "${COMPOSE_OBSERVABILITY}" -p "${PROJECT_NAME}-observability" down

echo -e "\n${GREEN}All services stopped.${NC}"