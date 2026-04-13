using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NestIQ.CommandService.Application.Interfaces;
using NestIQ.CommandService.Application.UseCases.Command;
using NestIQ.CommandService.Domain.Entities;
using NestIQ.CommandService.Domain.Enums;

namespace NestIQ.CommandService.ComponentTests;

public class CommandsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CommandsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SendCommand_WhenDeviceExists_Returns202Accepted()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var mockDeviceRegistryClient = new Mock<IDeviceRegistryClient>();
        var mockMqttPublisher = new Mock<IMqttPublisher>();
        var mockCommandRepository = new Mock<ICommandRepository>();

        mockDeviceRegistryClient
            .Setup(x => x.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeviceDto(deviceId, "Test Device", "Switch"));

        mockCommandRepository
            .Setup(x => x.AddAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceCommand cmd, CancellationToken ct) => cmd);

        mockMqttPublisher
            .Setup(x => x.PublishCommandAsync(It.IsAny<Guid>(), It.IsAny<CommandType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockCommandRepository
            .Setup(x => x.UpdateAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var deviceRegistryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDeviceRegistryClient));
                if (deviceRegistryDescriptor != null)
                    services.Remove(deviceRegistryDescriptor);

                var mqttDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMqttPublisher));
                if (mqttDescriptor != null)
                    services.Remove(mqttDescriptor);

                var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICommandRepository));
                if (repoDescriptor != null)
                    services.Remove(repoDescriptor);

                // Add mocks
                services.AddScoped(_ => mockDeviceRegistryClient.Object);
                services.AddScoped(_ => mockMqttPublisher.Object);
                services.AddScoped(_ => mockCommandRepository.Object);
            });
        }).CreateClient();

        var request = new { deviceId, commandType = "TurnOn" };

        // Act
        var response = await client.PostAsJsonAsync("/api/commands", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var result = await response.Content.ReadFromJsonAsync<SendCommandResult>();
        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(deviceId);
        result.CommandType.Should().Be(CommandType.TurnOn);
        result.Status.Should().Be(CommandStatus.Sent);
    }

    [Fact]
    public async Task SendCommand_WhenDeviceNotFound_Returns404NotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var mockDeviceRegistryClient = new Mock<IDeviceRegistryClient>();
        var mockMqttPublisher = new Mock<IMqttPublisher>();
        var mockCommandRepository = new Mock<ICommandRepository>();

        mockDeviceRegistryClient
            .Setup(x => x.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceDto?)null);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var deviceRegistryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDeviceRegistryClient));
                if (deviceRegistryDescriptor != null)
                    services.Remove(deviceRegistryDescriptor);

                var mqttDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMqttPublisher));
                if (mqttDescriptor != null)
                    services.Remove(mqttDescriptor);

                var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICommandRepository));
                if (repoDescriptor != null)
                    services.Remove(repoDescriptor);

                services.AddScoped(_ => mockDeviceRegistryClient.Object);
                services.AddScoped(_ => mockMqttPublisher.Object);
                services.AddScoped(_ => mockCommandRepository.Object);
            });
        }).CreateClient();

        var request = new { deviceId, commandType = "TurnOn" };

        // Act
        var response = await client.PostAsJsonAsync("/api/commands", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendCommand_WhenInvalidCommandType_Returns400BadRequest()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var mockDeviceRegistryClient = new Mock<IDeviceRegistryClient>();
        var mockMqttPublisher = new Mock<IMqttPublisher>();
        var mockCommandRepository = new Mock<ICommandRepository>();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var deviceRegistryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDeviceRegistryClient));
                if (deviceRegistryDescriptor != null)
                    services.Remove(deviceRegistryDescriptor);

                var mqttDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMqttPublisher));
                if (mqttDescriptor != null)
                    services.Remove(mqttDescriptor);

                var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICommandRepository));
                if (repoDescriptor != null)
                    services.Remove(repoDescriptor);

                services.AddScoped(_ => mockDeviceRegistryClient.Object);
                services.AddScoped(_ => mockMqttPublisher.Object);
                services.AddScoped(_ => mockCommandRepository.Object);
            });
        }).CreateClient();

        var request = new { deviceId, commandType = "InvalidCommand" };

        // Act
        var response = await client.PostAsJsonAsync("/api/commands", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendCommand_WhenMqttFails_Returns202WithFailedStatus()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var mockDeviceRegistryClient = new Mock<IDeviceRegistryClient>();
        var mockMqttPublisher = new Mock<IMqttPublisher>();
        var mockCommandRepository = new Mock<ICommandRepository>();

        mockDeviceRegistryClient
            .Setup(x => x.GetDeviceByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeviceDto(deviceId, "Test Device", "Switch"));

        mockCommandRepository
            .Setup(x => x.AddAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceCommand cmd, CancellationToken ct) => cmd);

        mockMqttPublisher
            .Setup(x => x.PublishCommandAsync(It.IsAny<Guid>(), It.IsAny<CommandType>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("MQTT broker unavailable"));

        mockCommandRepository
            .Setup(x => x.UpdateAsync(It.IsAny<DeviceCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var deviceRegistryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDeviceRegistryClient));
                if (deviceRegistryDescriptor != null)
                    services.Remove(deviceRegistryDescriptor);

                var mqttDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMqttPublisher));
                if (mqttDescriptor != null)
                    services.Remove(mqttDescriptor);

                var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICommandRepository));
                if (repoDescriptor != null)
                    services.Remove(repoDescriptor);

                services.AddScoped(_ => mockDeviceRegistryClient.Object);
                services.AddScoped(_ => mockMqttPublisher.Object);
                services.AddScoped(_ => mockCommandRepository.Object);
            });
        }).CreateClient();

        var request = new { deviceId, commandType = "TurnOff" };

        // Act
        var response = await client.PostAsJsonAsync("/api/commands", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var result = await response.Content.ReadFromJsonAsync<SendCommandResult>();
        result.Should().NotBeNull();
        result!.Status.Should().Be(CommandStatus.Failed);
    }
}
