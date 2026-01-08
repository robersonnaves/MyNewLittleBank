#!/usr/bin/env bash
set -euo pipefail

# Colors for output
CYAN='\033[0;36m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Mimic PowerShell's try/catch for a final error message.
trap 'echo -e "\n${RED}An error occurred. Exiting.${NC}" >&2' ERR

# CLI arguments parsing
# Stack control: --observability-only, --main-only, --skip-observability, --skip-main
STOP_MAIN="true"
STOP_OBSERVABILITY="true"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --observability-only)
            STOP_MAIN="false"
            STOP_OBSERVABILITY="true"
            ;;
        --main-only|--app-only)
            STOP_MAIN="true"
            STOP_OBSERVABILITY="false"
            ;;
        --skip-observability|--no-observability)
            STOP_OBSERVABILITY="false"
            ;;
        --skip-main|--no-main)
            STOP_MAIN="false"
            ;;
        -h|--help)
            echo "Usage: ${0##*/} [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --observability-only                       Stop only observability stack"
            echo "  --main-only, --app-only                    Stop only main application stack"
            echo "  --skip-observability, --no-observability   Skip observability stack"
            echo "  --skip-main, --no-main                     Skip main application stack"
            echo "  -h, --help                                 Show this help message"
            exit 0
            ;;
        --)
            shift; break
            ;;
        -*)
            echo "Unknown option: $1" >&2
            ;;
        *)
            break
            ;;
    esac
    shift || true
done

# Validate flag combinations
if [[ "${STOP_MAIN}" = "false" ]] && [[ "${STOP_OBSERVABILITY}" = "false" ]]; then
    echo -e "${CYAN}Warning: Both stacks disabled. Nothing to stop.${NC}"
    exit 0
fi

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

echo -e "${CYAN}Stopping services...${NC}"

if [ "${STOP_MAIN}" = "true" ]; then
    echo -e "${CYAN}Stopping main application stack...${NC}"
    "${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" down
    echo -e "${GREEN}Main application stack stopped.${NC}"
else
    echo -e "${CYAN}Main application stack skipped.${NC}"
fi

if [ "${STOP_OBSERVABILITY}" = "true" ]; then
    echo -e "${CYAN}Stopping observability stack...${NC}"
    COMPOSE_OBSERVABILITY="$(realpath "${SCRIPT_DIR}/../docker-compose.observability.yml")"
    "${ENGINE}" compose -f "${COMPOSE_OBSERVABILITY}" -p "${PROJECT_NAME}-observability" down
    echo -e "${GREEN}Observability stack stopped.${NC}"
else
    echo -e "${CYAN}Observability stack skipped.${NC}"
fi

echo -e "\n${GREEN}Done.${NC}"