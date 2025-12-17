using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Domain.Common;
using Domain.DTOs;
using Domain.Interfaces;
using Infra.Message;
using Services.Money;
using UseCases.Transactions;

namespace Services.Money.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Underscore-separated test naming improves readability of scenario/expectation.")]
public sealed class MoneyTransactionReceiverTests
{
    private readonly Mock<IProcessTransactionsHandler> _mockHandler;
    private readonly Mock<ILogger<MoneyTransactionReceiver>> _mockLogger;
    private readonly Mock<IOptions<RabbitOptions>> _mockOptions;
    private readonly RabbitOptions _rabbitOptions;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public MoneyTransactionReceiverTests()
    {
        _mockHandler = new Mock<IProcessTransactionsHandler>();
        _mockLogger = new Mock<ILogger<MoneyTransactionReceiver>>();
        _mockOptions = new Mock<IOptions<RabbitOptions>>();
        
        _rabbitOptions = new RabbitOptions { RoutingKey = "money.transactions" };
        _mockOptions.Setup(x => x.Value).Returns(_rabbitOptions);
        
        // Setup logger to enable all log levels for testing
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public void Constructor_WithNullOptions_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new MoneyTransactionReceiver(null!, _mockLogger.Object, _mockHandler.Object));
        
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new MoneyTransactionReceiver(_mockOptions.Object, null!, _mockHandler.Object));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullHandler_Should_ThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, null!));
        
        exception.ParamName.Should().Be("handler");
    }

    [Fact]
    public void CanHandle_WithMatchingRoutingKey_Should_ReturnTrue()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.money.transaction", "{}", "money.transactions");

        // Act
        var result = receiver.CanHandle(envelope);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_WithDifferentRoutingKey_Should_ReturnFalse()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.money.transaction", "{}", "card.transactions");

        // Act
        var result = receiver.CanHandle(envelope);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanHandle_WithNullEnvelope_Should_ThrowArgumentNullException()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => receiver.CanHandle(null!));
        exception.ParamName.Should().Be("envelope");
    }

    [Fact]
    public async Task HandleAsync_WithValidMoneyTransaction_Should_ProcessSuccessfully()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var moneyDto = new MoneyTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            250.75m,
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(moneyDto, JsonOptions);
        var envelope = new MessageEnvelope("mock.money.transaction", payload, "money.transactions");

        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.Is<MoneyTransactionDto>(dto => 
                dto.TransactionId == moneyDto.TransactionId &&
                dto.ClientId == moneyDto.ClientId &&
                dto.AccountNumber == moneyDto.AccountNumber &&
                dto.Amount == moneyDto.Amount &&
                dto.Status == moneyDto.Status), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_Should_ThrowJsonException()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.money.transaction", "invalid-json", "money.transactions");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => receiver.HandleAsync(envelope, CancellationToken.None));

        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNullDeserializationResult_Should_LogDeserializationError_And_Return()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.money.transaction", "null", "money.transactions");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        VerifyLogMessage(LogLevel.Warning, 4); // DeserializationFailed EventId
    }

    [Fact]
    public async Task HandleAsync_WithWrongRoutingKey_Should_LogIgnoredRoutingKey_And_Return()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("mock.money.transaction", "{}", "wrong.routing.key");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        VerifyLogMessage(LogLevel.Warning, 2); // IgnoredRoutingKey EventId
    }

    [Fact]
    public async Task HandleAsync_WithNonTransactionMessageType_Should_LogIgnoredMessageType_And_Return()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope("heartbeat.ping", "{}", "money.transactions");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        VerifyLogMessage(LogLevel.Debug, 3); // IgnoredMessageType EventId
    }

    [Fact]
    public async Task HandleAsync_WithHandlerFailure_Should_LogProcessingError_And_ThrowException()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var moneyDto = new MoneyTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            250.75m,
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(moneyDto, JsonOptions);
        var envelope = new MessageEnvelope("mock.money.transaction", payload, "money.transactions");

        const string errorMessage = "insufficient_funds";
        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(errorMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => receiver.HandleAsync(envelope, CancellationToken.None));
        
        exception.Message.Should().Be(errorMessage);
        
        VerifyLogMessage(LogLevel.Warning, 1); // ProcessingFailed EventId
        
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullEnvelope_Should_ThrowArgumentNullException()
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            receiver.HandleAsync(null!, CancellationToken.None));
        
        exception.ParamName.Should().Be("envelope");
    }

    [Theory]
    [InlineData("mock.card.transaction")]
    [InlineData("mock.pix.transaction")]
    [InlineData("mock.money.transaction")]
    [InlineData("Mock.MONEY.Transaction")]
    public async Task HandleAsync_WithTransactionMessageTypes_Should_ProcessMessage(string messageType)
    {
        // Arrange
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var moneyDto = new MoneyTransactionDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "123456",
            250.75m,
            TransactionStatus.Pending,
            DateTime.UtcNow);

        var payload = JsonSerializer.Serialize(moneyDto, JsonOptions);
        var envelope = new MessageEnvelope(messageType, payload, "money.transactions");

        _mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
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
        var receiver = new MoneyTransactionReceiver(_mockOptions.Object, _mockLogger.Object, _mockHandler.Object);
        var envelope = new MessageEnvelope(messageType, "{}", "money.transactions");

        // Act
        await receiver.HandleAsync(envelope, CancellationToken.None);

        // Assert
        _mockHandler.Verify(
            h => h.HandleAsync(It.IsAny<MoneyTransactionDto>(), It.IsAny<CancellationToken>()), 
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