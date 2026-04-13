using Microsoft.EntityFrameworkCore;
using NestIQ.CommandService.Domain.Entities;

namespace NestIQ.CommandService.Infrastructure.Persistence;

public class CommandServiceDbContext : DbContext
{
    public CommandServiceDbContext(DbContextOptions<CommandServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommandServiceDbContext).Assembly);
    }
}
