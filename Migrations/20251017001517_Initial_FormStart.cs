using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DynamicForm.Migrations
{
    /// <inheritdoc />
    public partial class Initial_FormStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Forms",
                columns: table => new
                {
                    FormId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.FormId);
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                columns: table => new
                {
                    FieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Options = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.FieldId);
                    table.ForeignKey(
                        name: "FK_FormFields_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "FormId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_Forms_FormId",
                        column: x => x.FormId,
                        principalTable: "Forms",
                        principalColumn: "FormId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissionValues",
                columns: table => new
                {
                    ValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    FieldValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FieldNameAtSubmission = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldTypeAtSubmission = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LabelAtSubmission = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OptionsAtSubmission = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissionValues", x => x.ValueId);
                    table.ForeignKey(
                        name: "FK_FormSubmissionValues_FormFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "FormFields",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmissionValues_FormSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "FormSubmissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Forms",
                columns: new[] { "FormId", "CreatedBy", "CreatedDate", "Description", "IsActive", "ModifiedDate", "Name" },
                values: new object[] { 1, "النظام", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "الحقول الإلزامية", true, null, "نموذج البيانات الأساسية" });

            migrationBuilder.InsertData(
                table: "FormFields",
                columns: new[] { "FieldId", "DisplayOrder", "FieldName", "FieldType", "FormId", "IsActive", "IsRequired", "Label", "Options", "ValidationRules" },
                values: new object[,]
                {
                    { 1, 1, "id", "text", 1, true, true, "المعرف", null, "{\"type\":\"number\",\"readOnly\":true}" },
                    { 2, 2, "referenceNo", "text", 1, true, true, "رقم المرجع", null, null },
                    { 3, 3, "customerName", "text", 1, true, true, "اسم العميل", null, null },
                    { 4, 4, "phoneNumber", "text", 1, true, true, "رقم الهاتف", null, "{\"pattern\":\"^[0-9\\u002B\\\\-\\\\s]\\u002B$\"}" },
                    { 5, 5, "salary", "text", 1, true, true, "الراتب", null, "{\"type\":\"number\",\"min\":0}" },
                    { 6, 6, "monthlySpent", "text", 1, true, true, "الالتزامات الشهريه", null, "{\"type\":\"number\",\"min\":0}" },
                    { 7, 7, "status", "dropdown", 1, true, true, "الحالة", "[\"\\u062C\\u062F\\u064A\\u062F\",\"\\u0642\\u064A\\u062F \\u0627\\u0644\\u0645\\u0631\\u0627\\u062C\\u0639\\u0629\",\"\\u0645\\u0642\\u0628\\u0648\\u0644\",\"\\u0645\\u0631\\u0641\\u0648\\u0636\",\"\\u0645\\u0643\\u062A\\u0645\\u0644\"]", null },
                    { 8, 8, "creationDate", "date", 1, true, true, "تاريخ الإنشاء", null, "{\"readOnly\":true}" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormId",
                table: "FormFields",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormId",
                table: "FormSubmissions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionValues_FieldId",
                table: "FormSubmissionValues",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionValues_SubmissionId",
                table: "FormSubmissionValues",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormSubmissionValues");

            migrationBuilder.DropTable(
                name: "FormFields");

            migrationBuilder.DropTable(
                name: "FormSubmissions");

            migrationBuilder.DropTable(
                name: "Forms");
        }
    }
}
