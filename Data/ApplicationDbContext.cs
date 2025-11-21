using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DynamicForm.Models.Entities;
using System.Security.Cryptography;
using System.Text;

namespace DynamicForm.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Form> Forms { get; set; }
    public DbSet<FormField> FormFields { get; set; }
    public DbSet<FormSubmission> FormSubmissions { get; set; }
    public DbSet<FormSubmissionValue> FormSubmissionValues { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

            // Create unique indexes
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Seed default SuperAdmin
        SeedDefaultSuperAdmin(modelBuilder);

        // Form entity configuration
        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.FormId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
        });

        // FormField entity configuration
        modelBuilder.Entity<FormField>(entity =>
        {
            entity.HasKey(e => e.FieldId);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);

            entity.HasOne(d => d.Form)
                .WithMany(p => p.FormFields)
                .HasForeignKey(d => d.FormId);
        });

        // FormSubmission entity configuration
        modelBuilder.Entity<FormSubmission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId);
            entity.Property(e => e.SubmittedBy).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.RejectionReasonEn).HasMaxLength(1000);

            entity.HasOne(d => d.Form)
                .WithMany(p => p.FormSubmissions)
                .HasForeignKey(d => d.FormId);
        });

        // FormSubmissionValue entity configuration
        modelBuilder.Entity<FormSubmissionValue>(entity =>
        {
            entity.Property(e => e.FieldNameAtSubmission).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FieldTypeAtSubmission).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LabelAtSubmission).IsRequired().HasMaxLength(200);

            entity.HasOne(d => d.FormSubmission)
                .WithMany(p => p.FormSubmissionValues)
                .HasForeignKey(d => d.SubmissionId);
        });

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

    private void SeedDefaultSuperAdmin(ModelBuilder modelBuilder)
    {
        // Default SuperAdmin credentials
        // Username: Superadmin

        var defaultPassword = "AzcT$nm4saw?qa";
        var passwordHash = HashPassword(defaultPassword);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Username = "Superadmin",
                Email = "Superadmin@dynamicforms.com",
                PasswordHash = passwordHash,
                FullName = "Super Administrator",
                PhoneNumber = null,
                Role = UserRoles.SuperAdmin,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

        return Convert.ToBase64String(hashedBytes);
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
        var citizenshipOptions = new List<string> { "مواطن", "مقيم" };
        var mortgageOptions = new List<string> { "نعم", "لا" };
        var ageOptions = new List<string> { "أكبر من 20 سنة", "أصغر من 20 سنة" };

        // Default form fields with Arabic labels
        modelBuilder.Entity<FormField>().HasData(
            // Original fields
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
                FieldType = "number",
                Label = "العمر",
                IsRequired = true,
                DisplayOrder = 2,
                IsActive = true,
                ValidationRules = JsonSerializer.Serialize(new { min = 1, max = 120, type = "number" })
            },
            new FormField
            {
                FieldId = 3,
                FormId = 1, // ✅ Added missing FormId
                FieldName = "birthDate",
                FieldType = "dropdown", // changed from date to dropdown
                Label = "تاريخ الميلاد",
                IsRequired = true,
                DisplayOrder = 3,
                IsActive = true,
                Options = JsonSerializer.Serialize(ageOptions)
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
                IsActive = true
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
                IsRequired = false,
                DisplayOrder = 8,
                IsActive = true
            },
            new FormField
            {
                FieldId = 9,
                FormId = 1,
                FieldName = "governorate",
                FieldType = "dropdown",
                Label = "المحافظة",
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
                IsRequired = true,
                DisplayOrder = 10,
                IsActive = true,
                Options = JsonSerializer.Serialize(maritalStatusOptions)
            },

            // NEW MANDATORY FIELDS - Added at the end
            new FormField
            {
                FieldId = 11,
                FormId = 1,
                FieldName = "citizenshipStatus",
                FieldType = "dropdown",
                Label = "مواطن أو مقيم",
                IsRequired = true,
                DisplayOrder = 11,
                IsActive = true,
                Options = JsonSerializer.Serialize(citizenshipOptions)
            },
            new FormField
            {
                FieldId = 12,
                FormId = 1,
                FieldName = "hasMortgage",
                FieldType = "dropdown",
                Label = "قرض عقاري",
                IsRequired = true,
                DisplayOrder = 12,
                IsActive = true,
                Options = JsonSerializer.Serialize(mortgageOptions)
            },
            new FormField
            {
                FieldId = 13,
                FormId = 1,
                FieldName = "monthlySalary",
                FieldType = "number",
                Label = "الراتب الشهري",
                IsRequired = true,
                DisplayOrder = 13,
                IsActive = true,
                ValidationRules = JsonSerializer.Serialize(new { type = "number", min = 1, step = 1 })
            },
            new FormField
            {
                FieldId = 14,
                FormId = 1,
                FieldName = "monthlyCommitments",
                FieldType = "number",
                Label = "الالتزامات الشهرية",
                IsRequired = true,
                DisplayOrder = 14,
                IsActive = true,
                ValidationRules = JsonSerializer.Serialize(new { type = "number", min = 0, step = 1 })
            }
        );
    }
}