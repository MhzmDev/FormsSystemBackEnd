using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
using Microsoft.OpenApi.Any;

namespace DynamicForm.BLL.DTOs.Filters
{
    public class ExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.Name == "CreateFormDto")
            {
                // add schema description in English
                schema.Description = "A form for creating a new Form (job application)";

                schema.Example = new OpenApiObject
                {
                    ["name"] = new OpenApiString("نموذج طلب وظيفة"),
                    ["description"] = new OpenApiString("نموذج لتقديم طلب وظيفة جديدة"),
                    ["fields"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("fullName"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("الاسم الثلاثي"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(1)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("phoneNumber"),
                            ["fieldType"] = new OpenApiString("number"),
                            ["label"] = new OpenApiString("رقم الجوال"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(2)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("birthDate"),
                            ["fieldType"] = new OpenApiString("date"),
                            ["label"] = new OpenApiString("تاريخ الميلاد"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(3)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("position"),
                            ["fieldType"] = new OpenApiString("dropdown"),
                            ["label"] = new OpenApiString("المنصب المطلوب"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["options"] = new OpenApiArray
                            {
                                new OpenApiString("مطور برمجيات"),
                                new OpenApiString("مصمم واجهات"),
                                new OpenApiString("مدير مشروع")
                            },
                            ["displayOrder"] = new OpenApiInteger(4)
                        },
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("monthlySalary"),
                            ["fieldType"] = new OpenApiString("number"),
                            ["label"] = new OpenApiString("الراتب الشهري"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(5),
                            ["validationRules"] = new OpenApiObject
                            {
                                ["operator"] = new OpenApiString(">="),
                                ["validValue"] = new OpenApiString("3000"),
                                ["isValid"] = new OpenApiBoolean(true),
                                ["errorMessageAr"] = new OpenApiString("الراتب يجب أن يكون 3000 ريال أو أكثر"),
                                ["errorMessageEn"] = new OpenApiString("Salary must be 3000 SAR or more")
                            }
                        }
                    }
                };
            }
            else if (context.Type.Name == "SubmitFormDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["submittedBy"] = new OpenApiString("أحمد محمد علي"),
                    ["values"] = new OpenApiObject
                    {
                        ["fullName"] = new OpenApiString("أحمد محمد علي السعيد"),
                        ["age"] = new OpenApiString("28"),
                        ["birthDate"] = new OpenApiString("1996-03-15"),
                        ["nationalId"] = new OpenApiString("1234567890"),
                        ["nationalIdType"] = new OpenApiString("بطاقة هوية وطنية"),
                        ["phoneNumber"] = new OpenApiString("+201011744192"),
                        ["email"] = new OpenApiString("ahmed.mohammed@gmail.com"),
                        ["address"] = new OpenApiString("شارع الملك فهد، حي الملز، الرياض"),
                        ["governorate"] = new OpenApiString("الرياض"),
                        ["maritalStatus"] = new OpenApiString("متزوج"),
                        ["citizenshipStatus"] = new OpenApiString("مواطن"),
                        ["hasMortgage"] = new OpenApiString("نعم"),
                        ["monthlySalary"] = new OpenApiString("15000"),
                        ["monthlyCommitments"] = new OpenApiString("3000")
                    }
                };
            }
            else if (context.Type.Name == "UpdateStatusDto")
            {
                schema.Example = new OpenApiObject
                {
                    ["status"] = new OpenApiString("مقبول")
                };
            }
        }
    }
}