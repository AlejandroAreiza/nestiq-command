using Microsoft.AspNetCore.Mvc;
using NestIQ.CommandService.Application.UseCases.Command;
using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly CommandHandler _commandHandler;
    private readonly ILogger<CommandsController> _logger;

    public CommandsController(
        CommandHandler commandHandler,
        ILogger<CommandsController> logger)
    {
        _commandHandler = commandHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendCommand(
        [FromBody] SendCommandRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CommandType>(request.CommandType, ignoreCase: true, out var commandType))
        {
            return BadRequest(new { error = "Invalid command type. Valid values: TurnOn, TurnOff" });
        }

        var command = new SendCommandCommand(request.DeviceId, commandType);

        var result = await _commandHandler.HandleAsync(command, cancellationToken);

        if (result == null)
        {
            return NotFound(new { error = "Device not found in Device Registry" });
        }

        _logger.LogInformation(
            "Command {CommandType} sent to device {DeviceId} with status {Status}",
            result.CommandType,
            result.DeviceId,
            result.Status);

        return Accepted(result);
    }
}

public record SendCommandRequest(Guid DeviceId, string CommandType);
