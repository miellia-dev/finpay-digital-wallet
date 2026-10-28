namespace FinPay.Domain.Common;

/// <summary>
/// Base class for domain entities with a Guid identifier.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
