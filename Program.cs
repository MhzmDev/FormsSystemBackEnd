using Microsoft.EntityFrameworkCore;
using DynamicForm.Data;
using DynamicForm.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
using DynamicForm.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Dynamic Forms API V2",
        Version = "v2",
        Description = @"Dynamic Form API, Please refer to Documentation for any inquiries.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "KadrySoftwareSolutions@gmail.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // include XML comments (enable in .csproj first)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add example schemas
    c.SchemaFilter<ExampleSchemaFilter>();

    // Group endpoints by tags
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);

    // Add security definition (for future JWT implementation)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting database migration...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully.");

        var formCount = await context.Forms.CountAsync();
        logger.LogInformation($"Database contains {formCount} forms after migration.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");

        if (app.Environment.IsProduction())
        {
            logger.LogCritical("Application startup failed due to database migration error in production environment.");

            throw;
        }
        else
        {
            logger.LogWarning("Database migration failed in development environment. Continuing startup...");
        }
    }
}

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();

//    app.UseSwaggerUI(c =>
//    {
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamic Forms API v1");
//        c.DocumentTitle = "Dynamic Forms API";
//        c.RoutePrefix = "swagger"; // Available at /swagger
//    });
//}

// Enable static files to serve wwwroot content (including your custom CSS)
app.UseStaticFiles();

// Configure Swagger for ALL environments
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamic Forms API v1");
    c.DocumentTitle = "Dynamic Forms API V2";
    c.RoutePrefix = "swagger"; // Available at /swagger
    c.InjectStylesheet("/swagger-ui/custom.css"); // Use your custom CSS
});
 
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();