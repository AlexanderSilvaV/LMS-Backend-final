using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LMSBackend.API.Helpers;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using OfficeOpenXml;


// Cargando archivo .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configurar EPPlus licensing
ExcelPackage.License.SetNonCommercialPersonal("Mariano Simonin Fernández");


// Cargar configuración desde appsettings.json y variables de entorno
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Obtener cadena de conexión desde el archivo .env o appsettings
var rawConnectionString = Environment.GetEnvironmentVariable("LMS_DB_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(rawConnectionString))
    throw new InvalidOperationException("La cadena de conexión no se encontró en el archivo .env ni en appsettings.json");

var connectionString = rawConnectionString.Trim(); // Limpiar espacios y caracteres invisibles

// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar Identity con Usuario personalizado y roles
builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Registrar controladores con configuración JSON camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configurar autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key not found in configuration"))),
        RoleClaimType = ClaimTypes.Role
    };
});

// Agregar autorización
builder.Services.AddAuthorization();

// Configurar opciones de 3DLAB
builder.Services.Configure<LMSBackend.API.Configuration.ThreeDLabOptions>(
    builder.Configuration.GetSection("ThreeDLab"));

// Configurar opciones de AWS S3 (desde variables de entorno o appsettings)
builder.Services.Configure<LMSBackend.API.Configuration.AWSOptions>(
    builder.Configuration.GetSection("AWS"));

// Registrar servicios personalizados

// Registrar S3StorageService con sus dependencias
builder.Services.AddScoped<S3StorageService>();
builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<SuscripcionService>();
builder.Services.AddScoped<LecturaService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<HiloService>();
builder.Services.AddScoped<ForoService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CursoService>();
builder.Services.AddScoped<ModuloService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<CursoUsuarioService>();
builder.Services.AddScoped<EvaluacionService>();
builder.Services.AddScoped<ValidacionEvaluacionService>();
builder.Services.AddScoped<BancoPreguntasService>();
builder.Services.AddScoped<Laboratorio3DLabService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LMS Backend API",
        Version = "v1",
        Description = "API para el sistema de gestión de aprendizaje (LMS)"
    });

    // Configurar autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Cargar configuración SMTP (si se usa)
var smtpSettings = builder.Configuration.GetSection("Smtp");

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000", 
            "http://localhost:3001", 
            "http://135.148.148.88:3000", 
            "http://158.69.212.241:3000",
            "https://lms-frontend-hwayctfyahg3hmbz.chilecentral-01.azurewebsites.net"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Inicializar roles y usuario de integración 3DLAB
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Inicializando servicios...");
        await InicializadorDeRoles.InicializarRolesAsync(services);
        logger.LogInformation("Roles inicializados correctamente");

        await ThreeDLabInitializer.EnsureIntegrationUserAsync(services);
        logger.LogInformation("Usuario de integración 3DLAB verificado");

        await ThreeDLabDataInitializer.EnsureCursoYLaboratorioAsync(services);
        logger.LogInformation("Curso y laboratorio 3DLAB verificados");

        logger.LogInformation("Servicios inicializados correctamente");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la inicialización de servicios");
        throw;
    }
}

// Middleware para entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Swagger si estás usando Minimal APIs + Swashbuckle
}

// Configurar pipeline de middleware
app.UseRouting();

// Middleware global de manejo de excepciones
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception in middleware");
        throw;
    }
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Habilitar CORS
app.UseCors("AllowLocalhost3000");

// Habilitar autenticación y autorización main
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

// Mapear endpoints de controladores
app.MapControllers();

// Ejecutar la aplicación
Console.WriteLine("Starting application...");
try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application failed to start: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}
