using RatesService.Domain.Aggregates;
using RatesService.Domain.Entities;
using RatesService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace RatesService.Infrastructure.Data;

public class RatesServiceDbContext : DbContext
    {
        public DbSet<CryptoInstrument> CryptoInstruments { get; set; }

        public RatesServiceDbContext(DbContextOptions<RatesServiceDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CryptoInstrument>(builder =>
            {
                builder.HasKey(ci => ci.Id);
                builder.HasAlternateKey(ci => ci.Symbol);

                builder.Property(ci => ci.Symbol).IsRequired().HasMaxLength(10);
                builder.Property(ci => ci.Name).IsRequired().HasMaxLength(100);
                builder.Property(ci => ci.LastUpdated).IsRequired();
                
                builder.OwnsOne(ci => ci.CurrentRate, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("CurrentRateAmount").HasColumnType("decimal(18,8)").IsRequired();
                    money.Property(m => m.Currency).HasColumnName("CurrentRateCurrency").HasMaxLength(3).IsRequired();
                });

                builder.HasMany(ci => ci.HistoricalRates)
                       .WithOne()
                       .HasForeignKey("CryptoInstrumentId")
                       .OnDelete(DeleteBehavior.Cascade);

                builder.Navigation(ci => ci.HistoricalRates).Metadata.SetField("_historicalRates");
                builder.Navigation(ci => ci.HistoricalRates).UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            modelBuilder.Entity<HistoricalRate>(builder =>
            {
                builder.HasKey(hr => hr.Id);
                builder.Property(hr => hr.Timestamp).IsRequired();

                builder.OwnsOne(hr => hr.Rate, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("HistoricalRateAmount").HasColumnType("decimal(18,8)").IsRequired();
                    money.Property(m => m.Currency).HasColumnName("HistoricalRateCurrency").HasMaxLength(3).IsRequired();
                });
            });

            base.OnModelCreating(modelBuilder);
        }
    }