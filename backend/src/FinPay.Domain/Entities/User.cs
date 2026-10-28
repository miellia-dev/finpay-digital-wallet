using FinPay.Domain.Common;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;

namespace FinPay.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Customer;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation property for 1:1 relationship with Wallet
    public WalletBase? Wallet { get; private set; }

    // Parameterless constructor for EF Core
    protected User() { }

    public User(string fullName, string email, string phoneNumber, string passwordHash, UserRole role = UserRole.Customer)
    {
        SetFullName(fullName);
        SetEmail(email);
        SetPhoneNumber(phoneNumber);
        SetPasswordHash(passwordHash);
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("Full name cannot be null or empty.");

        FullName = fullName.Trim();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainValidationException("A valid email address is required.");

        Email = email.Trim().ToLowerInvariant();
    }

    public void SetPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainValidationException("Phone number cannot be null or empty.");

        PhoneNumber = phoneNumber.Trim();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainValidationException("Password hash cannot be empty.");

        PasswordHash = passwordHash;
    }

    public void AssignWallet(WalletBase wallet)
    {
        ArgumentNullException.ThrowIfNull(wallet);
        Wallet = wallet;
    }

    public void UpgradeToAdmin()
    {
        Role = UserRole.Admin;
    }
}
