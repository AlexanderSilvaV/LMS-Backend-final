using LMSBackend.API.Data;
using LMSBackend.API.Models;
using LMSBackend.API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMSBackend.Tests;

public class Laboratorio3DLabServiceTests
{
    [Fact]
    public async Task CrearSesionYSeleccionarPreguntasAsync_RespetaPreguntasPorSesion()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var docente = new Usuario
        {
            Id = "doc-1",
            UserName = "docente",
            Email = "docente@example.com",
            Nombre = "Docente",
            Rut = "11111111-1",
            Rol = "Docente"
        };

        var administrador = docente;

        var curso = new Curso
        {
            Nrc = 1001,
            Nombre = "Curso de Laboratorio",
            Descripcion = "Curso de pruebas",
            Activo = true,
            AdministradorId = administrador.Id,
            Administrador = administrador,
            CursoUsuarios = new List<CursoUsuario>()
        };

        var evaluacion = new Evaluacion
        {
            Titulo = "Laboratorio 3DLAB",
            CursoId = curso.Nrc,
            Curso = curso,
            DocenteId = docente.Id,
            Docente = docente,
            EsLaboratorio3DLab = true,
            PreguntasMinimasLaboratorio = 5,
            PreguntasPorSesionLaboratorio = 3
        };

        var preguntas = Enumerable.Range(1, 5).Select(index =>
        {
            var pregunta = new Pregunta
            {
                Texto = $"Pregunta {index}",
                Orden = index,
                Evaluacion = evaluacion
            };

            pregunta.Opciones = new List<Opcion>
            {
                new() { Texto = "Opción A", EsCorrecta = true, Orden = 0, Pregunta = pregunta },
                new() { Texto = "Opción B", EsCorrecta = false, Orden = 1, Pregunta = pregunta },
                new() { Texto = "Opción C", EsCorrecta = false, Orden = 2, Pregunta = pregunta },
                new() { Texto = "Opción D", EsCorrecta = false, Orden = 3, Pregunta = pregunta }
            };

            return pregunta;
        }).ToList();

        evaluacion.Preguntas = preguntas;

        context.Users.Add(docente);
        context.Cursos.Add(curso);
        context.Evaluaciones.Add(evaluacion);
        context.Preguntas.AddRange(preguntas);
        context.Opciones.AddRange(preguntas.SelectMany(p => p.Opciones));

        var estudiante = new Usuario
        {
            Id = "std-1",
            UserName = "estudiante",
            Email = "estudiante@example.com",
            Nombre = "Estudiante",
            Rut = "22222222-2",
            Rol = "Alumno"
        };

        context.Users.Add(estudiante);

        await context.SaveChangesAsync();

        var service = new Laboratorio3DLabService(context, NullLogger<Laboratorio3DLabService>.Instance);

        var resultado = await service.CrearSesionYSeleccionarPreguntasAsync(evaluacion.Id, estudiante.Id);

    Assert.True(resultado.OperacionExitosa);
    Assert.NotNull(resultado.Dato);
    Assert.Equal(evaluacion.PreguntasPorSesionLaboratorio, resultado.Dato!.Preguntas.Count);

    var idsSeleccionados = resultado.Dato.Preguntas.Select(p => p.IdPregunta).ToList();
        Assert.Equal(idsSeleccionados.Distinct().Count(), idsSeleccionados.Count);
        Assert.All(idsSeleccionados, id => Assert.Contains(preguntas, p => p.Id == id));

        var sesion = await context.SesionesEvaluacion.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(sesion);
        Assert.False(string.IsNullOrWhiteSpace(sesion!.Metadata));
    }

    [Fact]
    public async Task CrearSesionYSeleccionarPreguntasAsync_FallaCuandoCantidadSuperaPool()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var docente = new Usuario
        {
            Id = "doc-2",
            UserName = "docente2",
            Email = "docente2@example.com",
            Nombre = "Docente",
            Rut = "11111111-1",
            Rol = "Docente"
        };

        var administrador = docente;

        var curso = new Curso
        {
            Nrc = 1002,
            Nombre = "Curso de Laboratorio",
            Descripcion = "Curso de pruebas",
            Activo = true,
            AdministradorId = administrador.Id,
            Administrador = administrador,
            CursoUsuarios = new List<CursoUsuario>()
        };

        var evaluacion = new Evaluacion
        {
            Titulo = "Laboratorio 3DLAB",
            CursoId = curso.Nrc,
            Curso = curso,
            DocenteId = docente.Id,
            Docente = docente,
            EsLaboratorio3DLab = true,
            PreguntasMinimasLaboratorio = 2,
            PreguntasPorSesionLaboratorio = 5
        };

        var preguntas = Enumerable.Range(1, 2).Select(index =>
        {
            var pregunta = new Pregunta
            {
                Texto = $"Pregunta {index}",
                Orden = index,
                Evaluacion = evaluacion
            };

            pregunta.Opciones = new List<Opcion>
            {
                new() { Texto = "Opción A", EsCorrecta = true, Orden = 0, Pregunta = pregunta },
                new() { Texto = "Opción B", EsCorrecta = false, Orden = 1, Pregunta = pregunta }
            };

            return pregunta;
        }).ToList();

        evaluacion.Preguntas = preguntas;

        context.Users.Add(docente);
        context.Cursos.Add(curso);
        context.Evaluaciones.Add(evaluacion);
        context.Preguntas.AddRange(preguntas);
        context.Opciones.AddRange(preguntas.SelectMany(p => p.Opciones));

        var estudiante = new Usuario
        {
            Id = "std-2",
            UserName = "estudiante2",
            Email = "estudiante2@example.com",
            Nombre = "Estudiante",
            Rut = "22222222-2",
            Rol = "Alumno"
        };

        context.Users.Add(estudiante);

        await context.SaveChangesAsync();

        var service = new Laboratorio3DLabService(context, NullLogger<Laboratorio3DLabService>.Instance);

        var resultado = await service.CrearSesionYSeleccionarPreguntasAsync(evaluacion.Id, estudiante.Id);

    Assert.False(resultado.OperacionExitosa);
    Assert.Equal(409, resultado.Codigo);
    Assert.Contains("preguntas por sesión", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
