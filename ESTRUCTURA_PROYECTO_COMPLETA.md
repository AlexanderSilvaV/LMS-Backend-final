# EXPLORACIÓN COMPLETA DEL PROYECTO LMSBackend

## 1. OVERVIEW DEL PROYECTO

### Tecnologías Base
- **Framework**: ASP.NET Core 9.0
- **Base de Datos**: PostgreSQL
- **Autenticación**: ASP.NET Identity + JWT Bearer
- **ORM**: Entity Framework Core
- **Almacenamiento**: AWS S3 + Google Drive
- **Email**: SendGrid/Brevo SMTP

### Estructura de Directorios
```
LMSBackend.API/
├── Controllers/          # Endpoints REST
├── Models/              # Entidades de base de datos
├── DTOs/                # Data Transfer Objects
├── Services/            # Lógica de negocio
├── Data/                # DbContext y configuración
├── Helpers/             # Utilidades y validadores
├── Configuration/       # Opciones de configuración
└── Properties/          # Configuración de proyecto
```

---

## 2. MODELOS DE DATOS (ESTRUCTURA DE BASE DE DATOS)

### 2.1 ENTIDAD USUARIO
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Usuario.cs`

```csharp
public class Usuario : IdentityUser
{
    public string Nombre { get; set; }
    public string Rut { get; set; }
    public string Rol { get; set; }  // Administrador, Docente, Alumno, Estudiante
    public ICollection<CursoUsuario> CursoUsuarios { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public UsuarioPerfil? Perfil { get; set; }
}
```

**Propiedades Heredadas de IdentityUser**:
- Id (GUID)
- UserName
- Email
- PasswordHash
- EmailConfirmed

**Relaciones**:
- 1:N con CursoUsuario (muchos cursos)
- 1:1 con UsuarioPerfil (perfil personal)
- 1:N con Material (creador de materiales)
- 1:N con Evaluacion (docente crea evaluaciones)

---

### 2.2 ENTIDAD CURSO
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Curso.cs`

```csharp
public class Curso
{
    [Key]
    public int Nrc { get; set; }  // Número de Registro de Curso
    [Required, MaxLength(70)]
    public string Nombre { get; set; }
    [Required, MaxLength(250)]
    public string Descripcion { get; set; }
    public bool Activo { get; set; }
    public string AdministradorId { get; set; }
    public Usuario Administrador { get; set; }
    public List<Modulo> Modulos { get; set; } = new();
    public ICollection<CursoUsuario> CursoUsuarios { get; set; }
}
```

**Relaciones**:
- N:1 con Usuario (administrador)
- 1:N con Modulo
- N:N con Usuario (vía CursoUsuario)
- 1:N con Evaluacion (cascada)

---

### 2.3 ENTIDAD MÓDULO
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Modulo.cs`

```csharp
public class Modulo
{
    [Key]
    public int ModuloId { get; set; }
    [Required, MaxLength(30)]
    public string Nombre { get; set; }
    [Range(0, 100)]
    public int Indice { get; set; }
    public bool EsPredeterminado { get; set; }
    public int CursoId { get; set; }
    public Curso Curso { get; set; }
    public List<Material> Materiales { get; set; } = new();
    public ICollection<Foro> Foros { get; set; } = new List<Foro>();
}
```

**Relaciones**:
- N:1 con Curso (cascada)
- 1:N con Material
- 1:N con Foro

---

### 2.4 ENTIDAD MATERIAL
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Material.cs`

```csharp
public enum TipoMaterial { Archivo, Enlace, Video }

public class Material
{
    [Key]
    public int MaterialId { get; set; }
    [Required, MaxLength(70)]
    public string Nombre { get; set; }
    [Required]
    public TipoMaterial Tipo { get; set; }
    [Required, MaxLength(500)]
    public string Ruta { get; set; }  // URL o ruta en almacenamiento
    public int ModuloId { get; set; }
    public Modulo Modulo { get; set; }
    public string UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
}
```

---

### 2.5 ENTIDAD CURSO-USUARIO (Relación N:N)
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/CursoUsuario.cs`

```csharp
public class CursoUsuario
{
    public string UsuarioId { get; set; }
    public int CursoId { get; set; }
    public RolEnCurso RolEnCurso { get; set; }  // Alumno, Docente
    
    public Usuario Usuario { get; set; }
    public Curso Curso { get; set; }
}

public enum RolEnCurso { Alumno, Docente }
```

**Clave Compuesta**: (CursoId, UsuarioId)

---

### 2.6 ENTIDAD PERFIL DE USUARIO
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/UsuarioPerfil.cs`

```csharp
public class UsuarioPerfil
{
    [Key, ForeignKey(nameof(Usuario))]
    public string UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    
    [MaxLength(1000)]
    public string? Descripcion { get; set; }
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    public byte[]? AvatarBytes { get; set; }
}
```

**Relación**: 1:1 con Usuario (cascada)

---

### 2.7 ENTIDAD EVALUACIÓN (Cuestionarios y Laboratorios)
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Evaluacion.cs`

```csharp
public class Evaluacion
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Titulo { get; set; }
    [MaxLength(500)]
    public string? Descripcion { get; set; }
    [Required]
    public int CursoId { get; set; }
    public Curso Curso { get; set; }
    [Required]
    public string DocenteId { get; set; }
    public Usuario Docente { get; set; }
    
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int TiempoLimiteMins { get; set; } = 60;
    public bool Activa { get; set; } = true;
    public int IntentosMaximos { get; set; } = 1;
    
    // Propiedades para Laboratorio 3DLAB
    public bool EsLaboratorio3DLab { get; set; } = false;
    public int PreguntasMinimasLaboratorio { get; set; } = 0;
    public int PreguntasPorSesionLaboratorio { get; set; } = 0;
    
    public ICollection<Pregunta> Preguntas { get; set; } = new List<Pregunta>();
    public ICollection<RespuestaUsuario> RespuestasUsuario { get; set; } = new List<RespuestaUsuario>();
}
```

**Validaciones en BD**:
- TiempoLimiteMins: > 0 AND <= 300
- IntentosMaximos: > 0 AND <= 10

---

### 2.8 ENTIDAD PREGUNTA
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Pregunta.cs`

```csharp
public class Pregunta
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(500)]
    public string Texto { get; set; }
    
    public int Orden { get; set; }
    public int Puntos { get; set; } = 1;
    
    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; }
    
    public ICollection<Opcion> Opciones { get; set; } = new List<Opcion>();
}
```

**Validaciones**: Puntos > 0 AND <= 100

---

### 2.9 ENTIDAD SESIÓN DE EVALUACIÓN
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/SesionEvaluacion.cs`

```csharp
public class SesionEvaluacion
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; }
    
    public int NumeroIntento { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFinalizacion { get; set; }
    public string Token { get; set; }  // Identificador único seguro
    public bool Activa { get; set; } = true;
    public bool Completada { get; set; } = false;
    public bool TiempoAgotado { get; set; } = false;
    
    // JSON serializado con metadata (ej: preguntas seleccionadas en 3DLAB)
    public string? Metadata { get; set; }
    
    public DateTime FechaLimite => FechaInicio.AddMinutes(Evaluacion?.TiempoLimiteMins ?? 60);
    public bool EstaVencida => DateTime.UtcNow > FechaLimite;
}
```

**Índices Únicos**: (UsuarioId, EvaluacionId, NumeroIntento)

---

### 2.10 ENTIDAD RESPUESTA DE USUARIO
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/RespuestaUsuario.cs`

```csharp
public class RespuestaUsuario
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    
    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; }
    
    [Required]
    public int PreguntaId { get; set; }
    public Pregunta Pregunta { get; set; }
    
    [Required]
    public int OpcionId { get; set; }
    public Opcion Opcion { get; set; }
    
    public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow;
    public int NumeroIntento { get; set; } = 1;
}
```

**Índice Único**: (UsuarioId, EvaluacionId, PreguntaId, NumeroIntento)

---

### 2.11 ENTIDAD NOTA
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Models/Nota.cs`

```csharp
public class Nota
{
    [Key]
    public int Id { get; set; }
    
    [Required, Range(1.0, 7.0)]
    [Column(TypeName = "decimal(3,1)")]
    public decimal Calificacion { get; set; }
    
    [Required]
    public string UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    
    [Required]
    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; }
    
    public DateTime FechaCalificacion { get; set; } = DateTime.UtcNow;
    [MaxLength(500)]
    public string? Observaciones { get; set; }
    
    public int NumeroIntento { get; set; } = 1;
    public bool EsNotaFinal { get; set; } = true;
    public bool EsLaboratorio { get; set; } = false;
}
```

**Validación**: Calificacion >= 1.0 AND <= 7.0
**Índice Único**: (UsuarioId, EvaluacionId, NumeroIntento)

---

### 2.12 ENTIDADES COMPLEMENTARIAS (Foros, Posts, Hilos)

Existen también entidades para:
- **Foro**: Foros de discusión por módulo
- **Hilo**: Temas dentro de foros
- **Post**: Mensajes en hilos
- **Evaluación de Banco de Preguntas**: Para preguntas reutilizables

---

## 3. DbContext y Configuración

### 3.1 ApplicationDbContext
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : IdentityDbContext<Usuario>
{
    public DbSet<Curso> Cursos { get; set; }
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
}
```

### 3.2 Configuraciones Clave en OnModelCreating

**Relación Curso -> Módulos (1:N)**:
```csharp
.HasMany(c => c.Modulos)
.WithOne(m => m.Curso)
.HasForeignKey(m => m.CursoId)
.OnDelete(DeleteBehavior.Cascade);
```

**Clave Compuesta CursoUsuario**:
```csharp
modelBuilder.Entity<CursoUsuario>()
    .HasKey(cu => new { cu.CursoId, cu.UsuarioId });
```

**Perfil de Usuario (1:1)**:
```csharp
e.HasOne(p => p.Usuario)
 .WithOne(u => u.Perfil)
 .HasForeignKey<UsuarioPerfil>(p => p.UsuarioId)
 .OnDelete(DeleteBehavior.Cascade);
```

**Evaluación -> Docente (N:1)**:
```csharp
.HasOne(e => e.Docente)
.WithMany()
.HasForeignKey(e => e.DocenteId)
.OnDelete(DeleteBehavior.Restrict);  // No permitir eliminar docente si tiene evaluaciones
```

---

## 4. CONTROLLERS (Endpoints REST)

### 4.1 UsuariosController
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Controllers/UsuariosController.cs`
**Ruta Base**: `/api/usuarios`

**Endpoints Principales**:

| Método | Endpoint | Autenticación | Descripción |
|--------|----------|---------------|-------------|
| POST | `/` | - | Crear nuevo usuario (DTO: UsuarioCreacionDTO) |
| POST | `/login` | - | Login y obtener JWT (DTO: UsuarioLoginDTO) |
| POST | `/recuperar-password` | - | Solicitar recuperación de contraseña |
| POST | `/restablecer-password` | - | Restablecer contraseña con token |
| GET | `/profile` | JWT Required | Obtener perfil actual |
| GET | `/` | Admin/Alumno | Listar usuarios con paginación |
| GET | `/{id}` | Admin | Obtener usuario por ID |
| GET | `/{id}/public` | Autenticado | Obtener info pública (roles limitados) |
| PUT | `/{id}` | Admin | Actualizar usuario |
| DELETE | `/{id}` | Admin | Eliminar usuario |
| PUT | `/{id}/name` | - | Actualizar nombre |
| POST | `/asignar-ruts-prueba` | Admin | Asignar RUTs de testing |

---

### 4.2 CursosController
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Controllers/CursosController.cs`
**Ruta Base**: `/api/cursos`

| Método | Endpoint | Autenticación | Descripción |
|--------|----------|---------------|-------------|
| POST | `/` | Admin | Crear curso (DTO: CursoCreacionDTO) |
| POST | `/buscar` | Admin | Buscar cursos (DTO: CursoBusquedaDTO) |
| PUT | `/{nrc}` | Admin | Editar curso |
| DELETE | `/{nrc}` | Admin | Eliminar curso |
| GET | `/asignados` | Docente/Alumno | Obtener cursos del usuario actual |
| POST | `/duplicar` | Admin | Duplicar curso (DTO: CursoDuplicacionDTO) |

---

### 4.3 Laboratorio3DLabController
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Controllers/Laboratorio3DLabController.cs`
**Ruta Base**: `/api/v2/3dlab`
**Autenticación**: API Key Header `X-3DLAB-Key`

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/preguntas?evaluacionId=X` | Obtener todas las preguntas disponibles para un laboratorio |
| GET | `/evaluaciones/{evaluacionId}/seleccion` | Crear sesión y seleccionar preguntas aleatorias |
| POST | `/resultados` | Registrar respuestas y obtener calificación |
| POST | `/token` | Generar token JWT para usuario de integración 3DLAB |

---

### 4.4 Controllers Adicionales

**EvaluacionController**: `/api/evaluaciones`
- Crear, editar, listar evaluaciones
- Gestión de preguntas y opciones

**ModulosController**: `/api/modulos`
- CRUD de módulos dentro de cursos

**MaterialController**: `/api/material`
- Gestión de materiales (archivos, enlaces, videos)

**ForoController**: `/api/foros`
- Gestión de foros y discusiones

**CursoUsuarioController**: `/api/curso-usuario`
- Asignar y desasignar usuarios a cursos

---

## 5. SERVICIOS (Business Logic)

### 5.1 Servicio de Usuarios
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Services/UsuarioService.cs`

**Métodos Principales**:
- `CrearUsuarioAsync(UsuarioCreacionDTO)`: Validar RUT, crear usuario con Identity
- `LoginUsuarioAsync(UsuarioLoginDTO)`: Autenticar y generar JWT
- `ObtenerPerfilAsync()`: Obtener usuario actual desde contexto HTTP
- `ObtenerUsuarioPorIdAsync(string id)`: Buscar usuario por ID
- `ListarUsuariosAsync(PaginacionDTO)`: Listar con paginación
- `EditarUsuarioAsync(string id, UsuarioEdicionDTO)`: Actualizar usuario
- `EliminarUsuarioAsync(string id)`: Eliminar usuario
- `RecuperarPasswordAsync(RecuperarPasswordDTO)`: Generar link de recuperación
- `RestablecerPasswordAsync(RestablecerPasswordDTO)`: Cambiar contraseña

**Validaciones**:
- Validación de RUT (formato + dígito verificador)
- Correo único
- RUT único
- Rol debe existir

---

### 5.2 Servicio de Laboratorio 3DLAB
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Services/Laboratorio3DLabService.cs`
**Líneas**: 390

**Métodos Principales**:

#### ObtenerPoolPreguntasAsync(int evaluacionId)
- Obtiene todas las preguntas disponibles para una evaluación 3DLAB
- Valida que sea un laboratorio (EsLaboratorio3DLab = true)
- Verifica cantidad mínima de preguntas
- Retorna: `LaboratorioPreguntasResponseDTO`

#### CrearSesionYSeleccionarPreguntasAsync(int evaluacionId, string alumnoId)
- Crea una SesionEvaluacion nueva
- Selecciona preguntas aleatorias según PreguntasPorSesionLaboratorio
- Almacena lista de preguntas en Metadata (JSON)
- Genera token seguro para la sesión
- Retorna: `LaboratorioSeleccionResponseDTO` con preguntas seleccionadas

#### RegistrarResultadosAsync(LaboratorioResultadosRequestDTO request)
- **Flujo**:
  1. Busca sesión por token
  2. Valida que corresponda al alumno
  3. Deserializa metadata para obtener preguntas originales
  4. Valida cantidad de respuestas
  5. Procesa cada respuesta:
     - Busca opción correcta
     - Calcula puntos (correcto = puntos de pregunta, incorrecto = 0)
  6. Calcula calificación escala 1.0-7.0: `(porcentaje * 6) + 1`
  7. Crea/Actualiza Nota
  8. Guarda RespuestasUsuario para historial
  9. Marca sesión como completada
- Retorna: `LaboratorioResultadosResponseDTO` con detalle de respuestas

**Métodos Auxiliares**:
- `SelectRandomQuestions()`: Selecciona N preguntas al azar
- `MapPregunta()`: Convierte Pregunta a LaboratorioPreguntaDTO
- `MapOpcionesPregunta()`: Crea diccionario de opciones por letra (A,B,C,D)
- `DeserializeMetadata()`: Deserializa JSON de metadata

---

### 5.3 Servicio de Token (JWT)
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Services/TokenService.cs`

```csharp
public string GenerarToken(Usuario usuario)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, usuario.Id),
        new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
        new Claim("nombre", usuario.Nombre),
        new Claim(ClaimTypes.Role, usuario.Rol)
    };
    
    // Usuario 3DLAB tiene token sin expiración (25 minutos para otros)
    DateTime? expiresAt = IsThreeDLabUser ? null : DateTime.UtcNow.AddMinutes(25);
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Configuración JWT** (appsettings.json):
```json
{
  "Jwt": {
    "Key": "1234567890ABCDEF1234567890ABCDEF",
    "Issuer": "LMSBackend",
    "Audience": "LMSBackendClient"
  }
}
```

---

### 5.4 Servicios Adicionales

**CursoService**:
- Crear, editar, listar, eliminar cursos
- Duplicar cursos (copia módulos y materiales)
- Validación de permisos

**CursoUsuarioService**:
- Asignar usuarios a cursos
- Obtener cursos del usuario actual
- Gestión de roles en curso

**EvaluacionService**:
- CRUD de evaluaciones
- Configuración de laboratorios 3DLAB
- Gestión de preguntas y opciones

**ModuloService**:
- CRUD de módulos
- Reordenar módulos

**MaterialService**:
- Subida de materiales
- Integración con AWS S3 y Google Drive

**ForoService**, **HiloService**, **PostService**:
- Gestión de foros, hilos y discusiones

**EmailService**, **TokenService**:
- Envío de emails (SendGrid/Brevo)
- Generación de tokens JWT

---

## 6. DATA TRANSFER OBJECTS (DTOs)

### 6.1 DTOs de Evaluación/Laboratorio

**Laboratorio3DLabDTO.cs**:
- `LaboratorioPreguntaDTO`: pregunta + opciones (A,B,C,D)
- `LaboratorioPreguntasResponseDTO`: evaluacionId, cursoId, lista de preguntas
- `LaboratorioSeleccionResponseDTO`: sesionId, preguntas seleccionadas para alumno
- `LaboratorioRespuestaSeleccionadaDTO`: preguntaId + selección (A-D)
- `LaboratorioResultadosRequestDTO`: sesionId, evaluacionId, alumnoId, respuestas[]
- `LaboratorioResultadosResponseDTO`: puntaje, calificación, detalle de respuestas

**EvaluacionCreacionDTO**:
```csharp
public class EvaluacionCreacionDTO
{
    public string Titulo { get; set; }
    public string? Descripcion { get; set; }
    public int CursoId { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int TiempoLimiteMins { get; set; } = 60;
    public int IntentosMaximos { get; set; } = 1;
    public List<PreguntaCreacionDTO>? Preguntas { get; set; }
}
```

**EvaluacionDTO**:
```csharp
public class EvaluacionDTO
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public int CursoId { get; set; }
    public string DocenteId { get; set; }
    public string DocenteNombre { get; set; }
    public int TiempoLimiteMins { get; set; }
    public int IntentosMaximos { get; set; }
    public int TotalPreguntas { get; set; }
    public List<PreguntaDTO> Preguntas { get; set; } = new();
}
```

### 6.2 DTOs de Usuario

**UsuarioCreacionDTO**:
```csharp
public class UsuarioCreacionDTO
{
    public string Nombre { get; set; }
    public string Correo { get; set; }
    public string Rut { get; set; }
    public string Contraseña { get; set; }
    public string Rol { get; set; }  // Administrador, Docente, Alumno
}
```

**UsuarioDTO**:
```csharp
public class UsuarioDTO
{
    public string Id { get; set; }
    public string Nombre { get; set; }
    public string Correo { get; set; }
    public string Rut { get; set; }
    public string Rol { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? FotoPerfil { get; set; }
}
```

**UsuarioLoginDTO**:
```csharp
public class UsuarioLoginDTO
{
    public string Correo { get; set; }
    public string Contraseña { get; set; }
}
```

### 6.3 DTOs de Curso

**CursoDTO**:
```csharp
public class CursoDTO
{
    public int Nrc { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public bool Activo { get; set; }
}
```

**CursoCreacionDTO**:
```csharp
public class CursoCreacionDTO
{
    public int Nrc { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public bool Activo { get; set; }
}
```

---

## 7. ARCHIVOS DE SEEDING E INICIALIZACIÓN

### 7.1 InicializadorDeRoles
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Helpers/InicializadorDeRoles.cs`

```csharp
public static async Task InicializarRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Administrador", "Docente", "Alumno", "Estudiante" };
    
    foreach (var rol in roles)
    {
        if (!await roleManager.RoleExistsAsync(rol))
        {
            await roleManager.CreateAsync(new IdentityRole(rol));
        }
    }
}
```

**Ejecución**: Se llama en `Program.cs` durante startup

---

### 7.2 ThreeDLabInitializer
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/LMSBackend.API/Helpers/ThreeDLabInitializer.cs`

Crea usuario de integración 3DLAB automáticamente si está configurado:
- Email: `{ThreeDLabOptions.ServiceUserEmail}`
- Nombre: `"Integración 3DLAB"`
- RUT: `"99999999-9"`
- Rol: Configurable (default: "Alumno")

**Actualiza**: Sincroniza cambios si el usuario ya existe

---

### 7.3 Seeder de Laboratorios 3DLAB
**Archivo**: `/root/elweno/LMS-Backend-Laboratorio/Seeder/Program.cs`

Script que:
1. Busca curso con nombre que contiene "3dlab"
2. Encuentra un docente existente
3. Crea 2 evaluaciones de laboratorio si no existen:
   - "Laboratorio Virtual 3DLAB - Seguridad"
   - "Laboratorio Virtual 3DLAB - Reactivos"
4. Configura propiedades de laboratorio

---

## 8. CONFIGURACIÓN DE AUTENTICACIÓN JWT

### 8.1 Configuración en Program.cs

```csharp
// 1. Agregar servicio Identity
builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 2. Configurar autenticación JWT
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        RoleClaimType = ClaimTypes.Role
    };
});

// 3. Agregar autorización
builder.Services.AddAuthorization();

// 4. En middleware
app.UseAuthentication();
app.UseAuthorization();
```

### 8.2 Propiedades del Token JWT

**Claims**:
- `sub` (subject): ID del usuario
- `email`: Email del usuario
- `nombre`: Nombre del usuario
- `role`: Rol del usuario

**Validaciones**:
- Firma: HMAC-SHA256
- Expiración: 25 minutos (excepto usuario 3DLAB sin expiración)
- Issuer: "LMSBackend"
- Audience: "LMSBackendClient"

### 8.3 Configuración 3DLAB

**ThreeDLabOptions.cs**:
```csharp
public class ThreeDLabOptions
{
    public string ApiKey { get; set; }              // Header X-3DLAB-Key
    public string ServiceUserEmail { get; set; }    // Email del usuario de integración
    public string ServiceUserPassword { get; set; } // Contraseña
    public string ServiceUserName { get; set; }     // "Integración 3DLAB"
    public string ServiceUserRut { get; set; }      // "99999999-9"
    public string ServiceUserRole { get; set; }     // "Alumno"
}
```

**Configuración en appsettings.json** (se debe agregar):
```json
{
  "ThreeDLab": {
    "ApiKey": "tu-clave-api-3dlab",
    "ServiceUserEmail": "3dlab@lms.local",
    "ServiceUserPassword": "contraseña-segura",
    "ServiceUserName": "Integración 3DLAB",
    "ServiceUserRut": "99999999-9",
    "ServiceUserRole": "Alumno"
  }
}
```

---

## 9. ESTRUCTURA DE DIRECTORIOS COMPLETA

```
LMSBackend.API/
├── Controllers/
│   ├── UsuariosController.cs
│   ├── CursosController.cs
│   ├── ModulosController.cs
│   ├── MaterialController.cs
│   ├── Laboratorio3DLabController.cs
│   ├── EvaluacionController.cs
│   ├── EvaluacionEstudianteController.cs
│   ├── ForoController.cs
│   ├── HiloController.cs
│   ├── PostsController.cs
│   ├── CursoUsuarioController.cs
│   ├── ProfileController.cs
│   ├── BancoPreguntasController.cs
│   ├── NotasController.cs
│   ├── SuscripcionesController.cs
│   └── LecturasController.cs
│
├── Models/
│   ├── Usuario.cs
│   ├── Curso.cs
│   ├── Modulo.cs
│   ├── Material.cs
│   ├── CursoUsuario.cs
│   ├── UsuarioPerfil.cs
│   ├── Evaluacion.cs
│   ├── Pregunta.cs
│   ├── Opcion.cs
│   ├── SesionEvaluacion.cs
│   ├── RespuestaUsuario.cs
│   ├── Nota.cs
│   ├── Foro.cs
│   ├── Hilo.cs
│   ├── HiloSuscripcion.cs
│   ├── HiloLectura.cs
│   ├── Post.cs
│   ├── BancoPregunta.cs
│   ├── OpcionBanco.cs
│   ├── Categoria.cs
│   ├── EmailLog.cs
│   └── TipoEvaluacion.cs
│
├── Services/
│   ├── UsuarioService.cs
│   ├── TokenService.cs
│   ├── EmailService.cs
│   ├── CursoService.cs
│   ├── ModuloService.cs
│   ├── MaterialService.cs
│   ├── CursoUsuarioService.cs
│   ├── EvaluacionService.cs
│   ├── ValidacionEvaluacionService.cs
│   ├── Laboratorio3DLabService.cs
│   ├── ForoService.cs
│   ├── HiloService.cs
│   ├── PostService.cs
│   ├── SuscripcionService.cs
│   ├── LecturaService.cs
│   ├── BancoPreguntasService.cs
│   ├── GoogleDriveService.cs
│   ├── S3StorageService.cs
│   ├── LocalStorageService.cs
│   ├── IStorageService.cs
│   └── IGoogleDriveService.cs
│
├── DTOs/
│   ├── (50+ DTOs para diferentes entidades)
│   ├── Laboratorio3DLabDTO.cs
│   ├── EvaluacionDTO.cs
│   ├── EvaluacionCreacionDTO.cs
│   ├── UsuarioDTO.cs
│   ├── UsuarioCreacionDTO.cs
│   ├── CursoDTO.cs
│   ├── CursoCreacionDTO.cs
│   ├── PreguntaDTO.cs
│   ├── OpcionDTO.cs
│   ├── ResultadoOperacion.cs
│   └── ...
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Helpers/
│   ├── InicializadorDeRoles.cs
│   ├── ThreeDLabInitializer.cs
│   ├── ValidadorRut.cs
│   └── ResultadoOperacion.cs
│
├── Configuration/
│   └── ThreeDLabOptions.cs
│
├── Properties/
│   └── launchSettings.json
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── LMSBackend.API.csproj
```

---

## 10. FLUJO DE INTEGRACIÓN CON 3DLAB (Resumen)

### 10.1 Inicialización
1. Crear usuario de integración (ThreeDLabInitializer)
2. Crear evaluación con `EsLaboratorio3DLab = true`
3. Añadir preguntas a la evaluación
4. Configurar `PreguntasPorSesionLaboratorio` y `PreguntasMinimasLaboratorio`

### 10.2 Flujo de Evaluación
1. **3DLAB solicita preguntas**: `GET /api/v2/3dlab/preguntas?evaluacionId=X`
   - Retorna: Lista de todas las preguntas disponibles

2. **Alumno comienza evaluación**: `GET /api/v2/3dlab/evaluaciones/{evaluacionId}/seleccion?alumnoId=Y`
   - Sistema crea SesionEvaluacion
   - Selecciona N preguntas al azar (según configuración)
   - Retorna: sesionId + preguntas seleccionadas

3. **Alumno responde**: `POST /api/v2/3dlab/resultados`
   - Recibe: sesionId, alumnoId, respuestas
   - Valida respuestas
   - Calcula puntaje
   - Crea/actualiza Nota
   - Registra RespuestasUsuario
   - Retorna: resultado detallado

4. **3DLAB obtiene token**: `POST /api/v2/3dlab/token`
   - Retorna JWT para usuario de integración

### 10.3 Autenticación
- **Header requerido**: `X-3DLAB-Key: {clave-configurada}`
- **Validación**: En Laboratorio3DLabController.Autorizar()

---

## 11. PALABRAS CLAVE TÉCNICAS

- **Entity Framework Core**: ORM para acceso a datos
- **Identity**: Framework de autenticación/autorización
- **JWT Bearer**: Token para APIs REST
- **Async/Await**: Operaciones asincrónicas
- **Dependency Injection**: Inyección de dependencias
- **DbContext**: Contexto de base de datos
- **Migrations**: Versionamiento de esquema BD
- **CORS**: Control de origen cruzado
- **DTO**: Objetos de transferencia de datos
- **Service Layer**: Capa de lógica de negocio

---

## 12. RESUMEN PARA IMPLEMENTACIÓN 3DLAB

**Modelos clave**:
- Evaluacion (EsLaboratorio3DLab, PreguntasMinimasLaboratorio, PreguntasPorSesionLaboratorio)
- SesionEvaluacion (Token, Metadata para almacenar preguntas seleccionadas)
- Pregunta + Opcion (para base de preguntas)
- Nota (para registro de calificaciones)
- RespuestaUsuario (para historial)

**Servicios clave**:
- Laboratorio3DLabService (orquestación del laboratorio)
- TokenService (generación JWT)
- UsuarioService (gestión de usuarios)

**Endpoints principales**:
- GET `/api/v2/3dlab/preguntas`
- GET `/api/v2/3dlab/evaluaciones/{id}/seleccion`
- POST `/api/v2/3dlab/resultados`
- POST `/api/v2/3dlab/token`

**Autenticación**: API Key + JWT Bearer

