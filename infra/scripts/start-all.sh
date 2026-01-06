#!/usr/bin/env bash
set -euo pipefail

# Colors for output
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Mimic PowerShell's try/catch for a final error message.
trap 'echo -e "\n${RED}An error occurred. Exiting.${NC}" >&2' ERR

# Detect OS and architecture
OS_TYPE="$(uname -s)"
OS_ARCH="$(uname -m)"

case "${OS_TYPE}" in
    Linux*)     OS_PLATFORM=linux;;
    Darwin*)    OS_PLATFORM=macos;;
    CYGWIN*|MINGW*|MSYS*) OS_PLATFORM=windows;;
    *)          
        echo -e "${RED}Unsupported OS: ${OS_TYPE}${NC}" >&2
        exit 1
        ;;
esac

echo -e "${CYAN}Detected platform: ${OS_PLATFORM}/${OS_ARCH}${NC}"

# Container engine detection
ENGINE="${CONTAINER_ENGINE:-podman}"
echo -e "${CYAN}Using container engine: ${ENGINE}${NC}"

# Compose files setup
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Use realpath to resolve the '..' and get a clean, absolute path
COMPOSE_BASE="$(realpath "${SCRIPT_DIR}/../docker-compose.yml")"
COMPOSE_LINUX="${SCRIPT_DIR}/../docker-compose.linux.yml"
COMPOSE_OVERRIDE="${SCRIPT_DIR}/../docker-compose.override.yml"

COMPOSE_FILES="-f ${COMPOSE_BASE}"

# Add Linux override if on Linux and file exists
if [[ "${OS_PLATFORM}" == "linux" ]] && [[ -f "${COMPOSE_LINUX}" ]]; then
    echo -e "${CYAN}Using Linux-specific overrides (SELinux)${NC}"
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_LINUX}"
fi

# Add local override if exists (gitignored)
if [[ -f "${COMPOSE_OVERRIDE}" ]]; then
    echo -e "${CYAN}Using local overrides${NC}"
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_OVERRIDE}"
fi

PROJECT_NAME="mynewlittlebank"
MOCK_TRANSACTIONS_ENABLED="${MOCK_TRANSACTIONS_ENABLED:-true}"

echo -e "${CYAN}Starting all services...${NC}"

# Ensure network exists before starting stacks
echo -e "${CYAN}Ensuring bank-net network exists...${NC}"
"${ENGINE}" network create bank-net 2>/dev/null || true

if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
  echo -e "${GREEN}Mock.Transactions enabled - starting via docker-compose${NC}"
  "${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" up -d --build
else
  echo -e "${YELLOW}Mock.Transactions disabled - scaling to 0${NC}"
  "${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" up -d --build --scale mock-transactions=0
fi

echo -e "${CYAN}Waiting for services to be ready...${NC}"
sleep 5

echo -e "\n${GREEN}All services started.${NC}"

status_message="DISABLED"
if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
    status_message="ENABLED (running in compose)"
fi
echo -e "${CYAN}Mock.Transactions status: ${status_message}${NC}"

# Start observability stack
echo -e "\n${CYAN}Starting observability stack...${NC}"
COMPOSE_OBSERVABILITY="$(realpath "${SCRIPT_DIR}/../docker-compose.observability.yml")"
"${ENGINE}" compose -f "${COMPOSE_OBSERVABILITY}" -p "${PROJECT_NAME}-observability" up -d

if [ $? -ne 0 ]; then
    echo -e "${YELLOW}Warning: Failed to start observability stack${NC}" >&2
else
    echo -e "${GREEN}Observability stack started successfully${NC}"
fi

echo -e "\n${CYAN}Service status:${NC}"
"${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" ps
echo -e "\n${CYAN}Observability stack status:${NC}"
"${ENGINE}" compose -f "${COMPOSE_OBSERVABILITY}" -p "${PROJECT_NAME}-observability" ps
