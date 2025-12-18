package storage

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"
	"os"
	"path/filepath"

	"go.opentelemetry.io/otel/trace"
)

var (
	// ErrAlreadyExists indicates the notification file already exists (idempotent case)
	ErrAlreadyExists = errors.New("notification already exists")
)

// FileWriter handles writing notification JSON files to disk with idempotency.
type FileWriter struct {
	baseDir string
	tracer  trace.Tracer
}

// NewFileWriter creates a new FileWriter for the given base directory.
// It creates the directory if it doesn't exist.
func NewFileWriter(baseDir string, tracer trace.Tracer) (*FileWriter, error) {
	if err := os.MkdirAll(baseDir, 0755); err != nil {
		return nil, fmt.Errorf("failed to create notifications directory: %w", err)
	}
	slog.Info("file writer initialized", "base_dir", baseDir)
	return &FileWriter{
		baseDir: baseDir,
		tracer:  tracer,
	}, nil
}

// Save persists a notification to disk as {transactionId}.json.
// If the file already exists, returns ErrAlreadyExists without writing (idempotent).
func (fw *FileWriter) Save(ctx context.Context, transactionID string, data interface{}) error {
	ctx, span := fw.tracer.Start(ctx, "storage.save")
	defer span.End()

	filePath := filepath.Join(fw.baseDir, fmt.Sprintf("%s.json", transactionID))

	// Check if file already exists (idempotency check)
	if _, err := os.Stat(filePath); err == nil {
		slog.InfoContext(ctx, "notification already exists, skipping write", "transaction_id", transactionID)
		return ErrAlreadyExists
	} else if !os.IsNotExist(err) {
		return fmt.Errorf("failed to check file existence: %w", err)
	}

	// Marshal to pretty JSON
	jsonData, err := json.MarshalIndent(data, "", "  ")
	if err != nil {
		return fmt.Errorf("failed to marshal notification: %w", err)
	}

	// Write to file atomically (write to temp, then rename)
	tempPath := filePath + ".tmp"
	if err := os.WriteFile(tempPath, jsonData, 0644); err != nil {
		return fmt.Errorf("failed to write temp file: %w", err)
	}

	if err := os.Rename(tempPath, filePath); err != nil {
		os.Remove(tempPath) // Clean up temp file
		return fmt.Errorf("failed to rename temp file: %w", err)
	}

	slog.InfoContext(ctx, "notification saved", "transaction_id", transactionID, "path", filePath)
	return nil
}
