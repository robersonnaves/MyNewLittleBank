# Start All Services

Scripts to start all MyNewLittleBank services (infrastructure, application, and observability).

## Features

- **Cross-platform**: Bash script for Unix-like systems, PowerShell for Windows
- **Container engine detection**: Automatically uses `podman` or `docker` via `CONTAINER_ENGINE` env var
- **Mock.Transactions control**: Enable/disable mock transaction generation via CLI flags or environment variables
- **Compose file layering**: Supports optional Linux-specific and local override compose files
- **Network management**: Creates shared `bank-net` external network for cross-stack communication
- **Observability stack**: Automatically starts Prometheus, Loki, Tempo, Pyroscope, and Grafana

## Usage

### Bash (Linux/macOS)

```bash
# Start with Mock.Transactions enabled (default)
./start-all.sh

# Disable Mock.Transactions via CLI flag
./start-all.sh --no-mock-transactions
# or
./start-all.sh --mock-transactions-enabled=false

# Disable via environment variable
MOCK_TRANSACTIONS_ENABLED=false ./start-all.sh

# Use Docker instead of Podman
CONTAINER_ENGINE=docker ./start-all.sh

# Show help
./start-all.sh --help
```

### PowerShell (Windows/cross-platform)

```powershell
# Start with Mock.Transactions enabled (default)
.\start-all.ps1

# Disable Mock.Transactions via parameter
.\start-all.ps1 -MockTransactionsEnabled $false

# Disable via environment variable
$env:MOCK_TRANSACTIONS_ENABLED='false'
.\start-all.ps1

# Use Docker instead of Podman
$env:CONTAINER_ENGINE='docker'
.\start-all.ps1
```

## Compose File Layering

Both scripts support layered compose configurations:

1. **Base**: `docker-compose.yml` (always included)
2. **Linux overrides**: `docker-compose.linux.yml` (added on Linux if present, for SELinux bind options)
3. **Local overrides**: `docker-compose.override.yml` (added if present, gitignored for local dev customization)

## Network Architecture

The scripts create an external `bank-net` bridge network that is shared between:
- Main application stack (`mynewlittlebank`)
- Observability stack (`mynewlittlebank-observability`)

This allows services in both stacks to communicate by container name.

## Service Status

After startup, both scripts display:
- Main application services status
- Observability stack services status
- Mock.Transactions enable/disable status

## Troubleshooting

### Network already exists error
Ignore "network already exists" warnings - the scripts handle this gracefully.

### Podman vs Docker
Set `CONTAINER_ENGINE` to override automatic detection:
```bash
export CONTAINER_ENGINE=docker  # Use Docker
# or
export CONTAINER_ENGINE=podman  # Use Podman (default)
```

### Services can't communicate across stacks
Verify both compose files use `external: true` for the `bank-net` network.

### Exit code 1 on first run
Normal - the network creation may fail initially on some systems. Re-run the script.
