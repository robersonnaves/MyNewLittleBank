using Domain.Entities;

namespace Domain.Interfaces;

public interface ITransaction
{
    void SetClient(Client client);
    void SetCreatedAt(DateTime createdAt);
    void SetAmount(int amount);
    void SetMovementType(MovementType movementType);
    Transaction Cancel();
    decimal GetDecimalAmount();
}