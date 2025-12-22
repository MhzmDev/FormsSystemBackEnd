using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynamicForm.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthenticationWithRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 2,
                column: "FieldType",
                value: "number");

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 3,
                columns: new[] { "FieldType", "Options" },
                values: new object[] { "dropdown", "[\"\\u0623\\u0643\\u0628\\u0631 \\u0645\\u0646 20 \\u0633\\u0646\\u0629\",\"\\u0623\\u0635\\u063A\\u0631 \\u0645\\u0646 20 \\u0633\\u0646\\u0629\"]" });

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 13,
                columns: new[] { "FieldType", "ValidationRules" },
                values: new object[] { "number", "{\"type\":\"number\",\"min\":1,\"step\":1}" });

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 14,
                columns: new[] { "FieldType", "ValidationRules" },
                values: new object[] { "number", "{\"type\":\"number\",\"min\":0,\"step\":1}" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedDate", "Email", "FullName", "IsActive", "IsDeleted", "LastLoginDate", "ModifiedDate", "PasswordHash", "PhoneNumber", "RefreshToken", "RefreshTokenExpiryTime", "Role", "Username" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Superadmin@dynamicforms.com", "Super Administrator", true, false, null, null, "SSvEzGlP9vabR0GPkY154vlgGJjZ8cyTWN/gQbFQ/lY=", null, null, null, "SuperAdmin", "Superadmin" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 2,
                column: "FieldType",
                value: "text");

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 3,
                columns: new[] { "FieldType", "Options" },
                values: new object[] { "date", null });

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 13,
                columns: new[] { "FieldType", "ValidationRules" },
                values: new object[] { "text", "{\"type\":\"number\",\"min\":0}" });

            migrationBuilder.UpdateData(
                table: "FormFields",
                keyColumn: "FieldId",
                keyValue: 14,
                columns: new[] { "FieldType", "ValidationRules" },
                values: new object[] { "text", "{\"type\":\"number\",\"min\":0}" });
        }
    }
}
