using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinPay.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<WalletBase>
{
    public void Configure(EntityTypeBuilder<WalletBase> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.HasIndex(w => w.UserId)
            .IsUnique();

        builder.Property(w => w.WalletNumber)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(w => w.WalletNumber)
            .IsUnique();

        builder.Property(w => w.Balance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(w => w.IsFrozen)
            .IsRequired()
            .HasDefaultValue(false);

        // Optimistic concurrency token
        builder.Property(w => w.RowVersion)
            .IsRowVersion();

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        // Table-Per-Hierarchy (TPH) Inheritance Mapping
        builder.HasDiscriminator<WalletType>("WalletType")
            .HasValue<BasicWallet>(WalletType.Basic)
            .HasValue<MerchantWallet>(WalletType.Merchant);

        // Foreign key relationships to Transactions
        builder.HasMany(w => w.SentTransactions)
            .WithOne(t => t.SenderWallet)
            .HasForeignKey(t => t.SenderWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.ReceivedTransactions)
            .WithOne(t => t.ReceiverWallet)
            .HasForeignKey(t => t.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.LedgerEntries)
            .WithOne(le => le.Wallet)
            .HasForeignKey(le => le.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
