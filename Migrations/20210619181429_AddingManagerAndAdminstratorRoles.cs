using Microsoft.EntityFrameworkCore.Migrations;

namespace CompanyEmployee.Migrations
{
    public partial class AddingManagerAndAdminstratorRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "ce569400-af1b-4707-a930-b2ad2b1fc2a3", "09f9c2e8-9df4-4fac-9441-074e4889d5ed", "Manager", "MANAGER" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "ca56accb-d102-4c5d-88fe-21f5261b1ef3", "02bd31c1-f0b9-4fa7-ba98-5242465b9dd9", "Administrator", "ADMINISTRATOR" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ca56accb-d102-4c5d-88fe-21f5261b1ef3");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ce569400-af1b-4707-a930-b2ad2b1fc2a3");
        }
    }
}
