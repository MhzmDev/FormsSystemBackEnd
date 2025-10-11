using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynamicForm.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldSnapshotToSubmissionValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FieldNameAtSubmission",
                table: "FormSubmissionValues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FieldTypeAtSubmission",
                table: "FormSubmissionValues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabelAtSubmission",
                table: "FormSubmissionValues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OptionsAtSubmission",
                table: "FormSubmissionValues",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Forms",
                keyColumn: "FormId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldNameAtSubmission",
                table: "FormSubmissionValues");

            migrationBuilder.DropColumn(
                name: "FieldTypeAtSubmission",
                table: "FormSubmissionValues");

            migrationBuilder.DropColumn(
                name: "LabelAtSubmission",
                table: "FormSubmissionValues");

            migrationBuilder.DropColumn(
                name: "OptionsAtSubmission",
                table: "FormSubmissionValues");

            migrationBuilder.UpdateData(
                table: "Forms",
                keyColumn: "FormId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 10, 10, 13, 14, 7, 562, DateTimeKind.Utc).AddTicks(2326));
        }
    }
}
