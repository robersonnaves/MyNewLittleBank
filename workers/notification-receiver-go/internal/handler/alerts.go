package handler

import (
	"context"
	"encoding/json"
	"errors"
	"log/slog"
	"net/http"
	"time"

	"github.com/roberson/mynewlittlebank/notification-receiver/internal/storage"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/metric"
	"go.opentelemetry.io/otel/trace"
)

// InsufficientFundsNotification matches the .NET DTO
type InsufficientFundsNotification struct {
	CPF              string    `json:"cpf"`
	AccountNumber    string    `json:"accountNumber"`
	TransactionID    string    `json:"transactionId"`
	AttemptedAmount  float64   `json:"attemptedAmount"`
	AvailableBalance float64   `json:"availableBalance"`
	OccurredAt       time.Time `json:"occurredAt"`
	TraceID          string    `json:"traceId"`
	Reason           string    `json:"reason"`
}

// Validate checks if all required fields are present
func (n *InsufficientFundsNotification) Validate() error {
	if n.CPF == "" {
		return errors.New("cpf is required")
	}
	if n.AccountNumber == "" {
		return errors.New("accountNumber is required")
	}
	if n.TransactionID == "" {
		return errors.New("transactionId is required")
	}
	if n.Reason == "" {
		return errors.New("reason is required")
	}
	if n.OccurredAt.IsZero() {
		return errors.New("occurredAt is required")
	}
	return nil
}

// AlertsHandler handles POST /alerts/insufficient-funds
type AlertsHandler struct {
	storage  *storage.FileWriter
	tracer   trace.Tracer
	counter  metric.Int64Counter
	duration metric.Float64Histogram
}

// NewAlertsHandler creates a new alerts handler
func NewAlertsHandler(storage *storage.FileWriter, tracer trace.Tracer, meter metric.Meter) (*AlertsHandler, error) {
	counter, err := meter.Int64Counter(
		"notifications_received_total",
		metric.WithDescription("Total notifications received by status"),
	)
	if err != nil {
		return nil, err
	}

	duration, err := meter.Float64Histogram(
		"notification_processing_duration_seconds",
		metric.WithDescription("Duration of notification processing"),
	)
	if err != nil {
		return nil, err
	}

	return &AlertsHandler{
		storage:  storage,
		tracer:   tracer,
		counter:  counter,
		duration: duration,
	}, nil
}

// ServeHTTP handles the HTTP request
func (h *AlertsHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	start := time.Now()
	ctx := r.Context()
	ctx, span := h.tracer.Start(ctx, "POST /alerts/insufficient-funds")
	defer span.End()

	// Parse request body
	var notification InsufficientFundsNotification
	if err := json.NewDecoder(r.Body).Decode(&notification); err != nil {
		h.recordMetrics(ctx, "invalid_payload", start)
		slog.WarnContext(ctx, "failed to decode request body", "error", err)
		http.Error(w, `{"error":"invalid JSON payload"}`, http.StatusBadRequest)
		return
	}

	// Validate payload
	if err := notification.Validate(); err != nil {
		h.recordMetrics(ctx, "invalid_payload", start)
		slog.WarnContext(ctx, "invalid notification payload", "error", err, "transaction_id", notification.TransactionID)
		http.Error(w, `{"error":"`+err.Error()+`"}`, http.StatusBadRequest)
		return
	}

	span.SetAttributes(
		attribute.String("transaction_id", notification.TransactionID),
		attribute.String("account_number", notification.AccountNumber),
	)

	// Save notification
	err := h.storage.Save(ctx, notification.TransactionID, notification)
	if err != nil {
		if errors.Is(err, storage.ErrAlreadyExists) {
			// Idempotent case: duplicate notification
			h.recordMetrics(ctx, "duplicate", start)
			slog.InfoContext(ctx, "duplicate notification received", "transaction_id", notification.TransactionID)
			w.WriteHeader(http.StatusAccepted)
			return
		}

		// Storage error
		h.recordMetrics(ctx, "storage_error", start)
		slog.ErrorContext(ctx, "failed to save notification", "error", err, "transaction_id", notification.TransactionID)
		http.Error(w, `{"error":"failed to save notification"}`, http.StatusInternalServerError)
		return
	}

	// Success
	h.recordMetrics(ctx, "success", start)
	slog.InfoContext(ctx, "notification received", "transaction_id", notification.TransactionID, "cpf", notification.CPF)
	w.WriteHeader(http.StatusAccepted)
}

func (h *AlertsHandler) recordMetrics(ctx context.Context, status string, start time.Time) {
	duration := time.Since(start).Seconds()
	h.counter.Add(ctx, 1, metric.WithAttributes(attribute.String("status", status)))
	h.duration.Record(ctx, duration, metric.WithAttributes(attribute.String("status", status)))
}
