using Microsoft.EntityFrameworkCore;
using PolicyService.Domain.Entities;
using PolicyService.Domain.Enums;

namespace PolicyService.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core DbContext for Policy Service
    /// Configured for Azure SQL Database
    /// </summary>
    public class PolicyDbContext : DbContext
    {
        public PolicyDbContext(DbContextOptions<PolicyDbContext> options)
            : base(options)
        {
        }

        public DbSet<Policy> Policies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Policy entity
            modelBuilder.Entity<Policy>(entity =>
            {
                // Table name
                entity.ToTable("Policies");

                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.Id)
                    .ValueGeneratedNever(); // We generate Guid in domain

                entity.Property(e => e.PolicyNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CustomerName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CustomerEmail)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.CustomerAge)
                    .IsRequired();

                entity.Property(e => e.Premium)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // Enum conversions to string (better for Azure SQL)
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.PolicyType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.ProcessedAt)
                    .IsRequired(false);

                // Indexes for performance
                entity.HasIndex(e => e.PolicyNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Policies_PolicyNumber");

                entity.HasIndex(e => e.CustomerEmail)
                    .HasDatabaseName("IX_Policies_CustomerEmail");

                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_Policies_Status");

                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Policies_CreatedAt");
            });

            // Seed some initial data (optional)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // We'll seed data later if needed
            // For now, leave empty
        }
    }
}