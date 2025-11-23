using LMSBackend.API.Configuration;
using LMSBackend.API.Data;
using LMSBackend.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMSBackend.API.Helpers
{
    public static class ThreeDLabDataInitializer
    {
        public static async Task EnsureCursoYLaboratorioAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ThreeDLabDataInitializer");

            var options = scope.ServiceProvider.GetRequiredService<IOptions<ThreeDLabOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ServiceUserEmail))
            {
                logger.LogWarning("3DLAB: ServiceUserEmail no configurado. No se puede inicializar curso y laboratorio.");
                return;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var usuario3DLab = await userManager.FindByEmailAsync(options.ServiceUserEmail.Trim());
            if (usuario3DLab == null)
            {
                logger.LogWarning("3DLAB: Usuario de integración no encontrado. Ejecute primero ThreeDLabInitializer.");
                return;
            }

            // Buscar o crear el curso "3DLAB Laboratorio"
            var nombreCurso = "3DLAB Laboratorio";
            var curso = await context.Cursos
                .Include(c => c.CursoUsuarios)
                .FirstOrDefaultAsync(c => c.Nombre == nombreCurso);

            if (curso == null)
            {
                // Crear el curso
                curso = new Curso
                {
                    Nombre = nombreCurso,
                    Descripcion = "Curso de prueba para integración con 3DLAB. Contiene laboratorios de práctica para el sistema 3DLAB.",
                    Activo = true,
                    AdministradorId = usuario3DLab.Id
                };

                context.Cursos.Add(curso);
                await context.SaveChangesAsync();
                logger.LogInformation("3DLAB: Curso '{NombreCurso}' creado correctamente (NRC: {Nrc})", nombreCurso, curso.Nrc);
            }
            else
            {
                logger.LogInformation("3DLAB: Curso '{NombreCurso}' ya existe (NRC: {Nrc})", nombreCurso, curso.Nrc);
            }

            // Verificar si el usuario 3DLAB está inscrito en el curso
            var inscripcion = await context.CursoUsuarios
                .FirstOrDefaultAsync(cu => cu.CursoId == curso.Nrc && cu.UsuarioId == usuario3DLab.Id);

            if (inscripcion == null)
            {
                // Inscribir al usuario en el curso como Alumno
                inscripcion = new CursoUsuario
                {
                    CursoId = curso.Nrc,
                    UsuarioId = usuario3DLab.Id,
                    RolEnCurso = RolEnCurso.Alumno
                };

                context.CursoUsuarios.Add(inscripcion);
                await context.SaveChangesAsync();
                logger.LogInformation("3DLAB: Usuario '{Email}' inscrito en el curso '{NombreCurso}' como Alumno", usuario3DLab.Email, nombreCurso);
            }
            else
            {
                logger.LogInformation("3DLAB: Usuario '{Email}' ya está inscrito en el curso '{NombreCurso}'", usuario3DLab.Email, nombreCurso);
            }

            // Buscar o crear una evaluación de tipo laboratorio 3DLAB
            var evaluacion = await context.Evaluaciones
                .Include(e => e.Preguntas)
                    .ThenInclude(p => p.Opciones)
                .FirstOrDefaultAsync(e => e.CursoId == curso.Nrc && e.EsLaboratorio3DLab);

            if (evaluacion == null)
            {
                // Crear evaluación de laboratorio 3DLAB
                evaluacion = new Evaluacion
                {
                    Titulo = "Laboratorio 3DLAB - Prueba de Integración",
                    Descripcion = "Laboratorio de prueba para validar la integración con el sistema 3DLAB. Contiene preguntas de ejemplo.",
                    CursoId = curso.Nrc,
                    DocenteId = usuario3DLab.Id,
                    FechaCreacion = DateTime.UtcNow,
                    FechaInicio = DateTime.UtcNow,
                    FechaFin = DateTime.UtcNow.AddYears(10), // Válido por 10 años
                    TiempoLimiteMins = 0, // 0 = sin límite de tiempo para laboratorios 3DLAB
                    Activa = true,
                    IntentosMaximos = 0, // 0 = intentos ilimitados para laboratorios 3DLAB
                    EsLaboratorio3DLab = true,
                    PreguntasMinimasLaboratorio = 5,
                    PreguntasPorSesionLaboratorio = 5
                };

                context.Evaluaciones.Add(evaluacion);
                await context.SaveChangesAsync();

                // Crear preguntas de ejemplo
                var preguntasEjemplo = new List<Pregunta>
                {
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuál es el resultado de 2 + 2?",
                        Puntos = 10,
                        Orden = 1,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "3", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "4", EsCorrecta = true, Orden = 2 },
                            new Opcion { Texto = "5", EsCorrecta = false, Orden = 3 },
                            new Opcion { Texto = "6", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuál es la capital de Chile?",
                        Puntos = 10,
                        Orden = 2,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "Valparaíso", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "Santiago", EsCorrecta = true, Orden = 2 },
                            new Opcion { Texto = "Concepción", EsCorrecta = false, Orden = 3 },
                            new Opcion { Texto = "La Serena", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuántos días tiene una semana?",
                        Puntos = 10,
                        Orden = 3,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "5", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "6", EsCorrecta = false, Orden = 2 },
                            new Opcion { Texto = "7", EsCorrecta = true, Orden = 3 },
                            new Opcion { Texto = "8", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuál es el color del cielo en un día despejado?",
                        Puntos = 10,
                        Orden = 4,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "Verde", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "Azul", EsCorrecta = true, Orden = 2 },
                            new Opcion { Texto = "Rojo", EsCorrecta = false, Orden = 3 },
                            new Opcion { Texto = "Amarillo", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuántos meses tiene un año?",
                        Puntos = 10,
                        Orden = 5,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "10", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "11", EsCorrecta = false, Orden = 2 },
                            new Opcion { Texto = "12", EsCorrecta = true, Orden = 3 },
                            new Opcion { Texto = "13", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuál es el resultado de 10 / 2?",
                        Puntos = 10,
                        Orden = 6,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "3", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "4", EsCorrecta = false, Orden = 2 },
                            new Opcion { Texto = "5", EsCorrecta = true, Orden = 3 },
                            new Opcion { Texto = "6", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿En qué continente se encuentra Chile?",
                        Puntos = 10,
                        Orden = 7,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "Europa", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "América", EsCorrecta = true, Orden = 2 },
                            new Opcion { Texto = "Asia", EsCorrecta = false, Orden = 3 },
                            new Opcion { Texto = "África", EsCorrecta = false, Orden = 4 }
                        }
                    },
                    new Pregunta
                    {
                        EvaluacionId = evaluacion.Id,
                        Texto = "¿Cuál es el resultado de 3 × 3?",
                        Puntos = 10,
                        Orden = 8,
                        Opciones = new List<Opcion>
                        {
                            new Opcion { Texto = "6", EsCorrecta = false, Orden = 1 },
                            new Opcion { Texto = "9", EsCorrecta = true, Orden = 2 },
                            new Opcion { Texto = "12", EsCorrecta = false, Orden = 3 },
                            new Opcion { Texto = "15", EsCorrecta = false, Orden = 4 }
                        }
                    }
                };

                context.Preguntas.AddRange(preguntasEjemplo);
                await context.SaveChangesAsync();

                logger.LogInformation("3DLAB: Laboratorio de prueba creado correctamente (ID: {EvaluacionId}) con {CantidadPreguntas} preguntas",
                    evaluacion.Id, preguntasEjemplo.Count);
            }
            else
            {
                logger.LogInformation("3DLAB: Ya existe un laboratorio 3DLAB para el curso '{NombreCurso}' (ID: {EvaluacionId})",
                    nombreCurso, evaluacion.Id);
            }

            logger.LogInformation("3DLAB: Inicialización de datos completada. Curso NRC: {Nrc}, Evaluación ID: {EvaluacionId}",
                curso.Nrc, evaluacion.Id);
        }
    }
}
