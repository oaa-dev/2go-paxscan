using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaxScan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AppVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SecretHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PairedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastSyncAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastSeq = table.Column<long>(type: "INTEGER", nullable: false),
                    PendingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SeqGapDetected = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClockOffsetMs = table.Column<long>(type: "INTEGER", nullable: true),
                    SyncRequested = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hub_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    HubId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WebBaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HubTokenProtected = table.Column<byte[]>(type: "BLOB", nullable: true),
                    AdvertisedHost = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    LastWebSyncAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DatasetVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hub_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pairing_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UsedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pairing_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "task_status_updates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BaseVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceSeq = table.Column<long>(type: "INTEGER", nullable: false),
                    DeviceCreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    HubReceivedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    BusinessStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RejectReason = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResultingVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    WebBaseVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    UploadStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OnlineAckedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    UploadError = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_status_updates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    WebVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pairing_tokens_TokenHash",
                table: "pairing_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_status_updates_DeviceId_DeviceSeq",
                table: "task_status_updates",
                columns: new[] { "DeviceId", "DeviceSeq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_status_updates_TaskId",
                table: "task_status_updates",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_task_status_updates_UploadStatus_HubReceivedAt",
                table: "task_status_updates",
                columns: new[] { "UploadStatus", "HubReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "hub_settings");

            migrationBuilder.DropTable(
                name: "pairing_tokens");

            migrationBuilder.DropTable(
                name: "task_status_updates");

            migrationBuilder.DropTable(
                name: "tasks");
        }
    }
}
