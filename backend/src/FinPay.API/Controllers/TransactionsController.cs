using FinPay.Application.Common;
using FinPay.Application.DTOs.Transaction;
using FinPay.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ICurrentUserService _currentUserService;

    public TransactionsController(IWalletService walletService, ICurrentUserService currentUserService)
    {
        _walletService = walletService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieve paginated statement and transaction history strictly scoped to the authenticated user's wallet.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TransactionResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] TransactionHistoryQueryDto query,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var history = await _walletService.GetHistoryAsync(userId, query, cancellationToken);
        return Ok(ApiResponse<PagedResult<TransactionResponseDto>>.Ok(history));
    }
}
