using FinPay.Application.DTOs.Auth;
using FinPay.Application.Exceptions;
using FinPay.Application.Interfaces;
using FinPay.Application.Interfaces.Repositories;
using FinPay.Application.Interfaces.Services;
using FinPay.Application.Services;
using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinPay.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _walletRepoMock = new Mock<IWalletRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>((func, _) => func());

        _authService = new AuthService(
            _userRepoMock.Object,
            _walletRepoMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserAndBasicWallet_AndReturnsToken()
    {
        // Arrange
        var request = new RegisterRequestDto("Jane Doe", "jane@finpay.com", "+923009876543", "StrongPass123!");

        _userRepoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepoMock.Setup(r => r.ExistsByPhoneNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _walletRepoMock.Setup(r => r.ExistsByWalletNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(p => p.HashPassword(request.Password))
            .Returns("hashed_secure_pwd");
        _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<string>()))
            .Returns(("jwt_test_token", DateTime.UtcNow.AddHours(1)));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result.FullName);
        Assert.Equal("jane@finpay.com", result.Email);
        Assert.Equal(10, result.WalletNumber.Length);
        Assert.Equal("jwt_test_token", result.Token);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "jane@finpay.com"), It.IsAny<CancellationToken>()), Times.Once);
        _walletRepoMock.Verify(r => r.AddAsync(It.Is<WalletBase>(w => w.Balance == 0m && w.WalletType == WalletType.Basic), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequestDto("Jane Doe", "duplicate@finpay.com", "+923001112233", "Pass1234");
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("duplicate@finpay.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorizedException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new User("Jane Doe", "jane@finpay.com", "+923001112233", "correct_hash");
        _userRepoMock.Setup(r => r.GetByEmailAsync("jane@finpay.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("wrong_pwd", "correct_hash"))
            .Returns(false);

        var request = new LoginRequestDto("jane@finpay.com", "wrong_pwd");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.LoginAsync(request));
    }
}
