using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rebus.Messages;
using Rebus.Serialization;
using RebusMessage = Rebus.Messages.Message;

namespace Infra.Message.Serialization;

/// <summary>
/// Serializer customizado para Rebus que usa FullName (sem assembly info) nos headers.
/// Resolve tipos através do MessageTypeRegistry com comportamento fail-fast.
/// </summary>
public sealed class SimpleTypeNameSerializer : ISerializer
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    
    private readonly ILogger<SimpleTypeNameSerializer> _logger;

    public SimpleTypeNameSerializer(ILogger<SimpleTypeNameSerializer> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<TransportMessage> Serialize(RebusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var headers = new Dictionary<string, string>(message.Headers);
            var body = message.Body;

            if (body == null)
            {
                var errorMsg = "Cannot serialize message with null body";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            var bodyType = body.GetType();

            // FAIL FAST: Verificar se tipo está registrado antes de serializar
            if (!MessageTypeRegistry.IsKnownType(bodyType))
            {
                var errorMsg = $"Type '{bodyType.FullName}' is not registered in MessageTypeRegistry. " +
                             $"Cannot serialize unregistered type.";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Serializar o body para JSON
            var jsonString = JsonSerializer.Serialize(body, bodyType, JsonOptions);
            var bytes = DefaultEncoding.GetBytes(jsonString);

            // Garantir que o header Type está usando FullName
            if (!headers.ContainsKey(Headers.Type))
            {
                headers[Headers.Type] = MessageTypeRegistry.GetTypeName(bodyType);
            }

            _logger.LogDebug("Serialized message of type {MessageType}, size {Size} bytes", 
                bodyType.FullName, bytes.Length);

            var transportMessage = new TransportMessage(headers, bytes);
            return Task.FromResult(transportMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize message");
            throw;
        }
    }

    public Task<RebusMessage> Deserialize(TransportMessage transportMessage)
    {
        ArgumentNullException.ThrowIfNull(transportMessage);

        string? typeHeader = null;
        try
        {
            var headers = transportMessage.Headers;
            var bodyBytes = transportMessage.Body;

            if (bodyBytes == null || bodyBytes.Length == 0)
            {
                var errorMsg = "Cannot deserialize message with empty body";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Extrair o tipo do header
            if (!headers.TryGetValue(Headers.Type, out typeHeader) || string.IsNullOrWhiteSpace(typeHeader))
            {
                var errorMsg = $"Message header '{Headers.Type}' is missing or empty";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // FAIL FAST: Resolver o tipo usando o registry
            // MessageTypeRegistry.ResolveType já faz fail fast
            var bodyType = MessageTypeRegistry.ResolveType(typeHeader);

            // Desserializar o JSON
            var jsonString = DefaultEncoding.GetString(bodyBytes);
            var body = JsonSerializer.Deserialize(jsonString, bodyType, JsonOptions);

            if (body == null)
            {
                var errorMsg = $"Deserialized body is null for type: {typeHeader}";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            _logger.LogDebug("Deserialized message of type {MessageType}, size {Size} bytes", 
                bodyType.FullName, bodyBytes.Length);

            return Task.FromResult(new RebusMessage(headers, body));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not registered"))
        {
            // Erro específico de tipo não registrado - propagar com contexto
            _logger.LogError(ex, "Type resolution failed for: {TypeHeader}", typeHeader ?? "MISSING");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize message. Type: {TypeHeader}", 
                typeHeader ?? "MISSING");
            throw;
        }
    }
}
