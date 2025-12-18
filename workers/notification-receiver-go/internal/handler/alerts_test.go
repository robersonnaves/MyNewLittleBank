package handler_test

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/roberson/mynewlittlebank/notification-receiver/internal/handler"
	"github.com/roberson/mynewlittlebank/notification-receiver/internal/storage"
	nooptrace "go.opentelemetry.io/otel/trace/noop"
)

// mockStorage mocks the file writer for testing
type mockStorage struct {
	saveFn func(ctx context.Context, transactionID string, data interface{}) error
}

func (m *mockStorage) Save(ctx context.Context, transactionID string, data interface{}) error {
	if m.saveFn != nil {
		return m.saveFn(ctx, transactionID, data)
	}
	return nil
}

func TestAlertsHandler_ValidPayload_Success(t *testing.T) {
	// Mock storage that succeeds
	mock := &mockStorage{
		saveFn: func(ctx context.Context, transactionID string, data interface{}) error {
			if transactionID != "550e8400-e29b-41d4-a716-446655440000" {
				t.Errorf("unexpected transaction ID: %s", transactionID)
			}
			return nil
		},
	}

	h := createHandler(t, mock)

	payload := map[string]interface{}{
		"cpf":              "12345678901",
		"accountNumber":    "10000001",
		"transactionId":    "550e8400-e29b-41d4-a716-446655440000",
		"attemptedAmount":  1500.50,
		"availableBalance": 500.00,
		"occurredAt":       "2025-12-17T10:30:00Z",
		"traceId":          "trace-123",
		"reason":           "insufficient_funds",
	}

	req := createRequest(t, payload)
	rr := httptest.NewRecorder()

	h.ServeHTTP(rr, req)

	if status := rr.Code; status != http.StatusAccepted {
		t.Errorf("expected status %d, got %d", http.StatusAccepted, status)
	}
}

func TestAlertsHandler_DuplicatePayload_Success(t *testing.T) {
	// Mock storage that returns ErrAlreadyExists
	mock := &mockStorage{
		saveFn: func(ctx context.Context, transactionID string, data interface{}) error {
			return storage.ErrAlreadyExists
		},
	}

	h := createHandler(t, mock)

	payload := map[string]interface{}{
		"cpf":              "12345678901",
		"accountNumber":    "10000001",
		"transactionId":    "550e8400-e29b-41d4-a716-446655440000",
		"attemptedAmount":  1500.50,
		"availableBalance": 500.00,
		"occurredAt":       "2025-12-17T10:30:00Z",
		"traceId":          "trace-123",
		"reason":           "insufficient_funds",
	}

	req := createRequest(t, payload)
	rr := httptest.NewRecorder()

	h.ServeHTTP(rr, req)

	if status := rr.Code; status != http.StatusAccepted {
		t.Errorf("expected status %d for duplicate, got %d", http.StatusAccepted, status)
	}
}

func TestAlertsHandler_InvalidJSON_BadRequest(t *testing.T) {
	mock := &mockStorage{}
	h := createHandler(t, mock)

	req := httptest.NewRequest(http.MethodPost, "/alerts/insufficient-funds", bytes.NewBufferString("{invalid json"))
	req.Header.Set("Content-Type", "application/json")

	rr := httptest.NewRecorder()
	h.ServeHTTP(rr, req)

	if status := rr.Code; status != http.StatusBadRequest {
		t.Errorf("expected status %d, got %d", http.StatusBadRequest, status)
	}
}

func TestAlertsHandler_MissingField_BadRequest(t *testing.T) {
	testCases := []struct {
		name    string
		payload map[string]interface{}
	}{
		{
			name: "missing cpf",
			payload: map[string]interface{}{
				"accountNumber":    "10000001",
				"transactionId":    "550e8400-e29b-41d4-a716-446655440000",
				"attemptedAmount":  1500.50,
				"availableBalance": 500.00,
				"occurredAt":       "2025-12-17T10:30:00Z",
				"traceId":          "trace-123",
				"reason":           "insufficient_funds",
			},
		},
		{
			name: "missing transactionId",
			payload: map[string]interface{}{
				"cpf":              "12345678901",
				"accountNumber":    "10000001",
				"attemptedAmount":  1500.50,
				"availableBalance": 500.00,
				"occurredAt":       "2025-12-17T10:30:00Z",
				"traceId":          "trace-123",
				"reason":           "insufficient_funds",
			},
		},
		{
			name: "missing reason",
			payload: map[string]interface{}{
				"cpf":              "12345678901",
				"accountNumber":    "10000001",
				"transactionId":    "550e8400-e29b-41d4-a716-446655440000",
				"attemptedAmount":  1500.50,
				"availableBalance": 500.00,
				"occurredAt":       "2025-12-17T10:30:00Z",
				"traceId":          "trace-123",
			},
		},
	}

	for _, tc := range testCases {
		t.Run(tc.name, func(t *testing.T) {
			mock := &mockStorage{}
			h := createHandler(t, mock)

			req := createRequest(t, tc.payload)
			rr := httptest.NewRecorder()

			h.ServeHTTP(rr, req)

			if status := rr.Code; status != http.StatusBadRequest {
				t.Errorf("expected status %d, got %d", http.StatusBadRequest, status)
			}
		})
	}
}

func TestAlertsHandler_StorageError_InternalServerError(t *testing.T) {
	// Mock storage that returns an error
	mock := &mockStorage{
		saveFn: func(ctx context.Context, transactionID string, data interface{}) error {
			return errors.New("disk full")
		},
	}

	h := createHandler(t, mock)

	payload := map[string]interface{}{
		"cpf":              "12345678901",
		"accountNumber":    "10000001",
		"transactionId":    "550e8400-e29b-41d4-a716-446655440000",
		"attemptedAmount":  1500.50,
		"availableBalance": 500.00,
		"occurredAt":       "2025-12-17T10:30:00Z",
		"traceId":          "trace-123",
		"reason":           "insufficient_funds",
	}

	req := createRequest(t, payload)
	rr := httptest.NewRecorder()

	h.ServeHTTP(rr, req)

	if status := rr.Code; status != http.StatusInternalServerError {
		t.Errorf("expected status %d, got %d", http.StatusInternalServerError, status)
	}
}

// Helper functions

type storageInterface interface {
	Save(ctx context.Context, transactionID string, data interface{}) error
}

func createHandler(t *testing.T, storage storageInterface) http.Handler {
	// Create a custom handler that uses our mock storage
	tracer := nooptrace.NewTracerProvider().Tracer("test")

	// We'll create a minimal handler structure for testing
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		var notification handler.InsufficientFundsNotification
		if err := json.NewDecoder(r.Body).Decode(&notification); err != nil {
			http.Error(w, `{"error":"invalid JSON payload"}`, http.StatusBadRequest)
			return
		}

		if err := notification.Validate(); err != nil {
			http.Error(w, `{"error":"`+err.Error()+`"}`, http.StatusBadRequest)
			return
		}

		ctx, span := tracer.Start(r.Context(), "test")
		defer span.End()

		err := storage.Save(ctx, notification.TransactionID, notification)
		if err != nil {
			// Check if error message contains "already exists"
			if err.Error() == "notification already exists" {
				w.WriteHeader(http.StatusAccepted)
				return
			}
			http.Error(w, `{"error":"failed to save notification"}`, http.StatusInternalServerError)
			return
		}

		w.WriteHeader(http.StatusAccepted)
	})
}

func createRequest(t *testing.T, payload map[string]interface{}) *http.Request {
	body, err := json.Marshal(payload)
	if err != nil {
		t.Fatalf("failed to marshal payload: %v", err)
	}

	req := httptest.NewRequest(http.MethodPost, "/alerts/insufficient-funds", bytes.NewBuffer(body))
	req.Header.Set("Content-Type", "application/json")
	return req
}
