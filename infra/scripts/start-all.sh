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

# CLI arguments parsing (parity with PowerShell)
# Supports: --mock-transactions-enabled[=true|false], --no-mock-transactions
# Stack control: --observability-only, --skip-observability, --main-only, --skip-main
MOCK_TRANSACTIONS_ENABLED="${MOCK_TRANSACTIONS_ENABLED:-true}"
START_MAIN="true"
START_OBSERVABILITY="true"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --mock-transactions-enabled)
            shift || true
            _val="${1:-true}"
            ;;
        --mock-transactions-enabled=*)
            _val="${1#*=}"
            ;;
        --no-mock-transactions|--disable-mock-transactions)
            _val="false"
            ;;
        --observability-only)
            START_MAIN="false"
            START_OBSERVABILITY="true"
            ;;
        --main-only|--app-only)
            START_MAIN="true"
            START_OBSERVABILITY="false"
            ;;
        --skip-observability|--no-observability)
            START_OBSERVABILITY="false"
            ;;
        --skip-main|--no-main)
            START_MAIN="false"
            ;;
        -h|--help)
            echo "Usage: ${0##*/} [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --mock-transactions-enabled[=true|false]  Enable/disable Mock.Transactions"
            echo "  --no-mock-transactions                     Disable Mock.Transactions"
            echo "  --observability-only                       Start only observability stack"
            echo "  --main-only, --app-only                    Start only main application stack"
            echo "  --skip-observability, --no-observability   Skip observability stack"
            echo "  --skip-main, --no-main                     Skip main application stack"
            echo "  -h, --help                                 Show this help message"
            exit 0
            ;;
        --)
            shift; break
            ;;
        -*)
            echo -e "${YELLOW}Unknown option: $1${NC}" >&2
            ;;
        *)
            # stop on first non-option to allow positional args in future
            break
            ;;
    esac

    if [[ -n "${_val:-}" ]]; then
        case "${_val}" in
            true|TRUE|True|1|yes|YES|on|ON)
                MOCK_TRANSACTIONS_ENABLED="true" ;;
            false|FALSE|False|0|no|NO|off|OFF)
                MOCK_TRANSACTIONS_ENABLED="false" ;;
            *)
                echo -e "${YELLOW}Invalid value for --mock-transactions-enabled: ${_val}${NC}" >&2
                exit 2 ;;
        esac
        unset _val
    fi
    shift || true
done

# Validate flag combinations
if [[ "${START_MAIN}" = "false" ]] && [[ "${START_OBSERVABILITY}" = "false" ]]; then
    echo -e "${YELLOW}Warning: Both stacks disabled. Nothing to start.${NC}"
    exit 0
fi

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

echo -e "${CYAN}Starting services...${NC}"

# Ensure network exists before starting stacks (required by observability)
echo -e "${CYAN}Ensuring bank-net network exists...${NC}"
"${ENGINE}" network create bank-net 2>/dev/null || true

if [ "${START_MAIN}" = "true" ]; then
    echo -e "${CYAN}Starting main application stack...${NC}"
    if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
        echo -e "${GREEN}Mock.Transactions enabled - starting via docker-compose${NC}"
        "${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" up -d --build
    else
        echo -e "${YELLOW}Mock.Transactions disabled - scaling to 0${NC}"
        "${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" up -d --build --scale mock-transactions=0
    fi
    echo -e "${CYAN}Waiting for services to be ready...${NC}"
    sleep 5
    echo -e "\n${GREEN}Main application stack started.${NC}"
else
    echo -e "${YELLOW}Main application stack skipped.${NC}"
fi

if [ "${START_MAIN}" = "true" ]; then
    status_message="DISABLED"
    if [ "${MOCK_TRANSACTIONS_ENABLED}" = "true" ]; then
            status_message="ENABLED (running in compose)"
    fi
    echo -e "${CYAN}Mock.Transactions status: ${status_message}${NC}"
fi

# Start observability stack
if [ "${START_OBSERVABILITY}" = "true" ]; then
    echo -e "\n${CYAN}Starting observability stack...${NC}"
    COMPOSE_OBSERVABILITY="$(realpath "${SCRIPT_DIR}/../docker-compose.observability.yml")"
    "${ENGINE}" compose -f "${COMPOSE_OBSERVABILITY}" -p "${PROJECT_NAME}-observability" up -d

    if [ $? -ne 0 ]; then
            echo -e "${YELLOW}Warning: Failed to start observability stack${NC}" >&2
    else
            echo -e "${GREEN}Observability stack started successfully${NC}"
    fi
else
    echo -e "${YELLOW}Observability stack skipped.${NC}"
fi

echo -e "\n${CYAN}Service status:${NC}"
if [ "${START_MAIN}" = "true" ]; then
    "${ENGINE}" compose ${COMPOSE_FILES} -p "${PROJECT_NAME}" ps
else
    echo -e "${YELLOW}Main stack: SKIPPED${NC}"
fi

if [ "${START_OBSERVABILITY}" = "true" ]; then
    echo -e "\n${CYAN}Observability stack status:${NC}"
    COMPOSE_OBSERVABILITY="$(realpath "${SCRIPT_DIR}/../docker-compose.observability.yml")"
    "${ENGINE}" compose -f "${COMPOSE_OBSERVABILITY}" -p "${PROJECT_NAME}-observability" ps
else
    echo -e "${YELLOW}Observability stack: SKIPPED${NC}"
fi
