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

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Use realpath to resolve the '..' and get a clean, absolute path
COMPOSE_FILE="$(realpath "${SCRIPT_DIR}/../docker-compose.yml")"
ENGINE="${CONTAINER_ENGINE:-podman}"
PROJECT_NAME="mynewlittlebank"
MOCK_TRANSACTIONS_ENABLED="${MOCK_TRANSACTIONS_ENABLED:-true}"

echo -e "${CYAN}Starting all services...${NC}"

if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
  echo -e "${GREEN}Mock.Transactions enabled - starting via docker-compose${NC}"
  "${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" up -d --build
else
  echo -e "${YELLOW}Mock.Transactions disabled - scaling to 0${NC}"
  "${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" up -d --build --scale mock-transactions=0
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
"${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" ps
