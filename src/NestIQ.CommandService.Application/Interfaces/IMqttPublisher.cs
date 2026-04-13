using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.Application.Interfaces;

public interface IMqttPublisher
{
    Task PublishCommandAsync(Guid deviceId, CommandType commandType, CancellationToken cancellationToken = default);
}
