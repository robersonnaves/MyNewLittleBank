package main

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
	"github.com/roberson/mynewlittlebank/notification-receiver/internal/handler"
	"github.com/roberson/mynewlittlebank/notification-receiver/internal/storage"
	"github.com/roberson/mynewlittlebank/notification-receiver/internal/telemetry"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
)

func main() {
	if err := run(); err != nil {
		slog.Error("application failed", "error", err)
		os.Exit(1)
	}
}

func run() error {
	// Configuration from environment
	port := getEnv("PORT", "8080")
	notificationsDir := getEnv("NOTIFICATIONS_DIR", "/data/notifications")
	otelEndpoint := getEnv("OTEL_EXPORTER_OTLP_ENDPOINT", "otel-collector:4317")
	serviceName := getEnv("OTEL_SERVICE_NAME", "notification-receiver")

	// Configure structured logging
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	}))
	slog.SetDefault(logger)

	slog.Info("service starting",
		"port", port,
		"notifications_dir", notificationsDir,
		"otel_endpoint", otelEndpoint,
		"service_name", serviceName,
	)

	ctx := context.Background()

	// Initialize OpenTelemetry
	shutdownTelemetry, err := telemetry.InitTelemetry(ctx, serviceName, otelEndpoint)
	if err != nil {
		return fmt.Errorf("failed to initialize telemetry: %w", err)
	}
	defer func() {
		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		if err := shutdownTelemetry(ctx); err != nil {
			slog.Error("failed to shutdown telemetry", "error", err)
		}
	}()

	tracer := telemetry.Tracer(serviceName)
	meter := telemetry.Meter(serviceName)

	// Initialize file storage
	fileWriter, err := storage.NewFileWriter(notificationsDir, tracer)
	if err != nil {
		return fmt.Errorf("failed to initialize file writer: %w", err)
	}

	// Initialize alerts handler
	alertsHandler, err := handler.NewAlertsHandler(fileWriter, tracer, meter)
	if err != nil {
		return fmt.Errorf("failed to initialize alerts handler: %w", err)
	}

	// Setup router
	r := chi.NewRouter()

	// Middlewares
	r.Use(middleware.RequestID)
	r.Use(middleware.RealIP)
	r.Use(middleware.Logger)
	r.Use(middleware.Recoverer)
	r.Use(middleware.Timeout(30 * time.Second))

	// Routes
	r.Route("/alerts", func(r chi.Router) {
		r.Post("/insufficient-funds", otelhttp.NewHandler(alertsHandler, "POST /alerts/insufficient-funds").ServeHTTP)
	})

	r.Get("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("OK"))
	})

	// HTTP server
	srv := &http.Server{
		Addr:         ":" + port,
		Handler:      r,
		ReadTimeout:  10 * time.Second,
		WriteTimeout: 10 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	// Start server in goroutine
	serverErrors := make(chan error, 1)
	go func() {
		slog.Info("server listening", "addr", srv.Addr)
		serverErrors <- srv.ListenAndServe()
	}()

	// Wait for interrupt signal or server error
	shutdown := make(chan os.Signal, 1)
	signal.Notify(shutdown, os.Interrupt, syscall.SIGTERM)

	select {
	case err := <-serverErrors:
		return fmt.Errorf("server error: %w", err)
	case sig := <-shutdown:
		slog.Info("shutting down", "signal", sig.String())

		// Graceful shutdown
		ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
		defer cancel()

		if err := srv.Shutdown(ctx); err != nil {
			srv.Close()
			return fmt.Errorf("graceful shutdown failed: %w", err)
		}

		if err := <-serverErrors; !errors.Is(err, http.ErrServerClosed) {
			return fmt.Errorf("server closed with error: %w", err)
		}

		slog.Info("shutdown complete")
	}

	return nil
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}
