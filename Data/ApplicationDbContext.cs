using Microsoft.EntityFrameworkCore;
using DynamicForm.Models;
using System.Text.Json;

namespace DynamicForm.Data
{
    public class ApplicationDbContext : DbContext
    {
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

            // Seed default Arabic form
            SeedDefaultArabicForm(modelBuilder);
        }

        private void SeedDefaultArabicForm(ModelBuilder modelBuilder)
        {
            // Default form - using fixed date for seeding
            modelBuilder.Entity<Form>().HasData(
                new Form
                {
                    FormId = 1,
                    Name = "نموذج البيانات الشخصية",
                    Description = "نموذج لجمع البيانات الشخصية الأساسية",
                    IsActive = true,
                    CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = "النظام"
                }
            );

            // National ID type options
            var nationalIdOptions = new List<string> 
            { 
                "بطاقة هوية وطنية", 
                "جواز سفر", 
                "رخصة قيادة", 
                "بطاقة إقامة" 
            };

            // Governorate options (example for Saudi Arabia)
            var governorateOptions = new List<string>
            {
                "الرياض", "مكة المكرمة", "المدينة المنورة", "القصيم", "المنطقة الشرقية",
                "عسير", "تبوك", "حائل", "الحدود الشمالية", "جازان", "نجران", "الباحة", "الجوف"
            };

            var maritalStatusOptions = new List<string> { "أعزب", "متزوج", "مطلق", "أرمل" };

            // Default form fields with Arabic labels
            modelBuilder.Entity<FormField>().HasData(
                new FormField
                {
                    FieldId = 1,
                    FormId = 1,
                    FieldName = "fullName",
                    FieldType = "text",
                    Label = "الاسم الكامل",
                    IsRequired = true,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 2,
                    FormId = 1,
                    FieldName = "age",
                    FieldType = "text",
                    Label = "العمر",
                    IsRequired = true,
                    DisplayOrder = 2,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { min = 1, max = 120, type = "number" })
                },
                new FormField
                {
                    FieldId = 3,
                    FormId = 1,
                    FieldName = "birthDate",
                    FieldType = "date",
                    Label = "تاريخ الميلاد",
                    IsRequired = true,
                    DisplayOrder = 3,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 4,
                    FormId = 1,
                    FieldName = "nationalId",
                    FieldType = "text",
                    Label = "رقم الهوية الوطنية",
                    IsRequired = true,
                    DisplayOrder = 4,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 5,
                    FormId = 1,
                    FieldName = "nationalIdType",
                    FieldType = "dropdown",
                    Label = "نوع الهوية",
                    IsRequired = true,
                    DisplayOrder = 5,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(nationalIdOptions)
                },
                new FormField
                {
                    FieldId = 6,
                    FormId = 1,
                    FieldName = "phoneNumber",
                    FieldType = "text",
                    Label = "رقم الهاتف",
                    IsRequired = true,
                    DisplayOrder = 6,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { pattern = "^[0-9+\\-\\s]+$" })
                },
                new FormField
                {
                    FieldId = 7,
                    FormId = 1,
                    FieldName = "email",
                    FieldType = "email",
                    Label = "البريد الإلكتروني",
                    IsRequired = false,
                    DisplayOrder = 7,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 8,
                    FormId = 1,
                    FieldName = "address",
                    FieldType = "text",
                    Label = "العنوان",
                    IsRequired = true,
                    DisplayOrder = 8,
                    IsActive = true
                },
                new FormField
                {
                    FieldId = 9,
                    FormId = 1,
                    FieldName = "governorate",
                    FieldType = "dropdown",
                    Label = "المنطقة/المحافظة",
                    IsRequired = true,
                    DisplayOrder = 9,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(governorateOptions)
                },
                new FormField
                {
                    FieldId = 10,
                    FormId = 1,
                    FieldName = "maritalStatus",
                    FieldType = "dropdown",
                    Label = "الحالة الاجتماعية",
                    IsRequired = false,
                    DisplayOrder = 10,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(maritalStatusOptions)
                }
            );
        }
    }
}