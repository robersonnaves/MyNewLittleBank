package storage_test

import (
	"context"
	"encoding/json"
	"os"
	"path/filepath"
	"testing"

	"github.com/roberson/mynewlittlebank/notification-receiver/internal/storage"
	"go.opentelemetry.io/otel/trace/noop"
)

type testNotification struct {
	TransactionID string  `json:"transactionId"`
	Amount        float64 `json:"amount"`
}

func TestFileWriter_Save_Success(t *testing.T) {
	// Create temp directory
	tempDir := t.TempDir()

	// Create file writer
	fw, err := storage.NewFileWriter(tempDir, noop.NewTracerProvider().Tracer("test"))
	if err != nil {
		t.Fatalf("failed to create file writer: %v", err)
	}

	// Save notification
	notification := testNotification{
		TransactionID: "550e8400-e29b-41d4-a716-446655440000",
		Amount:        1500.50,
	}

	err = fw.Save(context.Background(), notification.TransactionID, notification)
	if err != nil {
		t.Fatalf("expected no error, got: %v", err)
	}

	// Verify file was created
	filePath := filepath.Join(tempDir, notification.TransactionID+".json")
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		t.Fatal("file was not created")
	}

	// Verify file contents
	data, err := os.ReadFile(filePath)
	if err != nil {
		t.Fatalf("failed to read file: %v", err)
	}

	var saved testNotification
	if err := json.Unmarshal(data, &saved); err != nil {
		t.Fatalf("failed to unmarshal JSON: %v", err)
	}

	if saved.TransactionID != notification.TransactionID {
		t.Errorf("expected transaction ID %s, got %s", notification.TransactionID, saved.TransactionID)
	}
	if saved.Amount != notification.Amount {
		t.Errorf("expected amount %.2f, got %.2f", notification.Amount, saved.Amount)
	}
}

func TestFileWriter_Save_Duplicate(t *testing.T) {
	tempDir := t.TempDir()

	fw, err := storage.NewFileWriter(tempDir, noop.NewTracerProvider().Tracer("test"))
	if err != nil {
		t.Fatalf("failed to create file writer: %v", err)
	}

	notification := testNotification{
		TransactionID: "550e8400-e29b-41d4-a716-446655440000",
		Amount:        1500.50,
	}

	// Save first time
	err = fw.Save(context.Background(), notification.TransactionID, notification)
	if err != nil {
		t.Fatalf("first save failed: %v", err)
	}

	// Get file modification time
	filePath := filepath.Join(tempDir, notification.TransactionID+".json")
	info1, err := os.Stat(filePath)
	if err != nil {
		t.Fatalf("failed to stat file: %v", err)
	}

	// Try to save again (duplicate)
	notification.Amount = 2000.00 // Different data
	err = fw.Save(context.Background(), notification.TransactionID, notification)
	if err != storage.ErrAlreadyExists {
		t.Fatalf("expected ErrAlreadyExists, got: %v", err)
	}

	// Verify file was NOT modified
	info2, err := os.Stat(filePath)
	if err != nil {
		t.Fatalf("failed to stat file: %v", err)
	}

	if !info1.ModTime().Equal(info2.ModTime()) {
		t.Error("file was modified on duplicate save")
	}

	// Verify original data is still in file
	data, _ := os.ReadFile(filePath)
	var saved testNotification
	json.Unmarshal(data, &saved)
	if saved.Amount != 1500.50 {
		t.Errorf("expected original amount 1500.50, got %.2f", saved.Amount)
	}
}

func TestFileWriter_Save_DirectoryCreated(t *testing.T) {
	tempDir := t.TempDir()
	nestedDir := filepath.Join(tempDir, "nested", "path")

	// Directory doesn't exist yet
	if _, err := os.Stat(nestedDir); !os.IsNotExist(err) {
		t.Fatal("directory should not exist yet")
	}

	// Create file writer (should create directory)
	fw, err := storage.NewFileWriter(nestedDir, noop.NewTracerProvider().Tracer("test"))
	if err != nil {
		t.Fatalf("failed to create file writer: %v", err)
	}

	// Verify directory was created
	if _, err := os.Stat(nestedDir); os.IsNotExist(err) {
		t.Fatal("directory was not created")
	}

	// Should be able to save
	notification := testNotification{
		TransactionID: "test-123",
		Amount:        100.00,
	}
	err = fw.Save(context.Background(), notification.TransactionID, notification)
	if err != nil {
		t.Fatalf("save failed: %v", err)
	}
}

func TestFileWriter_Save_InvalidPath(t *testing.T) {
	// Try to create file writer in invalid location
	_, err := storage.NewFileWriter("/invalid/path/that/does/not/exist", noop.NewTracerProvider().Tracer("test"))
	if err == nil {
		t.Fatal("expected error for invalid path, got nil")
	}
}
