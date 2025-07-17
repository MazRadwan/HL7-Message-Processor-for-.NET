using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HL7Processor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Messages_PatientId",
                table: "Messages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ProcessingStatus",
                table: "Messages",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Timestamp",
                table: "Messages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageType",
                table: "Messages",
                column: "MessageType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_PatientId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ProcessingStatus",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Timestamp",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_MessageType",
                table: "Messages");
        }
    }
}
