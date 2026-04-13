using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NestIQ.CommandService.Domain.Entities;
using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.Infrastructure.Persistence.Configurations;

public class DeviceCommandConfiguration : IEntityTypeConfiguration<DeviceCommand>
{
    public void Configure(EntityTypeBuilder<DeviceCommand> builder)
    {
        builder.ToTable("device_commands");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(c => c.DeviceId)
            .HasColumnName("device_id")
            .IsRequired();

        builder.Property(c => c.CommandType)
            .HasColumnName("command_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(c => c.DeviceId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedAt);
    }
}
