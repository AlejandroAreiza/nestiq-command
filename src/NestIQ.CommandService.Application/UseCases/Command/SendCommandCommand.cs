using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.Application.UseCases.Command;

public record SendCommandCommand(Guid DeviceId, CommandType CommandType);
