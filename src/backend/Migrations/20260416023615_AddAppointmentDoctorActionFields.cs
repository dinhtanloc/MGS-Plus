using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MGSPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentDoctorActionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Appointments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RescheduleReason",
                table: "Appointments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduledTo",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduleReason",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RescheduledTo",
                table: "Appointments");
        }
    }
}
