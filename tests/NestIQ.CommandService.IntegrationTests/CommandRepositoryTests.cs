using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NestIQ.CommandService.Domain.Entities;
using NestIQ.CommandService.Domain.Enums;
using NestIQ.CommandService.Infrastructure.Persistence;
using NestIQ.CommandService.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;

namespace NestIQ.CommandService.IntegrationTests;

public class CommandRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private CommandServiceDbContext _dbContext = null!;
    private CommandRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("nestiq_command_service_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<CommandServiceDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new CommandServiceDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new CommandRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistCommandToDatabase()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = DeviceCommand.Create(deviceId, CommandType.TurnOn);

        // Act
        var result = await _repository.AddAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.DeviceId.Should().Be(deviceId);
        result.CommandType.Should().Be(CommandType.TurnOn);
        result.Status.Should().Be(CommandStatus.Pending);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it's actually in the database
        var savedCommand = await _dbContext.DeviceCommands.FindAsync(result.Id);
        savedCommand.Should().NotBeNull();
        savedCommand!.DeviceId.Should().Be(deviceId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCommandStatus()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = DeviceCommand.Create(deviceId, CommandType.TurnOff);
        await _repository.AddAsync(command);

        // Act
        command.MarkAsSent();
        await _repository.UpdateAsync(command);

        // Assert
        var updatedCommand = await _dbContext.DeviceCommands.FindAsync(command.Id);
        updatedCommand.Should().NotBeNull();
        updatedCommand!.Status.Should().Be(CommandStatus.Sent);
        updatedCommand.UpdatedAt.Should().NotBeNull();
        updatedCommand.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCommandExists_ReturnsCommand()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = DeviceCommand.Create(deviceId, CommandType.TurnOn);
        await _repository.AddAsync(command);

        // Act
        var result = await _repository.GetByIdAsync(command.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(command.Id);
        result.DeviceId.Should().Be(deviceId);
        result.CommandType.Should().Be(CommandType.TurnOn);
        result.Status.Should().Be(CommandStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCommandDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithMultipleCommands_ShouldPersistAll()
    {
        // Arrange
        var deviceId1 = Guid.NewGuid();
        var deviceId2 = Guid.NewGuid();
        var command1 = DeviceCommand.Create(deviceId1, CommandType.TurnOn);
        var command2 = DeviceCommand.Create(deviceId2, CommandType.TurnOff);

        // Act
        await _repository.AddAsync(command1);
        await _repository.AddAsync(command2);

        // Assert
        var allCommands = await _dbContext.DeviceCommands.ToListAsync();
        allCommands.Should().HaveCount(2);
        allCommands.Should().Contain(c => c.DeviceId == deviceId1);
        allCommands.Should().Contain(c => c.DeviceId == deviceId2);
    }

    [Fact]
    public async Task UpdateAsync_WithFailedStatus_ShouldPersistCorrectly()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = DeviceCommand.Create(deviceId, CommandType.TurnOn);
        await _repository.AddAsync(command);

        // Act
        command.MarkAsFailed();
        await _repository.UpdateAsync(command);

        // Assert
        var updatedCommand = await _dbContext.DeviceCommands.FindAsync(command.Id);
        updatedCommand.Should().NotBeNull();
        updatedCommand!.Status.Should().Be(CommandStatus.Failed);
        updatedCommand.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldStoreEnumsAsStrings()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var command = DeviceCommand.Create(deviceId, CommandType.TurnOn);

        // Act
        await _repository.AddAsync(command);

        // Assert - verify enums are stored as strings by querying raw SQL
        await using var conn = _dbContext.Database.GetDbConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT command_type, status FROM device_commands WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = command.Id;
        cmd.Parameters.Add(param);

        await using var reader = await cmd.ExecuteReaderAsync();
        reader.Read().Should().BeTrue();
        reader.GetString(0).Should().Be("TurnOn");
        reader.GetString(1).Should().Be("Pending");
    }
}
