using NestIQ.CommandService.Domain.Entities;

namespace NestIQ.CommandService.Application.Interfaces;

public interface ICommandRepository
{
    Task<DeviceCommand> AddAsync(DeviceCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(DeviceCommand command, CancellationToken cancellationToken = default);
    Task<DeviceCommand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
