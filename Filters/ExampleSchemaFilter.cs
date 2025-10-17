using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
using Microsoft.OpenApi.Any;

namespace DynamicForm.Filters
{
    public class ExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.Name == "CreateFormDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["name"] = new OpenApiString("نموذج طلب قرض شخصي"),
                    ["description"] = new OpenApiString("نموذج لتقديم طلب قرض شخصي - يحتوي على الحقول الإلزامية تلقائياً"),
                    ["fields"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("jobTitle"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("المسمى الوظيفي"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(1)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("employmentType"),
                            ["fieldType"] = new OpenApiString("dropdown"),
                            ["label"] = new OpenApiString("نوع التوظيف"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["options"] = new OpenApiArray
                            {
                                new OpenApiString("موظف حكومي"),
                                new OpenApiString("موظف قطاع خاص"),
                                new OpenApiString("أعمال حرة"),
                                new OpenApiString("متقاعد")
                            },
                            ["displayOrder"] = new OpenApiInteger(2)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("requestedAmount"),
                            ["fieldType"] = new OpenApiString("number"),
                            ["label"] = new OpenApiString("المبلغ المطلوب"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(3)
                        }
                    }
                };
            }
            else if (context.Type.Name == "SubmitFormDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["submittedBy"] = new OpenApiString("أحمد محمد السعيد"),
                    ["values"] = new OpenApiObject
                    {
                        // Mandatory fields examples (these will be auto-generated if not provided)
                        ["customerName"] = new OpenApiString("أحمد محمد السعيد"),
                        ["phoneNumber"] = new OpenApiString("+966501234567"),
                        ["salary"] = new OpenApiString("12000"),
                        ["monthlySpent"] = new OpenApiString("4500"),
                        // Custom fields examples
                        ["jobTitle"] = new OpenApiString("مهندس برمجيات"),
                        ["employmentType"] = new OpenApiString("موظف قطاع خاص"),
                        ["requestedAmount"] = new OpenApiString("50000")
                    }
                };
            }
            else if (context.Type.Name == "UpdateFormDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["name"] = new OpenApiString("نموذج طلب قرض شخصي محدث"),
                    ["description"] = new OpenApiString("نموذج محدث لتقديم طلب قرض شخصي"),
                    ["fields"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("jobTitle"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("المسمى الوظيفي المحدث"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(1)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("workExperience"),
                            ["fieldType"] = new OpenApiString("dropdown"),
                            ["label"] = new OpenApiString("سنوات الخبرة"),
                            ["isRequired"] = new OpenApiBoolean(false),
                            ["options"] = new OpenApiArray
                            {
                                new OpenApiString("أقل من سنة"),
                                new OpenApiString("1-3 سنوات"),
                                new OpenApiString("4-7 سنوات"),
                                new OpenApiString("أكثر من 7 سنوات")
                            },
                            ["displayOrder"] = new OpenApiInteger(2)
                        }
                    }
                };
            }
            else if (context.Type.Name == "UpdateStatusDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["status"] = new OpenApiString("قيد المراجعة")
                };
            }
            else if (context.Type.Name == "FormDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["formId"] = new OpenApiInteger(1),
                    ["name"] = new OpenApiString("نموذج طلب قرض شخصي"),
                    ["description"] = new OpenApiString("نموذج لتقديم طلب قرض شخصي"),
                    ["isActive"] = new OpenApiBoolean(true),
                    ["createdDate"] = new OpenApiString("2024-10-16T10:30:00Z"),
                    ["fields"] = new OpenApiArray
                    {
                        // Mandatory fields (automatically included)
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(1),
                            ["fieldName"] = new OpenApiString("id"),
                            ["fieldType"] = new OpenApiString("number"),
                            ["label"] = new OpenApiString("المعرف"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(1)
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(2),
                            ["fieldName"] = new OpenApiString("referenceNo"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("رقم المرجع"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(2)
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(3),
                            ["fieldName"] = new OpenApiString("customerName"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("اسم العميل"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(3)
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(4),
                            ["fieldName"] = new OpenApiString("phoneNumber"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("رقم الهاتف"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(4)
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(5),
                            ["fieldName"] = new OpenApiString("salary"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("الراتب"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(5)
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(6),
                            ["fieldName"] = new OpenApiString("monthlySpent"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("الالتزامات الشهريه"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(6)
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(7),
                            ["fieldName"] = new OpenApiString("status"),
                            ["fieldType"] = new OpenApiString("dropdown"),
                            ["label"] = new OpenApiString("الحالة"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(7),
                            ["options"] = new OpenApiArray
                            {
                                new OpenApiString("جديد"),
                                new OpenApiString("قيد المراجعة"),
                                new OpenApiString("مقبول"),
                                new OpenApiString("مرفوض"),
                                new OpenApiString("مكتمل")
                            }
                        },
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(8),
                            ["fieldName"] = new OpenApiString("creationDate"),
                            ["fieldType"] = new OpenApiString("date"),
                            ["label"] = new OpenApiString("تاريخ الإنشاء"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(8)
                        },
                        // Custom field example
                        new OpenApiObject
                        {
                            ["fieldId"] = new OpenApiInteger(9),
                            ["fieldName"] = new OpenApiString("jobTitle"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("المسمى الوظيفي"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(9)
                        }
                    }
                };
            }
            else if (context.Type.Name == "FormSubmissionSummaryDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["submissionId"] = new OpenApiInteger(12345),
                    ["formId"] = new OpenApiInteger(1),
                    ["formName"] = new OpenApiString("نموذج طلب قرض شخصي"),
                    ["submittedDate"] = new OpenApiString("2024-10-16T14:30:00Z"),
                    ["status"] = new OpenApiString("قيد المراجعة"),
                    ["submittedBy"] = new OpenApiString("أحمد محمد السعيد"),
                    // Mandatory fields as direct properties
                    ["id"] = new OpenApiString("12345"),
                    ["referenceNo"] = new OpenApiString("REF-20241016-012345"),
                    ["customerName"] = new OpenApiString("أحمد محمد السعيد"),
                    ["phoneNumber"] = new OpenApiString("+966501234567"),
                    ["salary"] = new OpenApiString("12000"),
                    ["monthlySpent"] = new OpenApiString("4500"),
                    ["formStatus"] = new OpenApiString("قيد المراجعة"),
                    ["creationDate"] = new OpenApiString("2024-10-16"),
                    ["preview"] = new OpenApiString("أحمد محمد السعيد - المرجع: REF-20241016-012345")
                };
            }
            else if (context.Type.Name == "FormSubmissionResponseDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["submissionId"] = new OpenApiInteger(12345),
                    ["formName"] = new OpenApiString("نموذج طلب قرض شخصي"),
                    ["submittedDate"] = new OpenApiString("2024-10-16T14:30:00Z"),
                    ["status"] = new OpenApiString("قيد المراجعة"),
                    ["submittedBy"] = new OpenApiString("أحمد محمد السعيد"),
                    ["values"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("id"),
                            ["label"] = new OpenApiString("المعرف"),
                            ["value"] = new OpenApiString("12345"),
                            ["fieldType"] = new OpenApiString("number")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("referenceNo"),
                            ["label"] = new OpenApiString("رقم المرجع"),
                            ["value"] = new OpenApiString("REF-20241016-012345"),
                            ["fieldType"] = new OpenApiString("text")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("customerName"),
                            ["label"] = new OpenApiString("اسم العميل"),
                            ["value"] = new OpenApiString("أحمد محمد السعيد"),
                            ["fieldType"] = new OpenApiString("text")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("phoneNumber"),
                            ["label"] = new OpenApiString("رقم الهاتف"),
                            ["value"] = new OpenApiString("+966501234567"),
                            ["fieldType"] = new OpenApiString("text")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("salary"),
                            ["label"] = new OpenApiString("الراتب"),
                            ["value"] = new OpenApiString("12000"),
                            ["fieldType"] = new OpenApiString("text")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("monthlySpent"),
                            ["label"] = new OpenApiString("الالتزامات الشهريه"),
                            ["value"] = new OpenApiString("4500"),
                            ["fieldType"] = new OpenApiString("text")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("status"),
                            ["label"] = new OpenApiString("الحالة"),
                            ["value"] = new OpenApiString("قيد المراجعة"),
                            ["fieldType"] = new OpenApiString("dropdown")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("creationDate"),
                            ["label"] = new OpenApiString("تاريخ الإنشاء"),
                            ["value"] = new OpenApiString("2024-10-16"),
                            ["fieldType"] = new OpenApiString("date")
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("jobTitle"),
                            ["label"] = new OpenApiString("المسمى الوظيفي"),
                            ["value"] = new OpenApiString("مهندس برمجيات"),
                            ["fieldType"] = new OpenApiString("text")
                        }
                    }
                };
            }
            else if (context.Type.Name == "UpdateFormFieldDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["label"] = new OpenApiString("المسمى الوظيفي المحدث"),
                    ["isRequired"] = new OpenApiBoolean(true),
                    ["displayOrder"] = new OpenApiInteger(1),
                    ["options"] = new OpenApiArray
                    {
                        new OpenApiString("مطور أول"),
                        new OpenApiString("مطور متوسط"),
                        new OpenApiString("مطور مبتدئ")
                    },
                    ["validationRules"] = new OpenApiString("{\"minLength\":2,\"maxLength\":50}")
                };
            }
        }
    }
}