#pragma warning disable CA1716 // Identifiers should not match keywords
namespace Shared.Observability;

public sealed class ActivitySourceOptions
{
    public string Name { get; init; } = "mynewlittlebank";
}
