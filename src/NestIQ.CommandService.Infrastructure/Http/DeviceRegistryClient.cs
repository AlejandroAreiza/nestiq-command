using NestIQ.CommandService.Application.Interfaces;
using Refit;

namespace NestIQ.CommandService.Infrastructure.Http;

public interface IDeviceRegistryApi
{
    [Get("/api/devices/{id}")]
    Task<DeviceDto?> GetDeviceByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public class DeviceRegistryClient : IDeviceRegistryClient
{
    private readonly IDeviceRegistryApi _api;

    public DeviceRegistryClient(IDeviceRegistryApi api)
    {
        _api = api;
    }

    public async Task<DeviceDto?> GetDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _api.GetDeviceByIdAsync(deviceId, cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
