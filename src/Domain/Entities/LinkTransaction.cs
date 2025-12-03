namespace Domain.Entities;

public class LinkTransaction(Uri uri) : Transaction
{
    public Uri Uri { get; init; } = uri;
}
