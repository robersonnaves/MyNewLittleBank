namespace Domain.Entities;

public abstract class PersistenceBaseClass
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public bool Active { get; set; } = true;

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}