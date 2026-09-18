using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareTracker.Migrations
{
    /// <summary>
    /// Introduces employment timeline tracking and offboarding support to employee entities.
    /// </summary>
    public partial class AddEmployeeDatesAndOffboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use database timestamp as a reliable fallback instead of DateTime.MinValue with Unspecified kind.
            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationDate",
                table: "Employees",
                type: "TEXT",
                nullable: true);

            // Infer HireDate from earliest hardware assignment if available, otherwise preserve CURRENT_TIMESTAMP.
            migrationBuilder.Sql(
                @"UPDATE Employees 
                  SET HireDate = COALESCE(
                      (SELECT MIN(AssignedDate) FROM AssignmentHistories WHERE AssignmentHistories.EmployeeId = Employees.Id),
                      HireDate
                  );");

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HireDate", "TerminationDate" },
                values: new object[] { new DateTime(2023, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HireDate", "TerminationDate" },
                values: new object[] { new DateTime(2023, 3, 15, 8, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HireDate", "TerminationDate" },
                values: new object[] { new DateTime(2022, 11, 1, 8, 0, 0, 0, DateTimeKind.Utc), null });

            // Partial index accelerates active employee lookups by ignoring terminated personnel.
            migrationBuilder.CreateIndex(
                name: "IX_Employees_TerminationDate",
                table: "Employees",
                column: "TerminationDate",
                filter: "[TerminationDate] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_TerminationDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                table: "Employees");
        }
    }
}