#!/usr/bin/env bash
set -euo pipefail

# Colors for output
CYAN='\033[0;36m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Mimic PowerShell's try/catch for a final error message.
trap 'echo -e "\n${RED}An error occurred. Exiting.${NC}" >&2' ERR

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# Use realpath to resolve the '..' and get a clean, absolute path
COMPOSE_FILE="$(realpath "${SCRIPT_DIR}/../docker-compose.yml")"
ENGINE="${CONTAINER_ENGINE:-podman}"
PROJECT_NAME="mynewlittlebank"

echo -e "${CYAN}Stopping all services...${NC}"

"${ENGINE}" compose -f "${COMPOSE_FILE}" -p "${PROJECT_NAME}" down

echo -e "\n${GREEN}All services stopped.${NC}"