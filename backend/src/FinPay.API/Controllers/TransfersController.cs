using FinPay.Application.Common;
using FinPay.Application.DTOs.Transfer;
using FinPay.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly ICurrentUserService _currentUserService;

    public TransfersController(ITransferService transferService, ICurrentUserService currentUserService)
    {
        _transferService = transferService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Execute an atomic peer-to-peer (P2P) fund transfer to a recipient wallet number or phone number.
    /// </summary>
    [HttpPost("p2p")]
    [ProducesResponseType(typeof(ApiResponse<TransferResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransferP2P([FromBody] TransferRequestDto request, CancellationToken cancellationToken)
    {
        // Identify sender exclusively from cryptographically signed claims
        var senderUserId = _currentUserService.GetRequiredUserId();
        var result = await _transferService.TransferFundsAsync(senderUserId, request, cancellationToken);
        return Ok(ApiResponse<TransferResponseDto>.Ok(result, "P2P transfer completed successfully."));
    }
}
