using Domain.Messaging;

namespace Infra.Message.Serialization;

/// <summary>
/// Registro central de tipos de mensagens conhecidos para resolução de tipos usando FullName.
/// Todos os tipos de mensagens devem ser registrados aqui antes de serem usados.
/// </summary>
public static class MessageTypeRegistry
{
    private static readonly Dictionary<string, Type> KnownTypes = new()
    {
        ["Domain.Messaging.MessageEnvelope"] = typeof(MessageEnvelope),
        // FUTURO: Adicione novos tipos aqui quando necessário
        // Exemplo:
        // ["Domain.Events.OrderCreated"] = typeof(OrderCreated),
        // ["Domain.Commands.ProcessPayment"] = typeof(ProcessPayment),
    };

    /// <summary>
    /// Resolve um tipo pelo seu FullName.
    /// FAIL FAST: Lança InvalidOperationException se o tipo não estiver registrado.
    /// </summary>
    /// <param name="typeName">Nome completo do tipo (FullName)</param>
    /// <returns>Tipo resolvido</returns>
    /// <exception cref="ArgumentNullException">Se typeName for null ou vazio</exception>
    /// <exception cref="InvalidOperationException">Se o tipo não estiver registrado</exception>
    public static Type ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentNullException(nameof(typeName), "Type name cannot be null or empty");

        // Normalizar: remover informações de assembly se presentes (compatibilidade reversa)
        var normalizedTypeName = typeName.Contains(',') 
            ? typeName.Split(',')[0].Trim() 
            : typeName;

        // FAIL FAST: Tipo deve estar registrado
        if (KnownTypes.TryGetValue(normalizedTypeName, out var knownType))
            return knownType;

        // Tipo não encontrado - fail fast
        var registeredTypes = string.Join(", ", KnownTypes.Keys);
        throw new InvalidOperationException(
            $"Type '{typeName}' is not registered in MessageTypeRegistry. " +
            $"Registered types: [{registeredTypes}]. " +
            $"Add the type to MessageTypeRegistry.KnownTypes before using it.");
    }

    /// <summary>
    /// Obtém o nome do tipo a ser usado nos headers (FullName).
    /// </summary>
    /// <param name="type">Tipo a ser serializado</param>
    /// <returns>Nome do tipo (FullName)</returns>
    public static string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.FullName ?? type.Name;
    }

    /// <summary>
    /// Verifica se um tipo está registrado.
    /// </summary>
    public static bool IsKnownType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var typeName = GetTypeName(type);
        return KnownTypes.ContainsKey(typeName);
    }

    /// <summary>
    /// Verifica se um tipo está registrado pelo nome.
    /// </summary>
    public static bool IsKnownType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        var normalizedTypeName = typeName.Contains(',') 
            ? typeName.Split(',')[0].Trim() 
            : typeName;

        return KnownTypes.ContainsKey(normalizedTypeName);
    }
}
