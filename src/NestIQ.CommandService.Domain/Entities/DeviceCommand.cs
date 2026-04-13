using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.Domain.Entities;

public class DeviceCommand
{
    public Guid Id { get; private set; }
    public Guid DeviceId { get; private set; }
    public CommandType CommandType { get; private set; }
    public CommandStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private DeviceCommand()
    {
    }

    public static DeviceCommand Create(Guid deviceId, CommandType commandType)
    {
        return new DeviceCommand
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            CommandType = commandType,
            Status = CommandStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSent()
    {
        Status = CommandStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = CommandStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}
