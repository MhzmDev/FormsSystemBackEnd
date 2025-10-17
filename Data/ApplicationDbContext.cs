using Microsoft.EntityFrameworkCore;
using DynamicForm.Models;
using System.Text.Json;

namespace DynamicForm.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Helper method to get mandatory field names
        public static readonly List<string> MandatoryFields = new List<string>
        {
            "id", "referenceNo", "customerName", "phoneNumber",
            "salary", "monthlySpent", "status", "creationDate"
        };

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Form> Forms { get; set; }
        public DbSet<FormField> FormFields { get; set; }
        public DbSet<FormSubmission> FormSubmissions { get; set; }
        public DbSet<FormSubmissionValue> FormSubmissionValues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<FormField>()
                .HasOne(f => f.Form)
                .WithMany(f => f.FormFields)
                .HasForeignKey(f => f.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FormSubmission>()
                .HasOne(s => s.Form)
                .WithMany(f => f.FormSubmissions)
                .HasForeignKey(s => s.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FormSubmissionValue>()
                .HasOne(v => v.FormSubmission)
                .WithMany(s => s.FormSubmissionValues)
                .HasForeignKey(v => v.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FormSubmissionValue>()
                .HasOne(v => v.FormField)
                .WithMany(f => f.FormSubmissionValues)
                .HasForeignKey(v => v.FieldId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FormSubmissionValue with new fields
            modelBuilder.Entity<FormSubmissionValue>()
                .Property(v => v.LabelAtSubmission)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<FormSubmissionValue>()
                .Property(v => v.FieldNameAtSubmission)
                .HasMaxLength(100)
                .IsRequired();

            // Seed default form with mandatory fields
            SeedDefaultForm(modelBuilder);
        }

        private void SeedDefaultForm(ModelBuilder modelBuilder)
        {
            // Default form - using fixed date for seeding
            modelBuilder.Entity<Form>().HasData(
                new Form
                {
                    FormId = 1,
                    Name = "نموذج البيانات الأساسية",
                    Description = "الحقول الإلزامية",
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "النظام"
                }
            );

            // Status options
            var statusOptions = new List<string>
            {
                "جديد",
                "قيد المراجعة",
                "مقبول",
                "مرفوض",
                "مكتمل"
            };

            // Default mandatory form fields - these cannot be changed
            modelBuilder.Entity<FormField>().HasData(
                new FormField
                {
                    FieldId = 1,
                    FormId = 1,
                    FieldName = "id",
                    FieldType = "text",
                    Label = "المعرف",
                    IsRequired = true,
                    DisplayOrder = 1,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { type = "number", readOnly = true })
                },
                new FormField
                {
                    FieldId = 2,
                    FormId = 1,
                    FieldName = "referenceNo",
                    FieldType = "text",
                    Label = "رقم المرجع",
                    IsRequired = true,
                    DisplayOrder = 2,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 3,
                    FormId = 1,
                    FieldName = "customerName",
                    FieldType = "text",
                    Label = "اسم العميل",
                    IsRequired = true,
                    DisplayOrder = 3,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 4,
                    FormId = 1,
                    FieldName = "phoneNumber",
                    FieldType = "text",
                    Label = "رقم الهاتف",
                    IsRequired = true,
                    DisplayOrder = 4,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { pattern = "^[0-9+\\-\\s]+$" })
                },
                new FormField
                {
                    FieldId = 5,
                    FormId = 1,
                    FieldName = "salary",
                    FieldType = "text",
                    Label = "الراتب",
                    IsRequired = true,
                    DisplayOrder = 5,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { type = "number", min = 0 })
                },
                new FormField
                {
                    FieldId = 6,
                    FormId = 1,
                    FieldName = "monthlySpent",
                    FieldType = "text",
                    Label = "الالتزامات الشهريه",
                    IsRequired = true,
                    DisplayOrder = 6,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { type = "number", min = 0 })
                },
                new FormField
                {
                    FieldId = 7,
                    FormId = 1,
                    FieldName = "status",
                    FieldType = "dropdown",
                    Label = "الحالة",
                    IsRequired = true,
                    DisplayOrder = 7,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(statusOptions)
                },
                new FormField
                {
                    FieldId = 8,
                    FormId = 1,
                    FieldName = "creationDate",
                    FieldType = "date",
                    Label = "تاريخ الإنشاء",
                    IsRequired = true,
                    DisplayOrder = 8,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { readOnly = true })
                }
            );
        }
    }
}