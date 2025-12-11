using Microsoft.EntityFrameworkCore;
using SalesPosting.Data.Entities;

namespace SalesPosting.Data.Data
{
    public class SalesDbContext : DbContext
    {
        public SalesDbContext(DbContextOptions<SalesDbContext> options)
            : base(options)
        {
        }

        public DbSet<SalesPayload> SalesPayloads { get; set; }
        public DbSet<ProcessingError> ProcessingErrors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SalesPayload configuration
            modelBuilder.Entity<SalesPayload>(entity =>
            {
                entity.ToTable("SalesPayload");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TransactionId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TerminalId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.RetryCount)
                    .HasDefaultValue(0);

                // Indexes
                entity.HasIndex(e => e.TransactionId)
                    .IsUnique()
                    .HasDatabaseName("UQ_SalesPayload_TransactionId");

                entity.HasIndex(e => new { e.Status, e.CreatedDate })
                    .HasDatabaseName("IX_SalesPayload_Status_CreatedDate");
            });

            // ProcessingError configuration
            modelBuilder.Entity<ProcessingError>(entity =>
            {
                entity.ToTable("ProcessingError");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OccurredAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.SalesPayload)
                    .WithMany(p => p.ProcessingErrors)
                    .HasForeignKey(e => e.SalesPayloadId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}