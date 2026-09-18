using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HardwareTracker.Migrations
{
    /// <summary>
    /// Adds lifecycle status tracking to historical assignment records using string-based enum representations.
    /// </summary>
    public partial class AddLifecycleTrackingToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Align column definition with HasConversion<string>() configured in AppDbContext.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AssignmentHistories",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Assigned");

            // Infer appropriate status for pre-existing dynamic records prior to applying fixed seed updates.
            migrationBuilder.Sql(
                @"UPDATE AssignmentHistories 
                  SET Status = CASE 
                      WHEN ReturnedDate IS NULL THEN 'Assigned' 
                      ELSE 'Available' 
                  END;");

            migrationBuilder.UpdateData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Status",
                value: "Assigned");

            migrationBuilder.UpdateData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 2,
                column: "Status",
                value: "Assigned");

            migrationBuilder.UpdateData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 3,
                column: "Status",
                value: "Assigned");

            migrationBuilder.InsertData(
                table: "AssignmentHistories",
                columns: new[] { "Id", "AssetId", "AssignedDate", "EmployeeId", "EmployeeName", "ReturnedDate", "Status" },
                values: new object[,]
                {
                    { 4, 4, new DateTime(2024, 3, 1, 11, 0, 0, 0, DateTimeKind.Utc), null, "Service", null, "InService" },
                    { 5, 5, new DateTime(2024, 4, 10, 14, 0, 0, 0, DateTimeKind.Utc), null, "Disposed", new DateTime(2024, 4, 10, 14, 0, 0, 0, DateTimeKind.Utc), "Disposed" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AssignmentHistories");
        }
    }
}