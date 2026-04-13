using FluentAssertions;
using Moq;
using NestIQ.CommandService.Application.Interfaces;
using NestIQ.CommandService.Application.UseCases.Command;
using NestIQ.CommandService.Domain.Entities;
using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.UnitTests;

public class CommandHandlerTests
{
    private readonly Mock<ICommandRepository> _mockCommandRepository;
    private readonly Mock<IDeviceRegistryClient> _mockDeviceRegistryClient;
    private readonly Mock<IMqttPublisher> _mockMqttPublisher;
    private readonly CommandHandler _handler;

    public CommandHandlerTests()
    {
        _mockCommandRepository = new Mock<ICommandRepository>();
        _mockDeviceRegistryClient = new Mock<IDeviceRegistryClient>();
        _mockMqttPublisher = new Mock<IMqttPublisher>();
        _handler = new CommandHandler(
            _mockCommandRepository.Object,
            _mockDeviceRegistryClient.Object,
            _mockMqttPublisher.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenDeviceNotFound_ReturnsNull()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = new SendCommandCommand(deviceId, CommandType.TurnOn);

        _mockDeviceRegistryClient
            .Setup(x => x.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceDto?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeNull();
        _mockCommandRepository.Verify(
            x => x.AddAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenMqttPublishSucceeds_ReturnsResultWithSentStatus()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = new SendCommandCommand(deviceId, CommandType.TurnOn);
        var deviceDto = new DeviceDto(deviceId, "Test Device", "Switch");

        _mockDeviceRegistryClient
            .Setup(x => x.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deviceDto);

        _mockCommandRepository
            .Setup(x => x.AddAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceCommand cmd, CancellationToken ct) => cmd);

        _mockMqttPublisher
            .Setup(x => x.PublishCommandAsync(deviceId, CommandType.TurnOn, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCommandRepository
            .Setup(x => x.UpdateAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(deviceId);
        result.CommandType.Should().Be(CommandType.TurnOn);
        result.Status.Should().Be(CommandStatus.Sent);

        _mockCommandRepository.Verify(
            x => x.AddAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockMqttPublisher.Verify(
            x => x.PublishCommandAsync(deviceId, CommandType.TurnOn, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockCommandRepository.Verify(
            x => x.UpdateAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenMqttPublishFails_ReturnsResultWithFailedStatus()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = new SendCommandCommand(deviceId, CommandType.TurnOff);
        var deviceDto = new DeviceDto(deviceId, "Test Device", "Switch");

        _mockDeviceRegistryClient
            .Setup(x => x.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deviceDto);

        _mockCommandRepository
            .Setup(x => x.AddAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceCommand cmd, CancellationToken ct) => cmd);

        _mockMqttPublisher
            .Setup(x => x.PublishCommandAsync(deviceId, CommandType.TurnOff, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MQTT connection failed"));

        _mockCommandRepository
            .Setup(x => x.UpdateAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(deviceId);
        result.CommandType.Should().Be(CommandType.TurnOff);
        result.Status.Should().Be(CommandStatus.Failed);

        _mockCommandRepository.Verify(
            x => x.UpdateAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
