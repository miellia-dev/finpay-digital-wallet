using FinPay.Application.Common;
using FinPay.Application.DTOs.Wallet;
using FinPay.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IWalletService _walletService;

    public AdminController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>
    /// Freezes or unfreezes a target digital wallet. Restricted strictly to Admin role.
    /// </summary>
    [HttpPatch("wallets/{id:guid}/freeze")]
    [ProducesResponseType(typeof(ApiResponse<WalletResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FreezeWallet(
        [FromRoute] Guid id,
        [FromBody] FreezeWalletRequestDto request,
        CancellationToken cancellationToken)
    {
        var updated = await _walletService.FreezeWalletAsync(id, request.IsFrozen, request.Reason, cancellationToken);
        var action = request.IsFrozen ? "frozen" : "unfrozen";
        return Ok(ApiResponse<WalletResponseDto>.Ok(updated, $"Wallet successfully {action}."));
    }
}
