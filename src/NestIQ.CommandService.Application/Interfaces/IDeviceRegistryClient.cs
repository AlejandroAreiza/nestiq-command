namespace NestIQ.CommandService.Application.Interfaces;

public interface IDeviceRegistryClient
{
    Task<DeviceDto?> GetDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
}

public record DeviceDto(Guid Id, string Name, string Type);
