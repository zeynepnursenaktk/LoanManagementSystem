using Microsoft.EntityFrameworkCore;
using LoanManagement.Entities.Models;

namespace LoanManagement.DataAccess.Context;

public class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users { get; set; }  
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Installment> Installments { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Customer> Customers { get; set; }  

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Loan>()
            .Property(l => l.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Loan>()
            .Property(l => l.ProfitRate)
            .HasColumnType("decimal(18,4)");

        modelBuilder.Entity<Installment>()
            .Property(i => i.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");



        // İlişkiler

        // Customer → Loans (1-N)
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Loans)
            .WithOne(l => l.Customer)
            .HasForeignKey(l => l.CustomerId);

        // Loan → Installments (1-N)
        modelBuilder.Entity<Loan>()
            .HasMany(l => l.Installments)
            .WithOne(i => i.Loan)
            .HasForeignKey(i => i.LoanId);

        // Installment → Payment (1-1)
        modelBuilder.Entity<Installment>()
            .HasOne(i => i.Payment)
            .WithOne(p => p.Installment)
            .HasForeignKey<Payment>(p => p.InstallmentId);
    }
}