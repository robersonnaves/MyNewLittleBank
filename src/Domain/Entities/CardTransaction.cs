namespace Domain.Entities;

public class CardTransaction(string number) : Transaction
{
    public string Number { get; set; } = number;
}
