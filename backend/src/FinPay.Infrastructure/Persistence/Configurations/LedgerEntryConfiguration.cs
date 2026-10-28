using FinPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinPay.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(le => le.Id);

        builder.Property(le => le.Id)
            .ValueGeneratedOnAdd();

        builder.Property(le => le.TransactionId)
            .IsRequired();

        builder.Property(le => le.WalletId)
            .IsRequired();

        builder.Property(le => le.EntryType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(le => le.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(le => le.BalanceAfter)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(le => le.Timestamp)
            .IsRequired();

        builder.HasIndex(le => le.WalletId);
        builder.HasIndex(le => le.TransactionId);
        builder.HasIndex(le => le.Timestamp);
    }
}
