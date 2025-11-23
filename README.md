# LMS Backend - Documentación de Configuración

## 📋 Resumen de Cambios Aplicados

Este documento detalla todas las configuraciones y cambios realizados en el sistema LMS (Learning Management System).

---

## 🌐 Configuración de Servidores

### Backend (ASP.NET Core)
- **Puerto**: `5253`
- **URL Local**: `http://localhost:5253`
- **URL Pública**: `http://135.148.148.88:5253`
- **Estado**: ✅ Activo
- **Base de Datos**: PostgreSQL (Azure/Neon) - `lmsdb`

### Frontend (Next.js)
- **Puerto**: `3000`
- **URL Local**: `http://localhost:3000`
- **URL Pública**: `http://135.148.148.88:3000`
- **Estado**: ✅ Activo
- **API URL**: `http://135.148.148.88:5253`

---

## 🔧 Cambios en el Backend

### 1. Modelos Actualizados

#### `Models/Evaluacion.cs` (Líneas 32-35)
Agregadas propiedades para soporte de Laboratorio 3DLab:
```csharp
public bool EsLaboratorio3DLab { get; set; } = false;
public int PreguntasMinimasLaboratorio { get; set; } = 0;
public int PreguntasPorSesionLaboratorio { get; set; } = 0;
```

#### `Models/SesionEvaluacion.cs` (Líneas 28-29)
```csharp
public string? Metadata { get; set; }
```

#### `Models/Nota.cs` (Línea 33)
```csharp
public bool EsLaboratorio { get; set; } = false;
```

### 2. Configuración CORS - `Program.cs`

**Líneas 161-170** - Configuración CORS actualizada:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001",
            "http://135.148.148.88:3000",
            "http://158.69.212.241:3000"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

**Línea 226** - Aplicación de CORS:
```csharp
app.UseCors("AllowLocalhost3000");
```

### 3. Controladores Actualizados

#### `Controllers/UsuariosController.cs`
- **Línea 97**: Endpoint GET `/api/usuarios`
  ```csharp
  [Authorize(Roles = "Administrador, Alumno")]
  ```

#### `Controllers/ModulosController.cs`
- **Línea 20**: GET `/api/Modulos/curso/{cursoId}`
  ```csharp
  [Authorize(Roles = "Administrador, Docente, Alumno")]
  ```
- **Línea 31**: Validación de rol
  ```csharp
  if (userRole == "Alumno")
  ```
- **Línea 81**: GET `/api/Modulos/por-curso/{cursoId}`
  ```csharp
  [Authorize(Roles = "Docente, Alumno")]
  ```

#### `Controllers/CursosController.cs`
- **Línea 54**: GET `/api/cursos/asignados`
  ```csharp
  [Authorize(Roles = "Docente,Alumno")]
  ```

#### `Controllers/ProfileController.cs`
Controlador existente con endpoint `/api/Profile/me`:
- **Línea 38-53**: GET para obtener perfil completo del usuario

---

## 🗄️ Base de Datos

### Conexión
```
Host: ep-little-hat-a8luhbp4-pooler.eastus2.azure.neon.tech
Database: lmsdb
Username: neondb_owner
```

### Roles de Usuario Estandarizados

Todos los roles fueron actualizados para consistencia:

| Rol Anterior | Rol Actual | Cantidad |
|--------------|------------|----------|
| Admin | Administrador | 4 |
| Docente | Docente | 3 |
| Estudiante | Alumno | 11 |
| Alumno | Alumno | - |

**SQL Ejecutado:**
```sql
UPDATE "AspNetUsers" SET "Rol" = 'Administrador' WHERE "Rol" = 'Admin';
UPDATE "AspNetUsers" SET "Rol" = 'Alumno' WHERE "Rol" = 'Estudiante';
```

### Datos Creados

#### Cursos
- **Total**: 55 cursos
- **Existentes**: 5 cursos (NRC 1001-1004, 12554)
- **Nuevos**: 50 cursos (NRC 2001-2050)

**Distribución por categoría (50 nuevos):**
- Programación: 5 cursos
- Base de Datos: 5 cursos
- Redes: 5 cursos
- Seguridad: 5 cursos
- Inteligencia Artificial: 5 cursos
- Cloud Computing: 5 cursos
- DevOps: 5 cursos
- Machine Learning: 5 cursos
- Blockchain: 5 cursos
- Web Development: 5 cursos

#### Asignaciones
- **Total**: 284 asignaciones curso-alumno
- Cada curso tiene entre 3-8 alumnos asignados

---

## 🎨 Configuración del Frontend

### Variables de Entorno - `.env.local`

```

### Content Security Policy - `next.config.mjs`

**Línea 27** - CSP actualizado:
```javascript
"connect-src 'self' http://localhost:5253 http://135.148.148.88:5253 https:"
```

---

## 👥 Usuarios de Prueba

### Administradores
| Email | Contraseña | Rol |
|-------|------------|-----|
| admin@admin.com | Admin1. | Administrador |
| superadmin@example.com | - | Administrador |
| mariano.simonin@gmail.com | - | Administrador |
| n.cheuquesaa@uandresbello.edu | - | Administrador |

### Docentes
| Email | Contraseña | Rol |
|-------|------------|-----|
| docente@docente.com | - | Docente |
| Docentevideo@uandresbello.edu | - | Docente |
| mariano_fernandezzs@hotmail.com | - | Docente |

### Estudiantes (Todos con contraseña: `Password123.`)
| Email | Nombre | Cursos Asignados |
|-------|--------|------------------|
| juan.perez@uandresbello.edu | Juan Pérez | ~50 cursos |
| maria.gonzalez@uandresbello.edu | María González | ~35 cursos |
| carlos.rodriguez@uandresbello.edu | Carlos Rodríguez | ~50 cursos |
| ana.lopez@uandresbello.edu | Ana López | ~42 cursos |
| pedro.martinez@uandresbello.edu | Pedro Martínez | ~50 cursos |
| estudiante@estudiante.com | Estudiante Demo | 51 cursos |
| m.mendezfernandez@uandresbello.edu | Marino Simonin | 50 cursos |

---

## 🐛 Problemas Resueltos

### 1. Errores de Compilación
**Problema**: Propiedades faltantes en modelos causaban errores CS1061.

**Solución**: Agregadas propiedades faltantes en:
- `Evaluacion.cs`: `EsLaboratorio3DLab`, `PreguntasMinimasLaboratorio`, `PreguntasPorSesionLaboratorio`
- `SesionEvaluacion.cs`: `Metadata`
- `Nota.cs`: `EsLaboratorio`

### 2. Error de Base de Datos
**Problema**: Conexión apuntaba a base de datos "neondb" inexistente.

**Solución**: Actualizado `.env` para usar base de datos "lmsdb".


**Solución**:
- Agregado origen a política CORS en backend
- Eliminada configuración CORS duplicada en `Program.cs`
- Actualizado CSP en `next.config.mjs`

### 4. Error 403 en Endpoints
**Problema**: Estudiantes recibían 403 al acceder a cursos y módulos.

**Solución**:
- Agregado rol "Alumno" a `[Authorize]` en controladores
- Actualizado `ModulosController.cs`
- Actualizado `CursosController.cs`
- Actualizado `UsuariosController.cs`

### 5. Bucle Infinito en Login
**Problema**: Redirección infinita entre `/` y `/student/dashboard`.

**Causa Raíz**:
- Dashboard verificaba rol `"Alumno"` exactamente
- Tokens JWT antiguos contenían rol `"Estudiante"`
- DB tenía usuarios con rol `"Estudiante"`

**Solución**:
1. Actualizar todos los roles en DB a `"Alumno"`
2. Actualizar controladores para usar `"Alumno"`
3. **IMPORTANTE**: Usuarios deben borrar localStorage y hacer login nuevamente

---

## 🚀 Instrucciones de Inicio

### Backend
```bash
cd /root/LMS-Backend-Laboratorio/LMSBackend.API
dotnet run
```
**Puerto**: 5253

### Frontend
```bash
cd /root/LMS-main
npm run dev:3000
```
**Puerto**: 3000

---

## 🔑 Borrar localStorage (IMPORTANTE)

Después de los cambios de roles, los usuarios **DEBEN** borrar el localStorage para obtener un token actualizado:

### Método 1: Desde el Navegador
1. Abre la consola del navegador (F12)
2. Ve a la pestaña **"Application"** o **"Almacenamiento"**
3. En **"Local Storage"** → `http://135.148.148.88:3000`
4. Haz clic derecho → **"Clear"** o borra las claves:
   - `token`
   - `userRole`
   - `userName`
   - `userEmail`
5. Refresca la página (F5)
6. Inicia sesión nuevamente

### Método 2: Desde la Consola del Navegador
```javascript
localStorage.clear();
location.reload();
```

### Método 3: Navegación Privada
Abre una ventana de incógnito/privada y accede a 

---

## 📊 Estructura de Datos

### Tabla: Cursos
```sql
SELECT "Nrc", "Nombre", "Activo" FROM "Cursos" LIMIT 5;
```

### Tabla: CursoUsuarios
```sql
SELECT * FROM "CursoUsuarios" WHERE "UsuarioId" = 'user-id' LIMIT 10;
```

### Tabla: AspNetUsers
```sql
SELECT "Email", "Rol" FROM "AspNetUsers" WHERE "Rol" = 'Alumno';
```

---

## 🔐 Seguridad

### JWT Token
- **Emisor**: LMSBackend
- **Audiencia**: LMSBackendClient
- **Claims incluidos**:
  - `sub`: Usuario ID
  - `email`: Email del usuario
  - `nombre`: Nombre del usuario
  - `role`: Rol del usuario

### Content Security Policy
- Script: `'self' 'unsafe-inline' 'unsafe-eval'`
- Style: `'self' 'unsafe-inline'`
- Connect: `'self' http://localhost:5253 http://135.148.148.88:5253 https:`
- Frame: `'none'`

---

## 📝 Endpoints Principales del Backend

### Autenticación
- `POST /api/usuarios/login` - Login de usuario
- `GET /api/usuarios/profile` - Perfil del usuario autenticado
- `GET /api/Profile/me` - Perfil completo del usuario

### Cursos
- `GET /api/cursos/asignados` - Cursos asignados al usuario
- `GET /api/Modulos/curso/{cursoId}` - Módulos de un curso
- `GET /api/Modulos/por-curso/{cursoId}` - Módulos por curso (alternativo)

### Usuarios
- `GET /api/usuarios?PaginaActual=1&CantidadPorPagina=10` - Listar usuarios

---

## 🧪 Testing

### Test de Login
```bash
curl -X POST http://135.148.148.88:5253/api/usuarios/login \
  -H "Content-Type: application/json" \
  -d '{"correo":"juan.perez@uandresbello.edu","contraseña":"Password123."}'
```

### Test de Cursos Asignados
```bash
curl http://135.148.148.88:5253/api/cursos/asignados \
  -H "Authorization: Bearer {TOKEN}"
```

### Test de Perfil
```bash
curl http://135.148.148.88:5253/api/Profile/me \
  -H "Authorization: Bearer {TOKEN}"
```

---

## 📦 Dependencias Principales

### Backend
- ASP.NET Core 9.0
- Entity Framework Core 9.0.4
- Npgsql (PostgreSQL)
- JWT Authentication
- ASP.NET Identity

### Frontend
- Next.js 15.2.4
- React 19
- TypeScript
- Tailwind CSS
- Radix UI Components

---

## 📧 Sistema de Notificaciones por Email

### Estado: ✅ Implementado y Funcional

El sistema de notificaciones automáticas por email está completamente implementado con 5 tipos de notificaciones:

| Notificación | Template | Color | Estado |
|-------------|----------|-------|--------|
| Nueva Evaluación | HTML Responsive | 🟢 Verde | ✅ |
| Recordatorio de Evaluación | HTML Responsive | 🟠 Naranja | ✅ |
| Nuevo Hilo en Foro | HTML Responsive | 🔵 Azul | ✅ |
| Respuesta en Hilo | HTML Responsive | 🟣 Púrpura | ✅ |
| Cierre de Evaluación | HTML Responsive | 🔴 Rojo | ✅ |

### Archivos Implementados
- **EmailService.cs** (`LMSBackend.API/Services/EmailService.cs`)
  - 5 métodos de notificación con templates HTML profesionales
  - Manejo robusto de errores (no bloquea operaciones principales)
  - Logging completo de envíos y errores

### Configuración SMTP
- **Proveedor**: Brevo (smtp-relay.brevo.com)
- **Puerto**: 587
- **Límite**: 300 emails/día (plan gratuito)
- **Configuración**: Ver `appsettings.json` y `.env`

### Documentación Completa
📖 Ver guía detallada de integración: **`NOTIFICACIONES_EMAIL_GUIA.md`**

Incluye:
- Ejemplos de uso de cada método
- Integración en controladores
- Servicio de fondo para recordatorios automáticos
- Manejo de errores y logs
- Tests manuales

---

## 🔐 Recuperación de Contraseña con SendGrid

### Estado: ✅ Implementado y Funcional

La funcionalidad de "Olvidar Contraseña" ahora envía emails profesionales usando **SendGrid** con template HTML responsive.

### Características
| Característica | Estado | Detalles |
|---------------|--------|----------|
| SendGrid Package | ✅ | v9.29.3 instalado |
| Método SendGrid | ✅ | `EnviarRecuperacionPasswordAsync()` |
| Template HTML | ✅ | Gradiente púrpura, responsive |
| URL Producción | ✅ | `http://135.148.148.88:3000` |
| Manejo de Errores | ✅ | Try-catch con logging |
| Endpoint | ✅ | `POST /api/usuarios/recuperar-password` |

### Endpoint de Recuperación
```bash
POST /api/usuarios/recuperar-password
Content-Type: application/json

{
  "correo": "usuario@uandresbello.edu"
}
```

**Respuestas**:
- `200 OK`: Email enviado exitosamente
- `404 Not Found`: Usuario no existe
- `500 Internal Server Error`: Error al enviar email

### Diseño del Email
- 🎨 Gradiente púrpura (#667eea → #764ba2)
- 🔐 Icono de candado
- 📱 Diseño responsive (max-width: 600px)
- ⚠️ Advertencias de seguridad
- 🔗 Botón CTA + enlace alternativo
- © Footer Universidad Andrés Bello

### Configuración SendGrid
**Archivo**: `appsettings.json`
```json

```

### Documentación Completa
📖 Ver guía detallada: **`RECUPERACION_PASSWORD_SENDGRID.md`**

Incluye:
- Ejemplos de uso con cURL y JavaScript
- Vista previa del email
- Troubleshooting completo
- Verificación de logs
- Checklist de pruebas

### Archivos Modificados
- ✅ `Services/EmailService.cs` - Nuevo método SendGrid
- ✅ `Services/UsuarioService.cs:117` - Integración SendGrid
- ✅ `LMSBackend.API.csproj` - SendGrid v9.29.3
- 📖 `RECUPERACION_PASSWORD_SENDGRID.md` - Documentación

---

## 🎯 Próximos Pasos

1. ✅ Borrar localStorage en todos los navegadores de prueba
2. ✅ Hacer login con nuevos tokens que contengan rol "Alumno"
3. ✅ Verificar que el dashboard de estudiantes funciona sin bucles
4. ✅ Sistema de notificaciones por email implementado
5. ✅ Recuperación de contraseña con SendGrid implementada
6. Integrar notificaciones en controladores (opcional)
7. Agregar módulos a los 50 cursos creados (opcional)
8. Agregar evaluaciones a los cursos (opcional)
9. Configurar foros para los cursos (opcional)

---

## 📞 Contacto y Soporte

Para más información sobre la configuración del sistema, consultar:
- Documentación de ASP.NET Core: https://docs.microsoft.com/aspnet/core
- Documentación de Next.js: https://nextjs.org/docs
- Documentación de PostgreSQL: https://www.postgresql.org/docs/

---

**Última actualización**: 2025-10-26
**Versión del Sistema**: 1.0
**Estado**: ✅ Producción
