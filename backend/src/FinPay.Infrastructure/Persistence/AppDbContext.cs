using FinPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinPay.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<WalletBase> Wallets => Set<WalletBase>();
    public DbSet<BasicWallet> BasicWallets => Set<BasicWallet>();
    public DbSet<MerchantWallet> MerchantWallets => Set<MerchantWallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
