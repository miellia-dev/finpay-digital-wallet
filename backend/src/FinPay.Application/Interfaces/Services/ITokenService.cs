using FinPay.Domain.Entities;

namespace FinPay.Application.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user, string walletNumber);
}
