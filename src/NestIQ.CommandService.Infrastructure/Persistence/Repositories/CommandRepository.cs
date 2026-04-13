using Microsoft.EntityFrameworkCore;
using NestIQ.CommandService.Application.Interfaces;
using NestIQ.CommandService.Domain.Entities;

namespace NestIQ.CommandService.Infrastructure.Persistence.Repositories;

public class CommandRepository : ICommandRepository
{
    private readonly CommandServiceDbContext _context;

    public CommandRepository(CommandServiceDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceCommand> AddAsync(DeviceCommand command, CancellationToken cancellationToken = default)
    {
        await _context.DeviceCommands.AddAsync(command, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return command;
    }

    public async Task UpdateAsync(DeviceCommand command, CancellationToken cancellationToken = default)
    {
        _context.DeviceCommands.Update(command);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeviceCommand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceCommands
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
