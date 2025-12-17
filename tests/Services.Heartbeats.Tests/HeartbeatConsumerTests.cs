using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Infra.Message;
using Services.Heartbeats;

namespace Services.Heartbeats.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscore-separated test naming improves readability of scenario/expectation.")]
public sealed class HeartbeatConsumerTests
{
    private readonly Mock<ILogger<HeartbeatConsumer>> _mockLogger;
    private readonly Mock<IOptions<RabbitOptions>> _mockOptions;
    private readonly RabbitOptions _rabbitOptions;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HeartbeatConsumerTests()
    {
        _mockLogger = new Mock<ILogger<HeartbeatConsumer>>();
        _mockOptions = new Mock<IOptions<RabbitOptions>>();
        
        _rabbitOptions = new RabbitOptions { RoutingKey = "heartbeat.ping" };
        _mockOptions.Setup(x => x.Value).Returns(_rabbitOptions);
        
        // Setup logger to enable all log levels for testing
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public void Constructor_WithNullOptions_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new HeartbeatConsumer(null!, _mockLogger.Object));
        
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new HeartbeatConsumer(_mockOptions.Object, null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void CanHandle_WithMatchingRoutingKey_Should_ReturnTrue()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "{}", "heartbeat.ping");

        // Act
        var result = consumer.CanHandle(envelope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WithDifferentRoutingKey_Should_ReturnFalse()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "{}", "different.routing.key");

        // Act
        var result = consumer.CanHandle(envelope);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WithNullEnvelope_Should_ThrowArgumentNullException()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => consumer.CanHandle(null!));
        exception.ParamName.Should().Be("envelope");
    }

    [Fact]
    public async Task HandleAsync_WithValidHeartbeat_Should_LogHeartbeatReceived()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var heartbeatDto = new HeartbeatDto("TestService", "healthy", DateTime.UtcNow);
        var payload = JsonSerializer.Serialize(heartbeatDto, JsonOptions);
        var envelope = new MessageEnvelope("heartbeat.ping", payload, "heartbeat.ping");

        // Act
        await consumer.HandleAsync(envelope, CancellationToken.None);

        // Assert - Verify structured logging with EventId 1
        VerifyLogMessage(LogLevel.Information, 1); // HeartbeatReceived EventId
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_Should_ThrowJsonException()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "invalid-json", "heartbeat.ping");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => consumer.HandleAsync(envelope, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithNullDeserializationResult_Should_LogWarning_And_Return()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "null", "heartbeat.ping");

        // Act
        await consumer.HandleAsync(envelope, CancellationToken.None);

        // Assert - Verify LogWarning was called for deserialization failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("could not be deserialized")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithWrongRoutingKey_Should_LogDebug_And_Return()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "{}", "wrong.routing.key");

        // Act
        await consumer.HandleAsync(envelope, CancellationToken.None);

        // Assert - Verify LogDebug was called for routing key mismatch
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ignoring heartbeat message")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullEnvelope_Should_ThrowArgumentNullException()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            consumer.HandleAsync(null!, CancellationToken.None));
        
        exception.ParamName.Should().Be("envelope");
    }

    [Theory]
    [InlineData("APIService", "healthy")]
    [InlineData("DatabaseService", "degraded")]
    [InlineData("CacheService", "unhealthy")]
    [InlineData("", "")]
    public async Task HandleAsync_WithDifferentServiceStatuses_Should_LogHeartbeatReceived(string serviceName, string status)
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var heartbeatDto = new HeartbeatDto(serviceName, status, DateTime.UtcNow);
        var payload = JsonSerializer.Serialize(heartbeatDto, JsonOptions);
        var envelope = new MessageEnvelope("heartbeat.ping", payload, "heartbeat.ping");

        // Act
        await consumer.HandleAsync(envelope, CancellationToken.None);

        // Assert - Verify structured logging with EventId 1 was called
        VerifyLogMessage(LogLevel.Information, 1); // HeartbeatReceived EventId
    }

    [Theory]
    [InlineData("heartbeat.ping")]
    [InlineData("health.check")]
    [InlineData("service.status")]
    public async Task HandleAsync_WithDifferentMessageTypes_Should_ProcessWhenRoutingKeyMatches(string messageType)
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        var heartbeatDto = new HeartbeatDto("TestService", "healthy", DateTime.UtcNow);
        var payload = JsonSerializer.Serialize(heartbeatDto, JsonOptions);
        var envelope = new MessageEnvelope(messageType, payload, "heartbeat.ping");

        // Act
        await consumer.HandleAsync(envelope, CancellationToken.None);

        // Assert - Verify structured logging with EventId 1 was called
        VerifyLogMessage(LogLevel.Information, 1); // HeartbeatReceived EventId
    }

    [Fact]
    public async Task HandleAsync_AllScenarios_Should_NeverThrowExceptions_Except_ArgumentNull_And_JsonException()
    {
        // Arrange
        var consumer = new HeartbeatConsumer(_mockOptions.Object, _mockLogger.Object);
        
        // Test various scenarios that should not throw
        var scenarios = new[]
        {
            new MessageEnvelope("heartbeat.ping", "null", "heartbeat.ping"), // null deserialization
            new MessageEnvelope("heartbeat.ping", "{}", "wrong.routing.key"), // wrong routing key
            new MessageEnvelope("any.message", "{}", "heartbeat.ping"), // any message type with correct routing
        };

        foreach (var envelope in scenarios)
        {
            // Act & Assert - Should complete without exceptions
            await consumer.HandleAsync(envelope, CancellationToken.None);
        }
        
        // All scenarios should have completed successfully without exceptions
        Assert.True(true); // If we reach here, no exceptions were thrown
    }

    private void VerifyLogMessage(LogLevel logLevel, int eventId)
    {
        _mockLogger.Verify(
            x => x.Log(
                logLevel,
                It.Is<EventId>(e => e.Id == eventId),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}