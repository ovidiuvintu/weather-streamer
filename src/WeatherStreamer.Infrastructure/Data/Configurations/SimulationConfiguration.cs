using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherStreamer.Domain.Entities;
using WeatherStreamer.Domain.Enums;

namespace WeatherStreamer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for the Simulation entity.
/// Implements explicit configuration as required by Constitution Principle XIV.
/// </summary>
public class SimulationConfiguration : IEntityTypeConfiguration<Simulation>
{
    public void Configure(EntityTypeBuilder<Simulation> builder)
    {
        // Table name
        builder.ToTable("Simulations");

        // Primary key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        // Name property
        builder.Property(s => s.Name)
            .HasMaxLength(70)
            .IsRequired();

        // StartTime property (stored as UTC)
        builder.Property(s => s.StartTime)
            .HasColumnType("datetime2")
            .IsRequired();

        // FileName property
        builder.Property(s => s.FileName)
            .HasMaxLength(260)
            .IsRequired();

        // Status property (stored as string, converted from enum)
        builder.Property(s => s.Status)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired()
            .HasDefaultValue(SimulationStatus.NotStarted);

        // Indexes for query optimization
        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Simulations_Status");

        builder.HasIndex(s => new { s.FileName, s.Status })
            .HasDatabaseName("IX_Simulations_FileName_Status");

        builder.HasIndex(s => s.StartTime)
            .HasDatabaseName("IX_Simulations_StartTime");

        // Check constraint for Status values (using ToTable)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Simulations_Status",
            "Status IN ('NotStarted', 'InProgress', 'Completed')"
        ));

        // Concurrency token configuration. Avoid configuring RowVersion as a
        // store-generated value so providers that don't auto-generate rowversion
        // (SQLite/InMemory) will persist client-assigned tokens.
        builder.Property(s => s.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        // Soft-delete flag
        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Optional global query filter to hide soft-deleted rows from normal queries
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
