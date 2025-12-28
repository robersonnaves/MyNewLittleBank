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

echo -e "\n${CYAN}Service status:${NC}"
"${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" ps
