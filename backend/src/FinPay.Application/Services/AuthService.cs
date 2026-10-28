using System.Security.Cryptography;
using FinPay.Application.DTOs.Auth;
using FinPay.Application.Exceptions;
using FinPay.Application.Interfaces;
using FinPay.Application.Interfaces.Repositories;
using FinPay.Application.Interfaces.Services;
using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinPay.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IWalletRepository walletRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _walletRepository = walletRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ValidationException("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ValidationException("Phone number is required.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ValidationException("Password must be at least 6 characters long.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedPhone = request.PhoneNumber.Trim();

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
            throw new ValidationException($"Email '{normalizedEmail}' is already registered.");

        if (await _userRepository.ExistsByPhoneNumberAsync(normalizedPhone, cancellationToken))
            throw new ValidationException($"Phone number '{normalizedPhone}' is already registered.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.FullName, normalizedEmail, normalizedPhone, passwordHash, request.Role);

        // Generate a cryptographically random, unique 10-digit wallet number
        var walletNumber = await GenerateUniqueWalletNumberAsync(cancellationToken);

        // Create initial Basic Wallet with 0 balance
        var wallet = new BasicWallet(user.Id, walletNumber, initialBalance: 0m);
        user.AssignWallet(wallet);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _userRepository.AddAsync(user, cancellationToken);
            await _walletRepository.AddAsync(wallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

        _logger.LogInformation("New user registered successfully: UserId={UserId}, WalletNumber={WalletNumber}, Role={Role}",
            user.Id, walletNumber, user.Role);

        var (token, expiresAt) = _tokenService.GenerateToken(user, walletNumber);

        return new AuthResponseDto(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role,
            WalletNumber: walletNumber,
            Token: token,
            ExpiresAt: expiresAt);
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new UnauthorizedException("Invalid email or password.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", normalizedEmail);
            throw new UnauthorizedException("Invalid email or password.");
        }

        var wallet = await _walletRepository.GetByUserIdAsync(user.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Wallet", $"UserId: {user.Id}");

        var (token, expiresAt) = _tokenService.GenerateToken(user, wallet.WalletNumber);

        _logger.LogInformation("User logged in successfully: UserId={UserId}, Email={Email}", user.Id, user.Email);

        return new AuthResponseDto(
            UserId: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            Role: user.Role,
            WalletNumber: wallet.WalletNumber,
            Token: token,
            ExpiresAt: expiresAt);
    }

    private async Task<string> GenerateUniqueWalletNumberAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate a 10-digit number where the first digit is 1-9
            var firstDigit = RandomNumberGenerator.GetInt32(1, 10);
            var remainingDigits = RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D9");
            var candidate = $"{firstDigit}{remainingDigits}";

            if (!await _walletRepository.ExistsByWalletNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new DomainValidationException("Unable to generate a unique wallet number. Please try again.");
    }
}
