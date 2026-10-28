using FinPay.Domain.Enums;

namespace FinPay.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    Guid GetRequiredUserId();
}
