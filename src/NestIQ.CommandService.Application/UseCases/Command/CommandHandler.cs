using NestIQ.CommandService.Application.Interfaces;
using NestIQ.CommandService.Domain.Entities;

namespace NestIQ.CommandService.Application.UseCases.Command;

public class CommandHandler
{
    private readonly ICommandRepository _commandRepository;
    private readonly IDeviceRegistryClient _deviceRegistryClient;
    private readonly IMqttPublisher _mqttPublisher;

    public CommandHandler(
        ICommandRepository commandRepository,
        IDeviceRegistryClient deviceRegistryClient,
        IMqttPublisher mqttPublisher)
    {
        _commandRepository = commandRepository;
        _deviceRegistryClient = deviceRegistryClient;
        _mqttPublisher = mqttPublisher;
    }

    public async Task<SendCommandResult?> HandleAsync(
        SendCommandCommand command,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate device exists in Device Registry
        var device = await _deviceRegistryClient.GetDeviceByIdAsync(
            command.DeviceId,
            cancellationToken);

        if (device == null)
        {
            return null; // Device not found
        }

        // Step 2: Create and save command with Pending status
        var deviceCommand = DeviceCommand.Create(command.DeviceId, command.CommandType);
        await _commandRepository.AddAsync(deviceCommand, cancellationToken);

        // Step 3: Publish to MQTT
        try
        {
            await _mqttPublisher.PublishCommandAsync(
                command.DeviceId,
                command.CommandType,
                cancellationToken);

            deviceCommand.MarkAsSent();
        }
        catch
        {
            deviceCommand.MarkAsFailed();
        }

        // Step 4: Update command status
        await _commandRepository.UpdateAsync(deviceCommand, cancellationToken);

        // Step 5: Return result
        return new SendCommandResult(
            deviceCommand.Id,
            deviceCommand.DeviceId,
            deviceCommand.CommandType,
            deviceCommand.Status,
            deviceCommand.CreatedAt
        );
    }
}
