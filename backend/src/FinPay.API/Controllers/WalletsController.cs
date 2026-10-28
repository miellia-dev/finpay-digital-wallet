using FinPay.Application.Common;
using FinPay.Application.DTOs.Wallet;
using FinPay.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ICurrentUserService _currentUserService;

    public WalletsController(IWalletService walletService, ICurrentUserService currentUserService)
    {
        _walletService = walletService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieve details and current balance of the authenticated user's digital wallet.
    /// </summary>
    [HttpGet("my-wallet")]
    [ProducesResponseType(typeof(ApiResponse<WalletResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyWallet(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var wallet = await _walletService.GetMyWalletAsync(userId, cancellationToken);
        return Ok(ApiResponse<WalletResponseDto>.Ok(wallet));
    }

    /// <summary>
    /// Simulated top-up endpoint for development and testing environments.
    /// Atomically credits the wallet and records transaction and ledger entries.
    /// </summary>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(ApiResponse<WalletResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Deposit([FromBody] DepositRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updatedWallet = await _walletService.DepositAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<WalletResponseDto>.Ok(updatedWallet, "Simulated deposit credited successfully."));
    }
}
