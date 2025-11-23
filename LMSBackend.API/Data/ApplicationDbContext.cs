//Importaciones
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LMSBackend.API.Models;


namespace LMSBackend.API.Data // Declarando un spacio logico, para organizar el codigo y solucionar problemas de nombramiento de clases.
{
    public class ApplicationDbContext : IdentityDbContext<Usuario> // Creacion de clase (ApplicationDbContext) y hereda de IdentityDbContext, mi entidad principal sera mi clase Usuario
    {
        public DbSet<Curso> Cursos { get; set; } // Propiedad cursos, para la creacion de la tabla Curso en la DB.

        public DbSet<Modulo> Modulos { get; set; }

        public DbSet<Material> Materiales { get; set; }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<CursoUsuario> CursoUsuarios { get; set; }

        public DbSet<Foro> Foros { get; set; }
        public DbSet<Hilo> Hilos { get; set; }
        public DbSet<HiloSuscripcion> HiloSuscripciones { get; set; }
        public DbSet<HiloLectura> HiloLecturas { get; set; }
        public DbSet<Post> Posts { get; set; }


        public DbSet<UsuarioPerfil> Perfiles { get; set; }

        public DbSet<Evaluacion> Evaluaciones { get; set; }
        public DbSet<Pregunta> Preguntas { get; set; }
        public DbSet<Opcion> Opciones { get; set; }
        public DbSet<RespuestaUsuario> RespuestasUsuario { get; set; }
        public DbSet<SesionEvaluacion> SesionesEvaluacion { get; set; }
        public DbSet<Nota> Notas { get; set; }

        public DbSet<BancoPregunta> BancoPreguntas { get; set; }
        public DbSet<OpcionBanco> OpcionesBanco { get; set; }
        public DbSet<Categoria> Categorias { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) // Esto es un Constructor, ApplicationDbContext usa Usuario como el modelo, DbContextOptions<ApplicationDbContext>configura la conexión a la base de datos utilizando opciones externas.
        : base(options) // Le pasa options a IdentityDbContext
        {

        }

        // Constructor sin parámetros para Entity Framework migrations
        public ApplicationDbContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=lmsdb;Username=postgres;Password=mariano123");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Curso -> Módulos (1:N)

            modelBuilder.Entity<Curso>()
                .HasMany(c => c.Modulos)
                .WithOne(m => m.Curso)
                .HasForeignKey(m => m.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Módulo -> Materiales (1:N)

            modelBuilder.Entity<Modulo>()
                .HasMany(m => m.Materiales)
                .WithOne(mat => mat.Modulo)
                .HasForeignKey(mat => mat.ModuloId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CursoUsuario>()
                .HasKey(cu => new { cu.CursoId, cu.UsuarioId });

            modelBuilder.Entity<CursoUsuario>()
                .HasOne(cu => cu.Curso)
                .WithMany(c => c.CursoUsuarios)
                .HasForeignKey(cu => cu.CursoId);

            modelBuilder.Entity<CursoUsuario>()
                .HasOne(cu => cu.Usuario)
                .WithMany(u => u.CursoUsuarios)
                .HasForeignKey(cu => cu.UsuarioId);

            modelBuilder.Entity<CursoUsuario>()
                .Property(cu => cu.RolEnCurso)
                .HasConversion<string>();

            modelBuilder.Entity<Curso>(e =>
            {
                e.Property(c => c.PortadaFileId)
                 .HasMaxLength(150);
                e.Property(c => c.PortadaUrl)
                 .HasMaxLength(500);
            });

            modelBuilder.Entity<UsuarioPerfil>(e =>
            {
                e.HasOne(p => p.Usuario)
                 .WithOne(u => u.Perfil)
                 .HasForeignKey<UsuarioPerfil>(p => p.UsuarioId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(p => p.Descripcion).HasMaxLength(1000);
                e.Property(p => p.AvatarBytes)
                 .HasColumnType("bytea");
            });

            modelBuilder.Entity<Foro>(e =>
                {
                    // PK
                    e.HasKey(f => f.ForoId);

                    e.HasOne(p => p.Modulo)
                    .WithMany(m => m.Foros)
                    .HasForeignKey(f => f.ModuloId)
                    .OnDelete(DeleteBehavior.Cascade);

                    e.Property(f => f.Titulo).HasMaxLength(120).IsRequired();
                    e.Property(f => f.Descripcion).HasMaxLength(2000);

                    // ENUM
                    e.Property(f => f.Estado).HasConversion<string>().IsRequired().HasDefaultValue(Estado.Activo);

                    // Flags
                    e.Property(f => f.AllowStudentThreads).HasDefaultValue(true);
                    e.Property(f => f.RequireInitialPostToView).HasDefaultValue(true);


                    e.HasIndex(f => f.ModuloId).HasDatabaseName("IX_Foros_Modulo");
                    e.HasIndex(f => new { f.ModuloId, f.Estado }).HasDatabaseName("IX_Foros_Modulo_Estado");

                });

            modelBuilder.Entity<Hilo>(e =>
                {
                    // PK
                    e.HasKey(h => h.HiloId);

                    e.HasOne(h => h.Foro)
                     .WithMany(f => f.Hilos)
                     .HasForeignKey(h => h.ForoId)
                     .OnDelete(DeleteBehavior.Cascade);


                    e.Property(h => h.Titulo).IsRequired().HasMaxLength(120);

                    e.Property(h => h.Pinned).HasDefaultValue(false);

                    e.Property(h => h.Cerrado).HasDefaultValue(false);


                    e.Property(h => h.LastActivityAt).IsRequired();
                    e.HasIndex(h => new { h.ForoId, h.Pinned, h.PinnedOrder, h.LastActivityAt })
                     .HasDatabaseName("IX_Hilos_Foro_Orden");


                });
            modelBuilder.Entity<Post>(e =>
            {
                e.HasKey(p => p.PostId);

                e.HasOne(p => p.Hilo)
                 .WithMany(h => h.Posts)
                 .HasForeignKey(p => p.HiloId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(p => p.Contenido).IsRequired().HasMaxLength(10000);

                e.Property(p => p.Editado).HasDefaultValue(false);
                e.Property(p => p.SoftDeleted).HasDefaultValue(false);

                e.HasIndex(p => new { p.HiloId, p.FechaCreacion })
                 .HasDatabaseName("IX_Posts_Hilo_Chrono");
            });
            modelBuilder.Entity<HiloSuscripcion>(e =>
            {
                e.HasKey(s => new { s.UsuarioId, s.HiloId });

                e.HasOne(s => s.Hilo)
                 .WithMany()                 // no necesitas colección en Hilo
                 .HasForeignKey(s => s.HiloId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(s => s.FechaSuscripcion).IsRequired();
                e.HasIndex(s => s.HiloId);   //listar suscriptores por hilo
            });
            modelBuilder.Entity<HiloLectura>(e =>
            {
                e.HasKey(r => new { r.UsuarioId, r.HiloId });

                e.HasOne(r => r.Hilo)
                 .WithMany()
                 .HasForeignKey(r => r.HiloId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(r => r.LastReadAt).IsRequired();
                e.HasIndex(r => r.HiloId);   //listar lecturas por hilo
            });

            // Evaluación -> Curso (N:1)
            modelBuilder.Entity<Evaluacion>()
                .HasOne(e => e.Curso)
                .WithMany()
                .HasForeignKey(e => e.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Evaluación -> Docente (N:1)
            modelBuilder.Entity<Evaluacion>()
                .HasOne(e => e.Docente)
                .WithMany()
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pregunta -> Evaluación (N:1)
            modelBuilder.Entity<Pregunta>()
                .HasOne(p => p.Evaluacion)
                .WithMany(e => e.Preguntas)
                .HasForeignKey(p => p.EvaluacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Opcion -> Pregunta (N:1)
            modelBuilder.Entity<Opcion>()
                .HasOne(o => o.Pregunta)
                .WithMany(p => p.Opciones)
                .HasForeignKey(o => o.PreguntaId)
                .OnDelete(DeleteBehavior.Cascade);

            // RespuestaUsuario -> Usuario (N:1)
            modelBuilder.Entity<RespuestaUsuario>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // RespuestaUsuario -> Evaluación (N:1)
            modelBuilder.Entity<RespuestaUsuario>()
                .HasOne(r => r.Evaluacion)
                .WithMany(e => e.RespuestasUsuario)
                .HasForeignKey(r => r.EvaluacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // RespuestaUsuario -> Pregunta (N:1)
            modelBuilder.Entity<RespuestaUsuario>()
                .HasOne(r => r.Pregunta)
                .WithMany()
                .HasForeignKey(r => r.PreguntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // RespuestaUsuario -> Opcion (N:1)
            modelBuilder.Entity<RespuestaUsuario>()
                .HasOne(r => r.Opcion)
                .WithMany()
                .HasForeignKey(r => r.OpcionId)
                .OnDelete(DeleteBehavior.Restrict);

            // SesionEvaluacion -> Usuario (N:1)
            modelBuilder.Entity<SesionEvaluacion>()
                .HasOne(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // SesionEvaluacion -> Evaluacion (N:1)
            modelBuilder.Entity<SesionEvaluacion>()
                .HasOne(s => s.Evaluacion)
                .WithMany()
                .HasForeignKey(s => s.EvaluacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices únicos para integridad de datos
            modelBuilder.Entity<RespuestaUsuario>()
                .HasIndex(r => new { r.UsuarioId, r.EvaluacionId, r.PreguntaId, r.NumeroIntento })
                .IsUnique()
                .HasDatabaseName("IX_RespuestaUsuario_Unique");

            modelBuilder.Entity<SesionEvaluacion>()
                .HasIndex(s => new { s.UsuarioId, s.EvaluacionId, s.NumeroIntento })
                .IsUnique()
                .HasDatabaseName("IX_SesionEvaluacion_Unique");

            // Validaciones adicionales de integridad (sintaxis PostgreSQL)
            modelBuilder.Entity<Evaluacion>()
                .ToTable(t => {
                    t.HasCheckConstraint("CK_Evaluacion_TiempoLimite",
                        "((\"EsLaboratorio3DLab\" = TRUE AND \"TiempoLimiteMins\" = 0) OR (\"EsLaboratorio3DLab\" = FALSE AND \"TiempoLimiteMins\" > 0 AND \"TiempoLimiteMins\" <= 300))");
                    t.HasCheckConstraint("CK_Evaluacion_IntentosMaximos",
                        "((\"EsLaboratorio3DLab\" = TRUE AND \"IntentosMaximos\" = 0) OR (\"EsLaboratorio3DLab\" = FALSE AND \"IntentosMaximos\" > 0 AND \"IntentosMaximos\" <= 10))");
                });

            modelBuilder.Entity<Pregunta>()
                .ToTable(t => t.HasCheckConstraint("CK_Pregunta_Puntos", "\"Puntos\" > 0 AND \"Puntos\" <= 100"));


            // Nota -> Usuario (N:1)
            modelBuilder.Entity<Nota>()
                .HasOne(n => n.Usuario)
                .WithMany()
                .HasForeignKey(n => n.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            // Nota -> Evaluación (N:1)
            modelBuilder.Entity<Nota>()
                .HasOne(n => n.Evaluacion)
                .WithMany()
                .HasForeignKey(n => n.EvaluacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice único para evitar notas duplicadas por intento
            modelBuilder.Entity<Nota>()
                .HasIndex(n => new { n.UsuarioId, n.EvaluacionId, n.NumeroIntento })
                .IsUnique()
                .HasDatabaseName("IX_Nota_Usuario_Evaluacion_Intento");

            // Validación de calificación
            modelBuilder.Entity<Nota>()
                .ToTable(t => t.HasCheckConstraint("CK_Nota_Calificacion", "\"Calificacion\" >= 1.0 AND \"Calificacion\" <= 7.0"));
          
            // BancoPregunta -> Docente (N:1)
            modelBuilder.Entity<BancoPregunta>()
                .HasOne(bp => bp.Docente)
                .WithMany()
                .HasForeignKey(bp => bp.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);

            // OpcionBanco -> BancoPregunta (N:1)
            modelBuilder.Entity<OpcionBanco>()
                .HasOne(ob => ob.BancoPregunta)
                .WithMany(bp => bp.Opciones)
                .HasForeignKey(ob => ob.BancoPreguntaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Validaciones adicionales para Banco de Preguntas
            modelBuilder.Entity<BancoPregunta>()
                .ToTable(t => t.HasCheckConstraint("CK_BancoPregunta_Puntos", "\"Puntos\" > 0 AND \"Puntos\" <= 100"));

            // Índices para mejora de performance
            modelBuilder.Entity<BancoPregunta>()
                .HasIndex(bp => bp.Categoria)
                .HasDatabaseName("IX_BancoPregunta_Categoria");

            modelBuilder.Entity<BancoPregunta>()
                .HasIndex(bp => new { bp.DocenteId, bp.Activa })
                .HasDatabaseName("IX_BancoPregunta_Docente_Activa");

        }
    }
}
