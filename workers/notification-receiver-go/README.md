# Notification Receiver Worker

Go-based HTTP service that receives insufficient funds notifications from .NET transaction workers and persists them as JSON files for auditing.

## Features

- **HTTP API**: `POST /alerts/insufficient-funds` endpoint
- **Idempotency**: Duplicate notifications (same transactionId) are handled safely
- **OpenTelemetry**: Full integration with traces, metrics, and logs
- **File Storage**: Notifications stored as `{transactionId}.json` files
- **Health Checks**: `/health` endpoint for container orchestration

## Configuration

All configuration via environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `8080` | HTTP server port |
| `NOTIFICATIONS_DIR` | `/data/notifications` | Directory for storing notification files |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `otel-collector:4317` | OpenTelemetry Collector endpoint |
| `OTEL_SERVICE_NAME` | `notification-receiver` | Service name in traces |
| `OTEL_RESOURCE_ATTRIBUTES` | - | Additional OTEL resource attributes (e.g., `deployment.environment=local`) |

## Local Development

### Prerequisites
- Go 1.22+
- Access to OpenTelemetry Collector (optional for local dev)

### Build
```bash
go build -o bin/notification-receiver ./cmd/server
```

### Run
```bash
export NOTIFICATIONS_DIR=./notifications
export PORT=8080
./bin/notification-receiver
```

### Test
```bash
go test ./...
```

## Docker

### Build Image
```bash
docker build -t notification-receiver:latest .
```

### Run Container
```bash
docker run -p 8080:8080 \
  -v notification_data:/data/notifications \
  -e OTEL_EXPORTER_OTLP_ENDPOINT=otel-collector:4317 \
  notification-receiver:latest
```

## API

### POST /alerts/insufficient-funds

Receives insufficient funds notification.

**Headers:**
- `Content-Type: application/json`
- `X-Correlation-Id` (optional): W3C Trace Context for distributed tracing

**Request Body:**
```json
{
  "cpf": "12345678901",
  "accountNumber": "10000001",
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "attemptedAmount": 1500.50,
  "availableBalance": 500.00,
  "occurredAt": "2025-12-17T10:30:00Z",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "reason": "insufficient_funds"
}
```

**Responses:**
- `202 Accepted`: Notification received and persisted (or duplicate detected)
- `400 Bad Request`: Invalid payload
- `500 Internal Server Error`: Storage failure

### GET /health

Health check endpoint.

**Response:**
- `200 OK`: Service is healthy

## Observability

### Metrics
- `notifications_received_total{status="success|duplicate|invalid_payload|storage_error"}`: Counter of notifications by status
- `notification_processing_duration_seconds`: Histogram of request latency

### Logs
Structured JSON logs with trace context:
```json
{
  "time": "2025-12-17T10:30:00Z",
  "level": "INFO",
  "msg": "notification received",
  "trace_id": "4bf92f3577b34da6a3ce929d0e0e4736",
  "span_id": "00f067aa0ba902b7",
  "transaction_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "success"
}
```

### Traces
Spans exported to OTLP Collector:
- `POST /alerts/insufficient-funds`: HTTP request span
- `storage.save`: File I/O span

## File Storage

Notifications are stored as individual JSON files:
- **Location**: `${NOTIFICATIONS_DIR}/{transactionId}.json`
- **Format**: Pretty-printed JSON matching request payload
- **Idempotency**: If file exists, no rewrite occurs (202 still returned)

## Troubleshooting

### Container won't start
- Check volume permissions: ensure `/data/notifications` is writable
- Verify OTEL_EXPORTER_OTLP_ENDPOINT is reachable

### Notifications not appearing
- Check logs for errors: `docker logs notification-receiver`
- Verify NOTIFICATIONS_DIR environment variable
- Check disk space

### Traces not in Jaeger
- Verify otel-collector is running and accessible
- Check OTEL_EXPORTER_OTLP_ENDPOINT configuration
- Review otel-collector logs for export errors
