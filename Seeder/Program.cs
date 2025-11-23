using LMSBackend.API.Data;
using LMSBackend.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LMSBackend.API"));

var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Connection string DefaultConnection not found in appsettings.json");
    return;
}

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
}));
services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

await using var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

try
{
    var curso = await context.Cursos
        .AsNoTracking()
        .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Nombre, "%3dlab%"));

    if (curso is null)
    {
        var cursosDisponibles = await context.Cursos.AsNoTracking().Select(c => new { c.Nrc, c.Nombre }).ToListAsync();
        Console.WriteLine("Curso 3DLAB no encontrado. Cursos disponibles:");
        foreach (var item in cursosDisponibles)
        {
            Console.WriteLine($" - {item.Nrc}: {item.Nombre}");
        }
        return;
    }

    var docente = await context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Rol == "Docente");

    if (docente is null)
    {
        Console.WriteLine("No se encontró un usuario con rol Docente.");
        return;
    }

    var existingLabs = await context.Evaluaciones
        .Where(e => e.CursoId == curso.Nrc && e.EsLaboratorio3DLab)
        .Select(e => e.Titulo)
        .ToListAsync();

    if (existingLabs.Count >= 2)
    {
        Console.WriteLine("Ya existen laboratorios 3DLAB en este curso:");
        foreach (var titulo in existingLabs)
        {
            Console.WriteLine($" - {titulo}");
        }
        return;
    }

    var labsToCreate = new[]
    {
        (Titulo: "Laboratorio Virtual 3DLAB - Seguridad", Descripcion: "Practica guiada de seguridad en laboratorio quimico."),
        (Titulo: "Laboratorio Virtual 3DLAB - Reactivos", Descripcion: "Clasificacion y manejo de reactivos en 3DLAB.")
    };

    foreach (var (titulo, descripcion) in labsToCreate)
    {
        if (existingLabs.Contains(titulo))
        {
            logger.LogInformation("Laboratorio {Titulo} ya existe, se omite.", titulo);
            continue;
        }

        var evaluacion = new Evaluacion
        {
            Titulo = titulo,
            Descripcion = descripcion,
            CursoId = curso.Nrc,
            DocenteId = docente.Id,
            FechaCreacion = DateTime.UtcNow,
            TiempoLimiteMins = 0,
            Activa = true,
            IntentosMaximos = 0,
            EsLaboratorio3DLab = true,
            PreguntasMinimasLaboratorio = 12,
            PreguntasPorSesionLaboratorio = 4
        };

        var preguntas = Enumerable.Range(1, 12).Select(index =>
        {
            var pregunta = new Pregunta
            {
                Texto = $"Pregunta {index}: Escenario {index} en {titulo}",
                Orden = index,
                Puntos = 1,
                Evaluacion = evaluacion
            };

            pregunta.Opciones = new List<Opcion>
            {
                new() { Texto = "Opcion correcta", EsCorrecta = true, Orden = 1, Pregunta = pregunta },
                new() { Texto = "Opcion distractor 1", EsCorrecta = false, Orden = 2, Pregunta = pregunta },
                new() { Texto = "Opcion distractor 2", EsCorrecta = false, Orden = 3, Pregunta = pregunta },
                new() { Texto = "Opcion distractor 3", EsCorrecta = false, Orden = 4, Pregunta = pregunta }
            };

            return pregunta;
        }).ToList();

        evaluacion.Preguntas = preguntas;

        await context.Evaluaciones.AddAsync(evaluacion);
        await context.Preguntas.AddRangeAsync(preguntas);
        await context.Opciones.AddRangeAsync(preguntas.SelectMany(p => p.Opciones));

        await context.SaveChangesAsync();
        Console.WriteLine($"Laboratorio creado: {titulo}");
    }

    Console.WriteLine("Operacion completada.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error al crear laboratorios de prueba");
    Console.WriteLine($"Error: {ex.Message}");
}
