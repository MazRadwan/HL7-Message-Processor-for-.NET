using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HL7Processor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterMessageIdToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParserMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DelimiterDetected = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SegmentCount = table.Column<int>(type: "int", nullable: true),
                    FieldCount = table.Column<int>(type: "int", nullable: true),
                    ComponentCount = table.Column<int>(type: "int", nullable: true),
                    ParseTimeMs = table.Column<int>(type: "int", nullable: false),
                    MemoryUsedBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParserMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidationLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    ValidationDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingTimeMs = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationResults_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_MessageId",
                table: "ValidationResults",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParserMetrics");

            migrationBuilder.DropTable(
                name: "ValidationResults");
        }
    }
}
