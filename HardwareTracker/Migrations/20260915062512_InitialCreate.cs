using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HardwareTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HireDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AssignedEmployeeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_Employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    // Nullable relationship ensures maintenance and disposal audit records remain intact if an employee is deleted.
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Department", "Email", "FirstName", "HireDate", "LastName", "TerminationDate" },
                values: new object[,]
                {
                    { 1, "IT", "anna.kowalska@company.com", "Anna", new DateTime(2023, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "Kowalska", null },
                    { 2, "DevOps", "marek.nowak@company.com", "Marek", new DateTime(2023, 3, 15, 8, 0, 0, 0, DateTimeKind.Utc), "Nowak", null },
                    { 3, "HR", "zofia.wisniewska@company.com", "Zofia", new DateTime(2022, 11, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Wisniewska", null }
                });

            migrationBuilder.InsertData(
                table: "Assets",
                columns: new[] { "Id", "AssignedEmployeeId", "Category", "Model", "SerialNumber", "Status" },
                values: new object[,]
                {
                    { 1, 1, "Laptop", "Dell Latitude 5540", "LT-2024-001", "Assigned" },
                    { 2, 2, "Laptop", "MacBook Pro 16", "LT-2024-002", "Assigned" },
                    { 3, null, "Monitor", "Dell UltraSharp U2723QE", "MN-2024-101", "Available" },
                    { 4, null, "Monitor", "LG 27UK850", "MN-2024-102", "InService" },
                    { 5, null, "Peripheral", "Logitech MX Keys", "KB-2023-550", "Disposed" }
                });

            migrationBuilder.InsertData(
                table: "AssignmentHistories",
                columns: new[] { "Id", "AssetId", "AssignedDate", "EmployeeId", "EmployeeName", "ReturnedDate", "Status" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 1, 15, 9, 0, 0, 0, DateTimeKind.Utc), 1, "Anna Kowalska", null, "Assigned" },
                    { 2, 2, new DateTime(2024, 2, 1, 10, 0, 0, 0, DateTimeKind.Utc), 2, "Marek Nowak", null, "Assigned" },
                    { 3, 3, new DateTime(2023, 6, 1, 8, 30, 0, 0, DateTimeKind.Utc), 3, "Zofia Wisniewska", new DateTime(2023, 12, 20, 16, 0, 0, 0, DateTimeKind.Utc), "Assigned" },
                    { 4, 4, new DateTime(2024, 3, 1, 11, 0, 0, 0, DateTimeKind.Utc), null, "Service", null, "InService" },
                    { 5, 5, new DateTime(2024, 4, 10, 14, 0, 0, 0, DateTimeKind.Utc), null, "Disposed", new DateTime(2024, 4, 10, 14, 0, 0, 0, DateTimeKind.Utc), "Disposed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssignedEmployeeId",
                table: "Assets",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_SerialNumber",
                table: "Assets",
                column: "SerialNumber",
                unique: true);

            // Composite index optimizes chronological timeline scans and lifecycle audits for a given asset.
            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_AssetId_AssignedDate",
                table: "AssignmentHistories",
                columns: new[] { "AssetId", "AssignedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_EmployeeId",
                table: "AssignmentHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentHistories");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}