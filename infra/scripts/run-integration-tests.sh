#!/usr/bin/env bash
set -euo pipefail

PROFILE="${1:-ci}"
ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
ENGINE="${CONTAINER_ENGINE:-docker}"

"${ROOT_DIR}/infra/scripts/bootstrap-compose.sh" "${PROFILE}"

dotnet test "${ROOT_DIR}/tests/Integration/MyNewLittleBank.Tests.Integration.csproj" --filter "Category=Integration" --logger "trx;LogFileName=integration.trx" --results-directory "${ROOT_DIR}/artifacts/test-results"

if [[ "${PROFILE}" == "ci" ]]; then
  "${ENGINE}" compose -f "${ROOT_DIR}/infra/docker-compose.yml" --profile "${PROFILE}" down -v --remove-orphans
fi
