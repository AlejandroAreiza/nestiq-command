using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestIQ.CommandService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_commands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    command_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_commands", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_created_at",
                table: "device_commands",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_device_id",
                table: "device_commands",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_status",
                table: "device_commands",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_commands");
        }
    }
}
