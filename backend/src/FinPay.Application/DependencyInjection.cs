using FinPay.Application.Interfaces.Services;
using FinPay.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FinPay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
