#!/usr/bin/env bash
set -euo pipefail

# Default values
FORCE=false
BACKUP=false
CONNECTION_STRING=""

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --force)
      FORCE=true
      shift
      ;;
    --backup)
      BACKUP=true
      shift
      ;;
    --connection-string)
      CONNECTION_STRING="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [--force] [--backup] [--connection-string <string>]"
      exit 1
      ;;
  esac
done

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
API_PROJECT="${REPO_ROOT}/src/API/API.csproj"
INFRA_DB_PROJECT="${REPO_ROOT}/src/Infra.Database/Infra.Database.csproj"
MIGRATIONS_FOLDER="${REPO_ROOT}/src/Infra.Database/Migrations"
APPSETTINGS_PATH="${REPO_ROOT}/src/API/appsettings.json"

# Validate projects exist
if [ ! -f "${API_PROJECT}" ]; then
  echo "Error: API project not found at ${API_PROJECT}"
  exit 1
fi

if [ ! -f "${INFRA_DB_PROJECT}" ]; then
  echo "Error: Infra.Database project not found at ${INFRA_DB_PROJECT}"
  exit 1
fi

# Get connection string
if [ -z "${CONNECTION_STRING}" ]; then
  if [ -f "${APPSETTINGS_PATH}" ]; then
    CONNECTION_STRING=$(grep -A 1 '"ConnectionStrings"' "${APPSETTINGS_PATH}" | grep '"DefaultConnection"' | sed 's/.*"DefaultConnection": "\(.*\)".*/\1/')
  fi
  
  if [ -z "${CONNECTION_STRING}" ]; then
    echo "Error: Could not find connection string in appsettings.json"
    exit 1
  fi
fi

echo ""
echo "=== MyNewLittleBank Database Reset ==="
echo "Connection String: ${CONNECTION_STRING}"
echo ""

# Parse connection string
DB_HOST=$(echo "${CONNECTION_STRING}" | sed -n 's/.*Host=\([^;]*\).*/\1/p')
DB_PORT=$(echo "${CONNECTION_STRING}" | sed -n 's/.*Port=\([^;]*\).*/\1/p')
DB_NAME=$(echo "${CONNECTION_STRING}" | sed -n 's/.*Database=\([^;]*\).*/\1/p')
DB_USER=$(echo "${CONNECTION_STRING}" | sed -n 's/.*Username=\([^;]*\).*/\1/p')
DB_PASSWORD=$(echo "${CONNECTION_STRING}" | sed -n 's/.*Password=\([^;]*\).*/\1/p')

DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-mynewlittlebank}
DB_USER=${DB_USER:-postgres}
DB_PASSWORD=${DB_PASSWORD:-postgres}

# Confirm action
if [ "${FORCE}" != "true" ]; then
  echo "WARNING: This will:"
  echo "  1. Drop database '${DB_NAME}'"
  echo "  2. Delete all migrations in '${MIGRATIONS_FOLDER}'"
  echo "  3. Create a new InitialCreate migration"
  echo "  4. Update the database with the new migration"
  echo ""
  read -p "Are you sure you want to continue? (yes/no): " confirmation
  if [ "${confirmation}" != "yes" ]; then
    echo "Operation cancelled."
    exit 0
  fi
fi

# Check if dotnet ef tool is installed
echo ""
echo "Checking dotnet ef tool..."
if ! dotnet ef --version &> /dev/null; then
  echo "Error: dotnet ef tool not installed. Installing..."
  dotnet tool install --global dotnet-ef
  if [ $? -ne 0 ]; then
    echo "Error: Failed to install dotnet ef tool"
    exit 1
  fi
fi
echo "✓ dotnet ef found"

# Backup database if requested
if [ "${BACKUP}" = "true" ]; then
  echo ""
  echo "Creating database backup..."
  TIMESTAMP=$(date +%Y%m%d_%H%M%S)
  BACKUP_FILE="${REPO_ROOT}/backup_${DB_NAME}_${TIMESTAMP}.sql"
  
  export PGPASSWORD="${DB_PASSWORD}"
  pg_dump -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -f "${BACKUP_FILE}" 2>&1
  
  if [ $? -eq 0 ] && [ -f "${BACKUP_FILE}" ]; then
    echo "✓ Backup created: ${BACKUP_FILE}"
  else
    echo "Warning: Backup failed, but continuing..."
  fi
  unset PGPASSWORD
fi

# Drop database
echo ""
echo "Dropping database '${DB_NAME}'..."
export PGPASSWORD="${DB_PASSWORD}"
if [ "${FORCE}" = "true" ]; then
  echo "Force disconnecting all sessions from database '${DB_NAME}'..."
  psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres -c "SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '${DB_NAME}' AND pid <> pg_backend_pid();" 2>&1
  if [ $? -ne 0 ]; then
    echo "Warning: Failed to force disconnect sessions."
  else
    echo "✓ Sessions disconnected."
  fi
fi
psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d postgres -c "DROP DATABASE IF EXISTS \"${DB_NAME}\";" 2>&1

if [ $? -eq 0 ]; then
  echo "✓ Database dropped successfully"
else
  echo "Warning: Could not drop database (it may not exist)"
fi
unset PGPASSWORD

# Remove existing migrations
echo ""
echo "Removing existing migrations..."
if [ -d "${MIGRATIONS_FOLDER}" ]; then
  FILE_COUNT=$(find "${MIGRATIONS_FOLDER}" -type f | wc -l)
  if [ "${FILE_COUNT}" -gt 0 ]; then
    rm -f "${MIGRATIONS_FOLDER}"/*
    echo "✓ Removed ${FILE_COUNT} migration file(s)"
  else
    echo "✓ No migrations to remove"
  fi
else
  echo "✓ Migrations folder doesn't exist"
fi

# Create new initial migration
echo ""
echo "Creating new InitialCreate migration..."
dotnet ef migrations add InitialCreate \
  --project "${INFRA_DB_PROJECT}" \
  --startup-project "${API_PROJECT}" \
  --context MyNewLittleBankContext \
  --output-dir Migrations

if [ $? -ne 0 ]; then
  echo "Error: Failed to create migration"
  exit 1
fi
echo "✓ Migration created successfully"

# Update database
echo ""
echo "Updating database..."
dotnet ef database update \
  --project "${INFRA_DB_PROJECT}" \
  --startup-project "${API_PROJECT}" \
  --context MyNewLittleBankContext \
  --connection "${CONNECTION_STRING}"

if [ $? -ne 0 ]; then
  echo "Error: Failed to update database"
  exit 1
fi

echo ""
echo "✓ Database reset complete!"
echo ""
echo "Summary:"
echo "  - Database: ${DB_NAME}"
echo "  - Host: ${DB_HOST}:${DB_PORT}"
echo "  - Migrations: New InitialCreate migration created"
