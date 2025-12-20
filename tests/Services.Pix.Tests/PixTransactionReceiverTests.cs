using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Domain.Common;
using Domain.DTOs;
using Domain.Interfaces;
using Infra.Message;
using Services.Pix;
using UseCases.Transactions;

namespace Services.Pix.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscore-separated test naming improves readability of scenario/expectation.")]
public sealed class PixTransactionReceiverTests
{
    private readonly Mock<IProcessTransactionsHandler> _mockHandler;
    private readonly Mock<ILogger<PixTransactionReceiver>> _mockLogger;
    private readonly Mock<IOptions<RabbitOptions>> _mockOptions;
    private readonly RabbitOptions _rabbitOptions;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PixTransactionReceiverTests()
    {
        _mockHandler = new Mock<IProcessTransactionsHandler>();
        _mockLogger = new Mock<ILogger<PixTransactionReceiver>>();
        _mockOptions = new Mock<IOptions<RabbitOptions>>();
        
        _rabbitOptions = new RabbitOptions { RoutingKey = "pix.transactions" };
        _mockOptions.Setup(x => x.Value).Returns(_rabbitOptions);
        
        // Setup logger to enable all log levels for testing
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public void Constructor_WithNullOptions_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PixTransactionReceiver(null!, _mockLogger.Object, _mockHandler.Object));
        
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PixTransactionReceiver(_mockOptions.Object, null!, _mockHandler.Object));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullHandler_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, null!));
        
        exception.ParamName.Should().Be("handler");
    }

    [Fact]
    public void CanHandle_WithMatchingRoutingKey_Should_ReturnTrue()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.pix.transaction", "{}", "pix.transactions");

        // Act
        var result = receiver.CanHandle(envelope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WithDifferentRoutingKey_Should_ReturnFalse()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.pix.transaction", "{}", "money.transactions");

        // Act
        var result = receiver.CanHandle(envelope);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WithNullEnvelope_Should_ThrowArgumentNullException()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => receiver.CanHandle(null!));
        exception.ParamName.Should().Be("envelope");
    }

    [Fact]
    public async Task HandleAsync_WithValidPixTransaction_Should_ProcessSuccessfully()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var pixDto = new PixTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            100.00m,
            "origin@bank.com",
            "destination@bank.com",
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(pixDto, JsonOptions);
        var envelope = new MessageEnvelope("mock.pix.transaction", payload, "pix.transactions");

        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.Is<PixTransactionDto>(dto => 
                dto.TransactionId == pixDto.TransactionId &&
                dto.ClientId == pixDto.ClientId &&
                dto.AccountNumber == pixDto.AccountNumber &&
                dto.Amount == pixDto.Amount &&
                dto.OriginPixKey == pixDto.OriginPixKey &&
                dto.DestinationPixKey == pixDto.DestinationPixKey &&
                dto.Status == pixDto.Status), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_Should_ThrowJsonException()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.pix.transaction", "invalid-json", "pix.transactions");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => receiver.HandleAsync(envelope, CancellationToken.None));

        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNullDeserializationResult_Should_LogDeserializationError_And_Return()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.pix.transaction", "null", "pix.transactions");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        VerifyLogMessage(LogLevel.Warning, 4); // DeserializationFailed EventId
    }

    [Fact]
    public async Task HandleAsync_WithWrongRoutingKey_Should_LogIgnoredRoutingKey_And_Return()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.pix.transaction", "{}", "wrong.routing.key");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        VerifyLogMessage(LogLevel.Warning, 2); // IgnoredRoutingKey EventId
    }

    [Fact]
    public async Task HandleAsync_WithNonTransactionMessageType_Should_LogIgnoredMessageType_And_Return()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "{}", "pix.transactions");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        VerifyLogMessage(LogLevel.Debug, 3); // IgnoredMessageType EventId
    }

    [Theory]
    [InlineData("insufficient_funds")]
    [InlineData("bank_account_not_found")]
    [InlineData("client_not_found")]
    [InlineData("invalid_cpf")]
    [InlineData("invalid_account_number")]
    [InlineData("invalid_money")]
    [InlineData("invalid_transaction")]
    public async Task HandleAsync_WithBusinessError_Should_LogProcessingError_And_AcknowledgeMessage(string errorMessage)
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var pixDto = new PixTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            100.00m,
            "origin@bank.com",
            "destination@bank.com",
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(pixDto, JsonOptions);
        var envelope = new MessageEnvelope("mock.pix.transaction", payload, "pix.transactions");

        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        // Act - Should complete without throwing
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        VerifyLogMessage(LogLevel.Warning, 1); // BusinessErrorOccurred EventId
    }

    [Fact]
    public async Task HandleAsync_WithTechnicalError_Should_LogProcessingError_And_ThrowException()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var pixDto = new PixTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            100.00m,
            "origin@bank.com",
            "destination@bank.com",
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(pixDto, JsonOptions);
        var envelope = new MessageEnvelope("mock.pix.transaction", payload, "pix.transactions");

        const string errorMessage = "database_connection_error";
        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => receiver.HandleAsync(envelope, CancellationToken.None));
        
        exception.Message.Should().Be(errorMessage);
        
        VerifyLogMessage(LogLevel.Error, 5); // TechnicalErrorOccurred EventId
        
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullEnvelope_Should_ThrowArgumentNullException()
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            receiver.HandleAsync(null!, CancellationToken.None));
        
        exception.ParamName.Should().Be("envelope");
    }

    [Theory]
    [InlineData("mock.card.transaction")]
    [InlineData("mock.pix.transaction")]
    [InlineData("mock.money.transaction")]
    [InlineData("Mock.PIX.Transaction")]
    public async Task HandleAsync_WithTransactionMessageTypes_Should_ProcessMessage(string messageType)
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var pixDto = new PixTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            100.00m,
            "origin@bank.com",
            "destination@bank.com",
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(pixDto, JsonOptions);
        var envelope = new MessageEnvelope(messageType, payload, "pix.transactions");

        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData("heartbeat.ping")]
    [InlineData("system.health")]
    [InlineData("notification.sent")]
    [InlineData("")]
    public async Task HandleAsync_WithNonTransactionMessageTypes_Should_IgnoreMessage(string messageType)
    {
        // Arrange
        var receiver = new PixTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope(messageType, "{}", "pix.transactions");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<PixTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
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