using DynamicForm.BLL.Contracts;
using DynamicForm.BLL.DTOs.Filters;
using DynamicForm.BLL.Services;
using DynamicForm.DAL;
using DynamicForm.DAL.Models.Configuration;
using DynamicForm.DAL.Models.Entities;
using DynamicForm.Middleware;
using DynamicForm.Middleware.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Cache Service 
builder.Services.AddMemoryCache();

// Add HttpClient for WhatsApp service
builder.Services.AddHttpClient<WhatsAppService>();

// ✅ Configure Email Settings with validation
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

// Validate email settings on startup
builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection(EmailSettings.SectionName))
    .ValidateOnStart();

// Add Authentication Services with Custom Events
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"))),
            ClockSkew = TimeSpan.Zero
        };

        // Add custom authentication events
        options.Events = new JwtAuthenticationEvents();
    });

// Add custom authorization handler
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddlewareResultHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, DepartmentAuthorizationHandler>(); // ✅ NEW

builder.Services.AddAuthorization(options =>
{
    // ✅ Role-based policies
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole(UserRoles.SuperAdmin));
    options.AddPolicy("AdminOrAbove", policy => policy.RequireRole(UserRoles.SuperAdmin, UserRoles.Admin));
    options.AddPolicy("EmployeeOrAbove", policy => policy.RequireRole(UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.Employee));

    // ✅ Department-based policies
    options.AddPolicy("SalesOnly", policy =>
        policy.Requirements.Add(new DepartmentRequirement(Departments.Sales)));

    options.AddPolicy("MarketingOnly", policy =>
        policy.Requirements.Add(new DepartmentRequirement(Departments.Marketing)));

    options.AddPolicy("SalesOrMarketing", policy =>
        policy.Requirements.Add(new DepartmentRequirement(Departments.Sales, Departments.Marketing)));
});

// Add services
builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IFieldValidationService, FieldValidationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRejectionAnalyticsService, RejectionAnalyticsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRejectionReasonCatalogService, RejectionReasonCatalogService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Dynamic Forms API",
        Version = "v1",
        Description = @"Dynamic Form API with WhatsApp Integration via Morasalaty and JWT Authentication",
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

    // Add JWT Bearer security definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true)
            .WithExposedHeaders();
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
    } catch (Exception ex)
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

// Enable static files to serve wwwroot content (including your custom CSS)
app.UseStaticFiles();

// Configure Swagger for ALL environments
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamic Forms API v1");
    c.DocumentTitle = "Dynamic Forms API";
    c.RoutePrefix = "swagger";
    c.InjectStylesheet("/swagger-ui/custom.css");
});

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication(); // Added
app.UseAuthorization();
app.MapControllers();

app.Run();