# Build stage
FROM golang:1.24-alpine AS builder

# Build arguments for multi-platform support
ARG TARGETOS
ARG TARGETARCH

WORKDIR /build

# Copy go mod files
COPY go.mod go.sum ./
RUN go mod download

# Copy source code
COPY . .

# Build binary with platform-specific variables
RUN CGO_ENABLED=0 GOOS=${TARGETOS} GOARCH=${TARGETARCH} \
    go build -a -installsuffix cgo -ldflags="-s -w" \
    -o notification-receiver ./cmd/server

# Runtime stage
FROM alpine:latest

# Install ca-certificates for HTTPS and curl for healthcheck
RUN apk --no-cache add ca-certificates curl

WORKDIR /app

# Copy binary from builder
COPY --from=builder /build/notification-receiver .

# Create data directory
RUN mkdir -p /data/notifications

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set default environment variables
ENV PORT=8080 \
    NOTIFICATIONS_DIR=/data/notifications \
    OTEL_SERVICE_NAME=notification-receiver

# Use entrypoint to fix permissions, then run as root for now
# TODO: Properly handle user permissions in production
ENTRYPOINT ["/entrypoint.sh"]
CMD ["./notification-receiver"]
