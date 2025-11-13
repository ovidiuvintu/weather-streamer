using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherStreamer.Domain.Entities;

namespace WeatherStreamer.Infrastructure.Data.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Actor).IsRequired().HasMaxLength(200);
        builder.Property(a => a.CorrelationId).HasMaxLength(200);
        builder.Property(a => a.TimestampUtc).IsRequired();
        builder.Property(a => a.ChangesJson).IsRequired();
        builder.Property(a => a.PrevETag).HasMaxLength(100);
        builder.Property(a => a.NewETag).HasMaxLength(100);
    }
}
