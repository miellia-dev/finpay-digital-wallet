using FinPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinPay.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ReferenceId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => t.ReferenceId)
            .IsUnique();

        builder.Property(t => t.SenderWalletId)
            .IsRequired(false);

        builder.Property(t => t.ReceiverWalletId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(t => t.Fee)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.HasIndex(t => t.CreatedAt);

        // Ledger entries navigation
        builder.HasMany(t => t.LedgerEntries)
            .WithOne(le => le.Transaction)
            .HasForeignKey(le => le.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
