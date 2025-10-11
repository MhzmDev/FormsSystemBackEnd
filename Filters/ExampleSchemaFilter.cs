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
                    ["name"] = new OpenApiString("نموذج طلب وظيفة"),
                    ["description"] = new OpenApiString("نموذج لتقديم طلب وظيفة جديدة"),
                    ["fields"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["fieldName"] = new OpenApiString("applicantName"),
                            ["fieldType"] = new OpenApiString("text"),
                            ["label"] = new OpenApiString("اسم المتقدم"),
                            ["isRequired"] = new OpenApiBoolean(true),
                            ["displayOrder"] = new OpenApiInteger(1)
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
                            ["displayOrder"] = new OpenApiInteger(2)
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
                        ["phoneNumber"] = new OpenApiString("+966501234567"),
                        ["email"] = new OpenApiString("ahmed.mohammed@gmail.com"),
                        ["address"] = new OpenApiString("شارع الملك فهد، حي الملز، الرياض"),
                        ["governorate"] = new OpenApiString("الرياض"),
                        ["maritalStatus"] = new OpenApiString("متزوج")
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
        }
    }
}