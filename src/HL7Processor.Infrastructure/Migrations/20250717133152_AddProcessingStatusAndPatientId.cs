using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HL7Processor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStatusAndPatientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PatientId",
                table: "Messages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "Messages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "Messages");
        }
    }
}
