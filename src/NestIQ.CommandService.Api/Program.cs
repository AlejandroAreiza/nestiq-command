using Microsoft.EntityFrameworkCore;
using NestIQ.CommandService.Application.Interfaces;
using NestIQ.CommandService.Application.UseCases.Command;
using NestIQ.CommandService.Infrastructure.Http;
using NestIQ.CommandService.Infrastructure.Mqtt;
using NestIQ.CommandService.Infrastructure.Persistence;
using NestIQ.CommandService.Infrastructure.Persistence.Repositories;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=nestiq_command;Username=Jupiter";

builder.Services.AddDbContext<CommandServiceDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<ICommandRepository, CommandRepository>();

// HTTP Clients
var deviceRegistryBaseUrl = builder.Configuration["DeviceRegistry:BaseUrl"]
    ?? "http://localhost:5001";

builder.Services
    .AddRefitClient<IDeviceRegistryApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(deviceRegistryBaseUrl));

builder.Services.AddScoped<IDeviceRegistryClient, DeviceRegistryClient>();

// MQTT
builder.Services.AddScoped<IMqttPublisher, MqttPublisher>();

// Use Cases
builder.Services.AddScoped<CommandHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// For WebApplicationFactory in tests
public partial class Program { }
