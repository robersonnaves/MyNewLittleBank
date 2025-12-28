#!/usr/bin/env bash
set -euo pipefail

PROFILE="${1:-ci}"
ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
ENGINE="${CONTAINER_ENGINE:-podman}"

# Detect OS
OS_TYPE="$(uname -s)"
case "${OS_TYPE}" in
    Linux*)     OS_PLATFORM=linux;;
    Darwin*)    OS_PLATFORM=macos;;
    *)          OS_PLATFORM=unknown;;
esac

# Compose files
COMPOSE_BASE="${ROOT_DIR}/infra/docker-compose.yml"
COMPOSE_LINUX="${ROOT_DIR}/infra/docker-compose.linux.yml"
COMPOSE_FILES="-f ${COMPOSE_BASE}"

if [[ "${OS_PLATFORM}" == "linux" ]] && [[ -f "${COMPOSE_LINUX}" ]]; then
    COMPOSE_FILES="${COMPOSE_FILES} -f ${COMPOSE_LINUX}"
fi

"${ROOT_DIR}/infra/scripts/bootstrap-compose.sh" "${PROFILE}"

dotnet test "${ROOT_DIR}/tests/Integration/MyNewLittleBank.Tests.Integration.csproj" --filter "Category=Integration" --logger "trx;LogFileName=integration.trx" --results-directory "${ROOT_DIR}/artifacts/test-results"

if [[ "${PROFILE}" == "ci" ]]; then
  "${ENGINE}" compose ${COMPOSE_FILES} --profile "${PROFILE}" down -v --remove-orphans
fi
