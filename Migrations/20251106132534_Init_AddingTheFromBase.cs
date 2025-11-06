using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DynamicForm.Migrations
{
    /// <inheritdoc />
    public partial class Init_AddingTheFromBase : Migration
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
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReasonEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
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
                values: new object[] { 1, "النظام", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "نموذج لجمع البيانات الشخصية الأساسية", true, null, "نموذج البيانات الشخصية" });

            migrationBuilder.InsertData(
                table: "FormFields",
                columns: new[] { "FieldId", "DisplayOrder", "FieldName", "FieldType", "FormId", "IsActive", "IsRequired", "Label", "Options", "ValidationRules" },
                values: new object[,]
                {
                    { 1, 1, "fullName", "text", 1, true, true, "الاسم الكامل", null, null },
                    { 2, 2, "age", "text", 1, true, true, "العمر", null, "{\"min\":1,\"max\":120,\"type\":\"number\"}" },
                    { 3, 3, "birthDate", "date", 1, true, true, "تاريخ الميلاد", null, null },
                    { 4, 4, "nationalId", "text", 1, true, true, "رقم الهوية الوطنية", null, null },
                    { 5, 5, "nationalIdType", "dropdown", 1, true, true, "نوع الهوية", "[\"\\u0628\\u0637\\u0627\\u0642\\u0629 \\u0647\\u0648\\u064A\\u0629 \\u0648\\u0637\\u0646\\u064A\\u0629\",\"\\u062C\\u0648\\u0627\\u0632 \\u0633\\u0641\\u0631\",\"\\u0631\\u062E\\u0635\\u0629 \\u0642\\u064A\\u0627\\u062F\\u0629\",\"\\u0628\\u0637\\u0627\\u0642\\u0629 \\u0625\\u0642\\u0627\\u0645\\u0629\"]", null },
                    { 6, 6, "phoneNumber", "text", 1, true, true, "رقم الهاتف", null, null },
                    { 7, 7, "email", "email", 1, true, false, "البريد الإلكتروني", null, null },
                    { 8, 8, "address", "text", 1, true, false, "العنوان", null, null },
                    { 9, 9, "governorate", "dropdown", 1, true, true, "المحافظة", "[\"\\u0627\\u0644\\u0631\\u064A\\u0627\\u0636\",\"\\u0645\\u0643\\u0629 \\u0627\\u0644\\u0645\\u0643\\u0631\\u0645\\u0629\",\"\\u0627\\u0644\\u0645\\u062F\\u064A\\u0646\\u0629 \\u0627\\u0644\\u0645\\u0646\\u0648\\u0631\\u0629\",\"\\u0627\\u0644\\u0642\\u0635\\u064A\\u0645\",\"\\u0627\\u0644\\u0645\\u0646\\u0637\\u0642\\u0629 \\u0627\\u0644\\u0634\\u0631\\u0642\\u064A\\u0629\",\"\\u0639\\u0633\\u064A\\u0631\",\"\\u062A\\u0628\\u0648\\u0643\",\"\\u062D\\u0627\\u0626\\u0644\",\"\\u0627\\u0644\\u062D\\u062F\\u0648\\u062F \\u0627\\u0644\\u0634\\u0645\\u0627\\u0644\\u064A\\u0629\",\"\\u062C\\u0627\\u0632\\u0627\\u0646\",\"\\u0646\\u062C\\u0631\\u0627\\u0646\",\"\\u0627\\u0644\\u0628\\u0627\\u062D\\u0629\",\"\\u0627\\u0644\\u062C\\u0648\\u0641\"]", null },
                    { 10, 10, "maritalStatus", "dropdown", 1, true, true, "الحالة الاجتماعية", "[\"\\u0623\\u0639\\u0632\\u0628\",\"\\u0645\\u062A\\u0632\\u0648\\u062C\",\"\\u0645\\u0637\\u0644\\u0642\",\"\\u0623\\u0631\\u0645\\u0644\"]", null },
                    { 11, 11, "citizenshipStatus", "dropdown", 1, true, true, "مواطن أو مقيم", "[\"\\u0645\\u0648\\u0627\\u0637\\u0646\",\"\\u0645\\u0642\\u064A\\u0645\"]", null },
                    { 12, 12, "hasMortgage", "dropdown", 1, true, true, "قرض عقاري", "[\"\\u0646\\u0639\\u0645\",\"\\u0644\\u0627\"]", null },
                    { 13, 13, "monthlySalary", "text", 1, true, true, "الراتب الشهري", null, "{\"type\":\"number\",\"min\":0}" },
                    { 14, 14, "monthlyCommitments", "text", 1, true, true, "الالتزامات الشهرية", null, "{\"type\":\"number\",\"min\":0}" }
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
