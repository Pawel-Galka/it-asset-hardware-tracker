using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareTracker.Migrations
{
    /// <summary>
    /// Decouples historical assignment logs from employee records by introducing an immutable snapshot column.
    /// </summary>
    public partial class AddEmployeeHistorySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentHistories_Employees_EmployeeId",
                table: "AssignmentHistories");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "AssignmentHistories",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeName",
                table: "AssignmentHistories",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            // Backfill existing production records with current employee names before finalizing the schema change.
            migrationBuilder.Sql(
                @"UPDATE AssignmentHistories 
                  SET EmployeeName = COALESCE(
                      (SELECT FirstName || ' ' || LastName FROM Employees WHERE Employees.Id = AssignmentHistories.EmployeeId), 
                      'Unknown'
                  ) 
                  WHERE EmployeeId IS NOT NULL;");

            migrationBuilder.UpdateData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 1,
                column: "EmployeeName",
                value: "Anna Kowalska");

            migrationBuilder.UpdateData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 2,
                column: "EmployeeName",
                value: "Marek Nowak");

            migrationBuilder.UpdateData(
                table: "AssignmentHistories",
                keyColumn: "Id",
                keyValue: 3,
                column: "EmployeeName",
                value: "Zofia Wisniewska");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentHistories_Employees_EmployeeId",
                table: "AssignmentHistories",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentHistories_Employees_EmployeeId",
                table: "AssignmentHistories");

            migrationBuilder.DropColumn(
                name: "EmployeeName",
                table: "AssignmentHistories");

            // Purge orphaned records without an employee before re-enforcing NOT NULL constraint to prevent SQLite table rebuild failure.
            migrationBuilder.Sql("DELETE FROM AssignmentHistories WHERE EmployeeId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "AssignmentHistories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentHistories_Employees_EmployeeId",
                table: "AssignmentHistories",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}