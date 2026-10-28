using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinPay.Application.Common;
using FinPay.Application.DTOs.Auth;
using FinPay.Application.DTOs.Transaction;
using FinPay.Application.DTOs.Transfer;
using FinPay.Application.DTOs.Wallet;
using FinPay.Domain.Enums;
using Xunit;

namespace FinPay.Tests.Integration;

public class FinancialIntegrityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FinancialIntegrityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AuthResponseDto> RegisterUserAsync(string name, string email, string phone, UserRole role = UserRole.Customer)
    {
        var registerDto = new RegisterRequestDto(name, email, phone, "SecurePassword123!", role);
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        Assert.NotNull(apiResponse?.Data);
        return apiResponse.Data;
    }

    private void SetAuthHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task CompleteFinancialFlow_VerifyAtomicTransfer_AndLedgerIntegrity()
    {
        // 1. Register Alice (Sender) and Bob (Receiver)
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var alice = await RegisterUserAsync($"Alice {uniqueId}", $"alice_{uniqueId}@finpay.com", $"+92300{Random.Shared.Next(1000000, 9999999)}");
        var bob = await RegisterUserAsync($"Bob {uniqueId}", $"bob_{uniqueId}@finpay.com", $"+92301{Random.Shared.Next(1000000, 9999999)}");

        // 2. Deposit PKR 20,000 to Alice's wallet
        SetAuthHeader(alice.Token);
        var depositResponse = await _client.PostAsJsonAsync("/api/wallets/deposit", new DepositRequestDto(20_000m));
        depositResponse.EnsureSuccessStatusCode();
        var depositResult = await depositResponse.Content.ReadFromJsonAsync<ApiResponse<WalletResponseDto>>();
        Assert.Equal(20_000m, depositResult?.Data?.Balance);

        // 3. Execute atomic P2P transfer: Alice sends PKR 7,500 to Bob
        var transferRequest = new TransferRequestDto(bob.WalletNumber, Amount: 7_500m);
        var transferResponse = await _client.PostAsJsonAsync("/api/transfers/p2p", transferRequest);
        transferResponse.EnsureSuccessStatusCode();

        var transferResult = await transferResponse.Content.ReadFromJsonAsync<ApiResponse<TransferResponseDto>>();
        Assert.NotNull(transferResult?.Data);
        Assert.Equal(7_500m, transferResult.Data.Amount);
        Assert.Equal(0m, transferResult.Data.Fee);
        Assert.Equal(7_500m, transferResult.Data.NetCreditedAmount);
        Assert.Equal(12_500m, transferResult.Data.SenderBalanceAfter);
        Assert.Equal(TransactionStatus.Completed, transferResult.Data.Status);

        // 4. Verify Alice's balance is exactly 12,500
        var aliceWalletResponse = await _client.GetAsync("/api/wallets/my-wallet");
        var aliceWallet = await aliceWalletResponse.Content.ReadFromJsonAsync<ApiResponse<WalletResponseDto>>();
        Assert.Equal(12_500m, aliceWallet?.Data?.Balance);

        // 5. Verify Bob's balance is exactly 7,500
        SetAuthHeader(bob.Token);
        var bobWalletResponse = await _client.GetAsync("/api/wallets/my-wallet");
        var bobWallet = await bobWalletResponse.Content.ReadFromJsonAsync<ApiResponse<WalletResponseDto>>();
        Assert.Equal(7_500m, bobWallet?.Data?.Balance);

        // 6. Verify Bob's transaction history shows the incoming credit
        var bobHistoryResponse = await _client.GetAsync("/api/transactions/history?pageNumber=1&pageSize=10");
        var bobHistory = await bobHistoryResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TransactionResponseDto>>>();
        Assert.NotNull(bobHistory?.Data?.Items);
        Assert.Single(bobHistory.Data.Items);
        Assert.Equal("Credit", bobHistory.Data.Items[0].Direction);
        Assert.Equal(7_500m, bobHistory.Data.Items[0].Amount);
        Assert.Equal(7_500m, bobHistory.Data.Items[0].BalanceAfter);
    }

    [Fact]
    public async Task Transfer_RejectsSelfTransfer_WithBadRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var user = await RegisterUserAsync($"User {uniqueId}", $"user_{uniqueId}@finpay.com", $"+92302{Random.Shared.Next(1000000, 9999999)}");

        SetAuthHeader(user.Token);
        var response = await _client.PostAsJsonAsync("/api/transfers/p2p", new TransferRequestDto(user.WalletNumber, 500m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var apiError = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.Equal("SELF_TRANSFER_FORBIDDEN", apiError?.ErrorCode);
    }

    [Fact]
    public async Task Transfer_RejectsInsufficientFunds_WithBadRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var sender = await RegisterUserAsync($"Sender {uniqueId}", $"sender_{uniqueId}@finpay.com", $"+92303{Random.Shared.Next(1000000, 9999999)}");
        var receiver = await RegisterUserAsync($"Receiver {uniqueId}", $"rec_{uniqueId}@finpay.com", $"+92304{Random.Shared.Next(1000000, 9999999)}");

        SetAuthHeader(sender.Token);
        // Sender has 0 balance, attempts to transfer 1000
        var response = await _client.PostAsJsonAsync("/api/transfers/p2p", new TransferRequestDto(receiver.WalletNumber, 1_000m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var apiError = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.Equal("INSUFFICIENT_FUNDS", apiError?.ErrorCode);
    }

    [Fact]
    public async Task Admin_CanFreezeWallet_AndBlockedWalletCannotTransfer()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var admin = await RegisterUserAsync($"Admin {uniqueId}", $"admin_{uniqueId}@finpay.com", $"+92305{Random.Shared.Next(1000000, 9999999)}", UserRole.Admin);
        var targetUser = await RegisterUserAsync($"Target {uniqueId}", $"target_{uniqueId}@finpay.com", $"+92306{Random.Shared.Next(1000000, 9999999)}");
        var receiver = await RegisterUserAsync($"Rec {uniqueId}", $"rec2_{uniqueId}@finpay.com", $"+92307{Random.Shared.Next(1000000, 9999999)}");

        // Target deposits funds
        SetAuthHeader(targetUser.Token);
        await _client.PostAsJsonAsync("/api/wallets/deposit", new DepositRequestDto(5_000m));

        // Get target's wallet id
        var myWalletRes = await _client.GetAsync("/api/wallets/my-wallet");
        var walletData = await myWalletRes.Content.ReadFromJsonAsync<ApiResponse<WalletResponseDto>>();
        var walletId = walletData!.Data!.WalletId;

        // Admin freezes target's wallet
        SetAuthHeader(admin.Token);
        var freezeRes = await _client.PatchAsJsonAsync($"/api/admin/wallets/{walletId}/freeze", new FreezeWalletRequestDto(IsFrozen: true, Reason: "Compliance hold"));
        freezeRes.EnsureSuccessStatusCode();

        // Target user attempts to transfer while frozen -> MUST FAIL with 400 WALLET_FROZEN
        SetAuthHeader(targetUser.Token);
        var transferRes = await _client.PostAsJsonAsync("/api/transfers/p2p", new TransferRequestDto(receiver.WalletNumber, 500m));

        Assert.Equal(HttpStatusCode.BadRequest, transferRes.StatusCode);
        var apiError = await transferRes.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.Equal("WALLET_FROZEN", apiError?.ErrorCode);
    }

    [Fact]
    public async Task History_EnforcesOwnership_UserOnlyViewsTheirOwnTransactions()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var alice = await RegisterUserAsync($"Alice {uniqueId}", $"alice2_{uniqueId}@finpay.com", $"+92308{Random.Shared.Next(1000000, 9999999)}");
        var bob = await RegisterUserAsync($"Bob {uniqueId}", $"bob2_{uniqueId}@finpay.com", $"+92309{Random.Shared.Next(1000000, 9999999)}");
        var charlie = await RegisterUserAsync($"Charlie {uniqueId}", $"charlie_{uniqueId}@finpay.com", $"+92310{Random.Shared.Next(1000000, 9999999)}");

        // Alice transfers to Bob
        SetAuthHeader(alice.Token);
        await _client.PostAsJsonAsync("/api/wallets/deposit", new DepositRequestDto(10_000m));
        await _client.PostAsJsonAsync("/api/transfers/p2p", new TransferRequestDto(bob.WalletNumber, 2_000m));

        // Charlie checks history -> Charlie must see ZERO transactions
        SetAuthHeader(charlie.Token);
        var charlieHistoryRes = await _client.GetAsync("/api/transactions/history");
        var charlieHistory = await charlieHistoryRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TransactionResponseDto>>>();

        Assert.NotNull(charlieHistory?.Data);
        Assert.Empty(charlieHistory.Data.Items);
    }
}
