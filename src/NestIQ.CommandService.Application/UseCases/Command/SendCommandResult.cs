using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.Application.UseCases.Command;

public record SendCommandResult(
    Guid Id,
    Guid DeviceId,
    CommandType CommandType,
    CommandStatus Status,
    DateTime CreatedAt
);
