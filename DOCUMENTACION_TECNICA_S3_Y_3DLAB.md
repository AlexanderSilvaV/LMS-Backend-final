# Documentación Técnica: AWS S3 y Sistema 3DLAB

## Índice

1. [AWS S3 y URLs Prefirmadas](#aws-s3-y-urls-prefirmadas)
   - [Conceptos Fundamentales](#conceptos-fundamentales)
   - [Configuración del Sistema](#configuración-del-sistema)
   - [Arquitectura de Seguridad](#arquitectura-de-seguridad)
   - [Estructura de Almacenamiento](#estructura-de-almacenamiento)
   - [Implementación Técnica](#implementación-técnica)
   - [Flujos de Trabajo](#flujos-de-trabajo)
2. [Integración 3DLAB](#integración-3dlab)
   - [Arquitectura del Sistema](#arquitectura-del-sistema)
   - [Autenticación y Seguridad](#autenticación-y-seguridad)
   - [Endpoints Disponibles](#endpoints-disponibles)
   - [Flujo de Integración Completo](#flujo-de-integración-completo)
   - [Modelos de Datos](#modelos-de-datos)

---

## AWS S3 y URLs Prefirmadas

### Conceptos Fundamentales

#### ¿Qué es una URL Prefirmada (Presigned URL)?

Una **URL prefirmada** es una URL temporal y firmada criptográficamente que permite el acceso controlado a un objeto en Amazon S3 sin necesidad de credenciales AWS o permisos públicos.

**Características principales:**

- **Temporal**: La URL expira después de un período definido (15 minutos en este sistema)
- **Segura**: Firmada con credenciales AWS usando HMAC-SHA256
- **Sin autenticación adicional**: El usuario final solo necesita la URL, no credenciales
- **Permisos específicos**: Puede permitir GET (descarga) o PUT (subida)
- **Privada**: Los archivos en S3 permanecen privados, solo accesibles mediante URLs prefirmadas

#### ¿Por qué usar URLs Prefirmadas?

**Seguridad:**
- Los archivos NO son públicos en S3
- No se exponen credenciales AWS al cliente
- Control granular de permisos (lectura/escritura)
- Expiración automática previene acceso indefinido

**Performance:**
- El cliente interactúa directamente con S3, no pasa por el backend
- Reduce carga en el servidor backend
- Mejor rendimiento para archivos grandes
- Escalabilidad automática de AWS S3

**Casos de uso en LMS:**
- Fotos de perfil de usuarios
- Portadas de cursos
- Materiales educativos
- Archivos adjuntos en publicaciones

---

### Configuración del Sistema

#### Archivo: `appsettings.json`

```json
{
  "AWS": {
    "AccessKey": "AKIAXXXXXXXXXX",
    "SecretKey": "xxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "Region": "us-east-2",
    "BucketName": "lms-s3-storage",
    "PresignedUrlExpirationMinutes": 15,
    "MaxFileSizeBytes": 52428800
  }
}
```

**Parámetros explicados:**

| Parámetro | Descripción | Valor |
|-----------|-------------|-------|
| `AccessKey` | Clave de acceso AWS IAM | Credencial de AWS |
| `SecretKey` | Clave secreta AWS IAM | Credencial de AWS |
| `Region` | Región del bucket S3 | `us-east-2` (Ohio) |
| `BucketName` | Nombre del bucket | `lms-s3-storage` |
| `PresignedUrlExpirationMinutes` | Tiempo de expiración de URLs | 15 minutos |
| `MaxFileSizeBytes` | Tamaño máximo de archivo | 50 MB (52428800 bytes) |

#### Permisos IAM Requeridos

El usuario IAM debe tener los siguientes permisos:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:GetObjectMetadata",
        "s3:CopyObject"
      ],
      "Resource": "arn:aws:s3:::lms-s3-storage/*"
    }
  ]
}
```

---

### Arquitectura de Seguridad

#### Principios de Seguridad Implementados

**1. Archivos Privados por Defecto**

```csharp
// ✅ NUNCA se usa CannedACL.PublicRead
// ❌ NO SE HACE: CannedACL = S3CannedACL.PublicRead

// Los archivos son privados, solo accesibles mediante URLs prefirmadas
```

**2. Encriptación Obligatoria en Reposo (SSE-S3/AES256)**

```csharp
var putRequest = new PutObjectRequest
{
    BucketName = _options.BucketName,
    Key = s3Key,
    InputStream = stream,
    ContentType = contentType,

    // ✅ SEGURIDAD: Encriptación AES-256 en reposo
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
};
```

**3. Validación de Archivos**

```csharp
// Validaciones implementadas en FileValidator:
- Extensiones permitidas (imágenes, documentos, videos)
- Tipos MIME válidos
- Tamaño máximo (50 MB)
- Sanitización de nombres de archivo
- Prevención de path traversal
```

**4. URLs con Expiración Temporal**

```csharp
Expires = DateTime.UtcNow.AddMinutes(15)  // 15 minutos
```

---

### Estructura de Almacenamiento

#### Organización de Carpetas en S3

```
lms-s3-storage/
├── uploads/
│   ├── {userId}/                    # ID único del usuario
│   │   ├── avatars/                 # Fotos de perfil
│   │   │   └── {fileId}.jpg        # Ejemplo: a1b2c3d4-...-5e6f.jpg
│   │   ├── documents/               # Documentos del usuario
│   │   │   └── {fileId}.pdf
│   │   └── materials/               # Materiales educativos
│   │       └── {fileId}.docx
│   └── system/                      # Archivos del sistema
│       └── course-covers/           # Portadas de cursos
│           └── {fileId}.png
```

**Ejemplo de rutas reales:**

```
uploads/f1d2a26a-a208-4451-9ceb-dadf92def7e0/avatars/8e9f1234-5678-90ab-cdef-1234567890ab.jpg
uploads/system/course-covers/1a2b3c4d-5e6f-7890-abcd-ef1234567890.png
```

#### Construcción de S3 Key

```csharp
private static string BuildS3Key(string userId, string fileId, string? subfolder)
{
    // Formato: uploads/{userId}/{subfolder}/{fileId}
    var basePath = $"uploads/{userId}";

    if (!string.IsNullOrWhiteSpace(subfolder))
    {
        return $"{basePath}/{subfolder}/{fileId}";
    }

    return $"{basePath}/{fileId}";
}
```

**Ejemplos:**

```csharp
// Avatar de usuario
BuildS3Key("user-123", "avatar.jpg", "avatars")
// Resultado: uploads/user-123/avatars/avatar.jpg

// Portada de curso
BuildS3Key("system", "cover.png", "course-covers")
// Resultado: uploads/system/course-covers/cover.png
```

---

### Implementación Técnica

#### Clase: `S3StorageService.cs`

**Ubicación:** `/root/LMS-Backend/LMSBackend.API/Services/S3StorageService.cs`

**Métodos principales:**

##### 1. Generar URL Prefirmada para SUBIR (PUT)

```csharp
public async Task<GenerateUploadUrlResponseDTO> GeneratePresignedUploadUrlAsync(
    string userId,
    GenerateUploadUrlRequestDTO request)
{
    // 1. Validar archivo (extensión, tipo MIME, tamaño)
    var (isValid, errorMessage) = FileValidator.ValidateFile(
        request.FileName,
        request.ContentType,
        request.FileSize);

    if (!isValid)
    {
        throw new ArgumentException(errorMessage);
    }

    // 2. Sanitizar nombre y generar ID único
    var sanitizedFileName = FileValidator.SanitizeFileName(request.FileName);
    var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
    var fileId = $"{Guid.NewGuid()}{extension}";

    // 3. Construir key S3
    var s3Key = BuildS3Key(userId, fileId, request.Subfolder);
    // Ejemplo: uploads/user-123/avatars/a1b2c3d4-...-5e6f.jpg

    // 4. Crear request de URL prefirmada
    var presignRequest = new GetPreSignedUrlRequest
    {
        BucketName = _options.BucketName,
        Key = s3Key,
        Verb = HttpVerb.PUT,  // Permiso de escritura
        Expires = DateTime.UtcNow.AddMinutes(15),
        ContentType = request.ContentType,
        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
    };

    // 5. Generar URL prefirmada
    var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);

    return new GenerateUploadUrlResponseDTO
    {
        FileId = fileId,
        PresignedUrl = presignedUrl,
        ExpiresIn = 900,  // 15 minutos en segundos
        S3Key = s3Key
    };
}
```

**Request DTO:**

```csharp
public class GenerateUploadUrlRequestDTO
{
    public string FileName { get; set; }        // "avatar.jpg"
    public string ContentType { get; set; }     // "image/jpeg"
    public long FileSize { get; set; }          // 2048576 (2 MB)
    public string? Subfolder { get; set; }      // "avatars"
}
```

**Response DTO:**

```csharp
public class GenerateUploadUrlResponseDTO
{
    public string FileId { get; set; }          // "a1b2c3d4-...-5e6f.jpg"
    public string PresignedUrl { get; set; }    // URL firmada para PUT
    public int ExpiresIn { get; set; }          // 900 segundos
    public string S3Key { get; set; }           // "uploads/user-123/avatars/a1b2c3d4.jpg"
}
```

##### 2. Generar URL Prefirmada para DESCARGAR (GET)

```csharp
public async Task<GenerateDownloadUrlResponseDTO> GeneratePresignedDownloadUrlAsync(
    string userId,
    GenerateDownloadUrlRequestDTO request)
{
    // 1. Construir key S3
    var s3Key = BuildS3Key(userId, request.FileId, request.Subfolder);

    // 2. Crear request de URL prefirmada para GET
    var presignRequest = new GetPreSignedUrlRequest
    {
        BucketName = _options.BucketName,
        Key = s3Key,
        Verb = HttpVerb.GET,  // Permiso de lectura
        Expires = DateTime.UtcNow.AddMinutes(15)
    };

    // 3. Opcional: Forzar descarga con nombre específico
    if (!string.IsNullOrWhiteSpace(request.DownloadFileName))
    {
        var sanitizedName = FileValidator.SanitizeFileName(request.DownloadFileName);
        presignRequest.ResponseHeaderOverrides.ContentDisposition =
            $"attachment; filename=\"{sanitizedName}\"";
    }

    // 4. Generar URL prefirmada
    var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);

    return new GenerateDownloadUrlResponseDTO
    {
        PresignedUrl = presignedUrl,
        ExpiresIn = 900  // 15 minutos en segundos
    };
}
```

**Request DTO:**

```csharp
public class GenerateDownloadUrlRequestDTO
{
    public string FileId { get; set; }          // "a1b2c3d4-...-5e6f.jpg"
    public string? Subfolder { get; set; }      // "avatars"
    public string? DownloadFileName { get; set; } // "mi-avatar.jpg" (opcional)
}
```

**Response DTO:**

```csharp
public class GenerateDownloadUrlResponseDTO
{
    public string PresignedUrl { get; set; }    // URL firmada para GET
    public int ExpiresIn { get; set; }          // 900 segundos
}
```

##### 3. Subida Directa (Método Tradicional)

```csharp
public async Task<(string fileId, string fileUrl)> SaveFileAsync(
    string userId,
    IFormFile archivo,
    string subfolder)
{
    // 1. Validar archivo
    var (isValid, errorMessage) = FileValidator.ValidateFile(
        archivo.FileName,
        archivo.ContentType,
        archivo.Length);

    if (!isValid)
    {
        throw new ArgumentException(errorMessage);
    }

    // 2. Generar fileId único
    var sanitizedFileName = FileValidator.SanitizeFileName(archivo.FileName);
    var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
    var fileId = $"{Guid.NewGuid()}{extension}";

    // 3. Construir key S3
    var s3Key = BuildS3Key(userId, fileId, subfolder);

    // 4. Subir archivo con encriptación
    using var stream = archivo.OpenReadStream();
    var putRequest = new PutObjectRequest
    {
        BucketName = _options.BucketName,
        Key = s3Key,
        InputStream = stream,
        ContentType = archivo.ContentType,
        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
    };

    await _s3Client.PutObjectAsync(putRequest);

    // 5. Generar URL prefirmada para descarga inmediata
    var downloadUrl = await GeneratePresignedDownloadUrlAsync(userId,
        new GenerateDownloadUrlRequestDTO
        {
            FileId = fileId,
            Subfolder = subfolder
        });

    return (fileId, downloadUrl.PresignedUrl);
}
```

##### 4. Eliminar Archivo

```csharp
public async Task DeleteFileAsync(string fileId, string subfolder)
{
    var s3Key = string.IsNullOrEmpty(subfolder)
        ? fileId
        : $"{subfolder}/{fileId}";

    var deleteRequest = new DeleteObjectRequest
    {
        BucketName = _options.BucketName,
        Key = s3Key
    };

    await _s3Client.DeleteObjectAsync(deleteRequest);
}
```

##### 5. Copiar Archivo

```csharp
public async Task<(string fileId, string fileUrl)> CopyFileAsync(
    string originalFileId,
    string subfolder)
{
    // 1. Construir keys
    var originalKey = string.IsNullOrEmpty(subfolder)
        ? originalFileId
        : $"{subfolder}/{originalFileId}";

    var extension = Path.GetExtension(originalFileId);
    var newFileId = $"{Guid.NewGuid()}{extension}";
    var newKey = string.IsNullOrEmpty(subfolder)
        ? newFileId
        : $"{subfolder}/{newFileId}";

    // 2. Copiar con encriptación
    var copyRequest = new CopyObjectRequest
    {
        SourceBucket = _options.BucketName,
        SourceKey = originalKey,
        DestinationBucket = _options.BucketName,
        DestinationKey = newKey,
        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
    };

    await _s3Client.CopyObjectAsync(copyRequest);

    // 3. Generar URL prefirmada para el nuevo archivo
    var downloadUrl = await GeneratePresignedDownloadUrlAsync("system",
        new GenerateDownloadUrlRequestDTO
        {
            FileId = newFileId,
            Subfolder = subfolder
        });

    return (newFileId, downloadUrl.PresignedUrl);
}
```

---

### Flujos de Trabajo

#### Flujo 1: Subida de Avatar de Usuario

**Diagrama:**

```
┌─────────┐         ┌─────────┐         ┌─────────┐         ┌─────────┐
│         │         │         │         │         │         │         │
│ Cliente │         │ Backend │         │   S3    │         │   DB    │
│         │         │         │         │         │         │         │
└────┬────┘         └────┬────┘         └────┬────┘         └────┬────┘
     │                   │                   │                   │
     │ 1. Solicitar URL  │                   │                   │
     │   prefirmada PUT  │                   │                   │
     │──────────────────>│                   │                   │
     │                   │                   │                   │
     │                   │ 2. Validar        │                   │
     │                   │    archivo        │                   │
     │                   │    (tamaño,       │                   │
     │                   │     tipo, ext)    │                   │
     │                   │                   │                   │
     │                   │ 3. Generar        │                   │
     │                   │    fileId único   │                   │
     │                   │                   │                   │
     │                   │ 4. Crear URL      │                   │
     │                   │    prefirmada     │                   │
     │                   │    (15 min)       │                   │
     │                   │                   │                   │
     │ 5. Devolver URL   │                   │                   │
     │    y fileId       │                   │                   │
     │<──────────────────│                   │                   │
     │                   │                   │                   │
     │ 6. PUT archivo    │                   │                   │
     │   directamente    │                   │                   │
     │───────────────────────────────────────>│                   │
     │                   │                   │                   │
     │                   │                   │ 7. Guardar con    │
     │                   │                   │    encriptación   │
     │                   │                   │    AES-256        │
     │                   │                   │                   │
     │ 8. 200 OK         │                   │                   │
     │<───────────────────────────────────────│                   │
     │                   │                   │                   │
     │ 9. Confirmar      │                   │                   │
     │    subida         │                   │                   │
     │──────────────────>│                   │                   │
     │                   │                   │                   │
     │                   │ 10. Guardar       │                   │
     │                   │     AvatarFileId  │                   │
     │                   │────────────────────────────────────────>│
     │                   │                   │                   │
     │ 11. Éxito         │                   │                   │
     │<──────────────────│                   │                   │
     │                   │                   │                   │
```

**Código del Cliente (JavaScript):**

```javascript
// Paso 1: Solicitar URL prefirmada
const response = await fetch('/api/storage/upload-url', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    fileName: 'avatar.jpg',
    contentType: 'image/jpeg',
    fileSize: file.size,
    subfolder: 'avatars'
  })
});

const { fileId, presignedUrl, expiresIn } = await response.json();

// Paso 2: Subir archivo directamente a S3
const uploadResponse = await fetch(presignedUrl, {
  method: 'PUT',
  headers: {
    'Content-Type': 'image/jpeg'
  },
  body: file
});

if (uploadResponse.ok) {
  // Paso 3: Confirmar subida al backend
  await fetch('/api/profile/avatar', {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ fileId })
  });
}
```

#### Flujo 2: Visualización de Portada de Curso

**Diagrama:**

```
┌─────────┐         ┌─────────┐         ┌─────────┐         ┌─────────┐
│         │         │         │         │         │         │         │
│ Cliente │         │ Backend │         │   S3    │         │   DB    │
│         │         │         │         │         │         │         │
└────┬────┘         └────┬────┘         └────┬────┘         └────┬────┘
     │                   │                   │                   │
     │ 1. GET /cursos    │                   │                   │
     │──────────────────>│                   │                   │
     │                   │                   │                   │
     │                   │ 2. Consultar      │                   │
     │                   │    cursos         │                   │
     │                   │────────────────────────────────────────>│
     │                   │                   │                   │
     │                   │<────────────────────────────────────────│
     │                   │  Cursos con       │                   │
     │                   │  PortadaFileId    │                   │
     │                   │                   │                   │
     │                   │ 3. Para cada curso│                   │
     │                   │    generar URL    │                   │
     │                   │    prefirmada GET │                   │
     │                   │                   │                   │
     │ 4. Lista de cursos│                   │                   │
     │    con URLs       │                   │                   │
     │    prefirmadas    │                   │                   │
     │<──────────────────│                   │                   │
     │                   │                   │                   │
     │ 5. GET presignedUrl│                   │                   │
     │    (para imagen)  │                   │                   │
     │───────────────────────────────────────>│                   │
     │                   │                   │                   │
     │                   │                   │ 6. Desencriptar   │
     │                   │                   │    y servir       │
     │                   │                   │                   │
     │ 7. Imagen (bytes) │                   │                   │
     │<───────────────────────────────────────│                   │
     │                   │                   │                   │
     │ 8. Renderizar     │                   │                   │
     │    en navegador   │                   │                   │
     │                   │                   │                   │
```

**Código del Servicio (CursoService.cs):**

```csharp
public async Task<List<CursoDTO>> BuscarCursosAsync(string? nombre)
{
    var query = _context.Cursos.AsQueryable();

    if (!string.IsNullOrWhiteSpace(nombre))
    {
        query = query.Where(c => c.Nombre.Contains(nombre));
    }

    var cursos = await query.ToListAsync();
    var cursosDTO = new List<CursoDTO>();

    // Para cada curso, generar URL prefirmada de portada
    foreach (var curso in cursos)
    {
        var portadaUrl = await BuildPortadaUrlAsync(curso);

        cursosDTO.Add(new CursoDTO
        {
            Nrc = curso.Nrc,
            Nombre = curso.Nombre,
            Descripcion = curso.Descripcion,
            Activo = curso.Activo,
            PortadaUrl = portadaUrl,  // URL prefirmada (15 min)
            PortadaActualizada = curso.PortadaActualizada
        });
    }

    return cursosDTO;
}

private async Task<string?> BuildPortadaUrlAsync(Curso curso)
{
    if (string.IsNullOrEmpty(curso.PortadaFileId))
    {
        return null;
    }

    try
    {
        if (_storageService is S3StorageService s3Service)
        {
            // Generar URL prefirmada para descarga
            var downloadResponse = await s3Service.GeneratePresignedDownloadUrlAsync(
                "system",
                new GenerateDownloadUrlRequestDTO
                {
                    FileId = curso.PortadaFileId,
                    Subfolder = "course-covers"
                }
            );

            return downloadResponse.PresignedUrl;
        }

        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al generar URL de portada para curso {Nrc}", curso.Nrc);
        return null;
    }
}
```

**Respuesta JSON:**

```json
[
  {
    "nrc": 1,
    "nombre": "Programación Avanzada",
    "descripcion": "Curso de programación avanzada",
    "activo": true,
    "portadaUrl": "https://lms-s3-storage.s3.us-east-2.amazonaws.com/uploads/system/course-covers/abc123.jpg?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAXXXXXXX%2F20250104%2Fus-east-2%2Fs3%2Faws4_request&X-Amz-Date=20250104T120000Z&X-Amz-Expires=900&X-Amz-SignedHeaders=host&X-Amz-Signature=xxxxxxxxxxxxx",
    "portadaActualizada": "2025-01-04T12:00:00Z"
  }
]
```

#### Flujo 3: Caché y Regeneración de URLs

**Problema:** Las URLs prefirmadas expiran en 15 minutos.

**Solución:** Regenerar URLs cuando se soliciten los datos.

**Implementación:**

```typescript
// Frontend: Verificar si URL está cerca de expirar
function isUrlExpiringSoon(url: string): boolean {
  try {
    const urlObj = new URL(url);
    const expiresParam = urlObj.searchParams.get('X-Amz-Expires');
    const dateParam = urlObj.searchParams.get('X-Amz-Date');

    if (!expiresParam || !dateParam) return true;

    const issueDate = new Date(dateParam);
    const expiresIn = parseInt(expiresParam);
    const expiryDate = new Date(issueDate.getTime() + expiresIn * 1000);

    // Si expira en menos de 5 minutos, considerarla expirada
    return (expiryDate.getTime() - Date.now()) < 5 * 60 * 1000;
  } catch {
    return true;
  }
}

// Refrescar URL si es necesario
async function getValidCourseImageUrl(courseId: number, currentUrl: string | null): Promise<string> {
  if (!currentUrl || isUrlExpiringSoon(currentUrl)) {
    // Re-fetch curso para obtener nueva URL prefirmada
    const response = await fetch(`/api/cursos/${courseId}`);
    const curso = await response.json();
    return curso.portadaUrl;
  }

  return currentUrl;
}
```

---

## Integración 3DLAB

### Arquitectura del Sistema

#### Visión General

La **integración 3DLAB** permite que aplicaciones Unity externas (desarrolladas por el equipo 3DLAB) consuman laboratorios del LMS y envíen resultados de evaluaciones.

**Componentes principales:**

1. **Usuario de integración**: Usuario fijo con credenciales hardcodeadas
2. **Autenticación dual**: JWT + API Key
3. **Endpoints especializados**: API versión v2 dedicada a 3DLAB
4. **Laboratorios 3DLAB**: Evaluaciones especiales sin límites
5. **Sesiones de evaluación**: Sistema de sesiones temporales para tracking

**Diagrama de arquitectura:**

```
┌──────────────────────────────────────────────────────────────┐
│                       Unity 3DLAB App                        │
│  (Aplicación desarrollada por equipo 3DLAB)                 │
└────────────┬─────────────────────────────────────────────────┘
             │
             │ HTTPS/REST API
             │
┌────────────▼─────────────────────────────────────────────────┐
│                   LMS Backend API                            │
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │     Laboratorio3DLabController                     │    │
│  │     /api/v2/3dlab/*                                │    │
│  │                                                     │    │
│  │  - GET  /preguntas                                 │    │
│  │  - GET  /evaluaciones/{id}/seleccion              │    │
│  │  - POST /resultados                                │    │
│  │  - POST /token                                     │    │
│  └──────────────────┬─────────────────────────────────┘    │
│                     │                                       │
│  ┌──────────────────▼─────────────────────────────────┐    │
│  │     Laboratorio3DLabService                        │    │
│  │  - Lógica de negocio                               │    │
│  │  - Selección aleatoria de preguntas                │    │
│  │  - Calificación automática                         │    │
│  │  - Gestión de sesiones                             │    │
│  └──────────────────┬─────────────────────────────────┘    │
│                     │                                       │
│  ┌──────────────────▼─────────────────────────────────┐    │
│  │     PostgreSQL Database                            │    │
│  │  - Evaluaciones                                    │    │
│  │  - Preguntas y Opciones                            │    │
│  │  - EvaluacionSesiones                              │    │
│  │  - Calificaciones                                  │    │
│  └────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

---

### Autenticación y Seguridad

#### Usuario de Integración 3DLAB

**Configuración en `appsettings.json`:**

```json
{
  "ThreeDLab": {
    "ApiKey": "3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION",
    "ServiceUserEmail": "3dlab@laboratorio.edu",
    "ServiceUserPassword": "3DLab@2025!Secure",
    "ServiceUserName": "Integración 3DLAB",
    "ServiceUserRut": "99999999-9",
    "ServiceUserRole": "Alumno"
  }
}
```

**Credenciales del usuario:**

| Campo | Valor |
|-------|-------|
| **Email** | `3dlab@laboratorio.edu` |
| **Contraseña** | `LMS3dLab` |
| **Nombre** | `Integración 3DLAB` |
| **RUT** | `99999999-9` |
| **Rol** | `Alumno` |
| **Usuario ID** | `f1d2a26a-a208-4451-9ceb-dadf92def7e0` |

#### Autenticación Dual

**1. JWT Token (para endpoints estándar)**

El usuario 3DLAB tiene un **token de larga duración** (10 años) en lugar de los 25 minutos estándar.

**Implementación en `TokenService.cs`:**

```csharp
public async Task<TokenResponseDTO> GenerarToken(Usuario usuario)
{
    // Identificar si es usuario 3DLAB
    var isThreeDLabIntegrationUser =
        string.Equals(usuario.Email, "3dlab@laboratorio.edu", StringComparison.OrdinalIgnoreCase)
        || string.Equals(usuario.Rut, "99999999-9", StringComparison.OrdinalIgnoreCase)
        || string.Equals(usuario.Nombre, "Integración 3DLAB", StringComparison.OrdinalIgnoreCase);

    // Usuario 3DLAB tiene token de larga duración (10 años), otros usuarios 25 minutos
    DateTime? expiresAt = isThreeDLabIntegrationUser
        ? DateTime.UtcNow.AddYears(10)
        : DateTime.UtcNow.AddMinutes(25);

    // Claims estándar
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id),
        new Claim(ClaimTypes.Email, usuario.Email),
        new Claim(ClaimTypes.Name, usuario.Nombre),
        new Claim(ClaimTypes.Role, usuario.Rol)
    };

    // Generar token JWT
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _issuer,
        audience: _audience,
        claims: claims,
        expires: expiresAt,
        signingCredentials: credentials
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return new TokenResponseDTO
    {
        Token = tokenString
    };
}
```

**Flujo de autenticación JWT:**

```bash
# 1. Login
curl -X POST 'http://localhost:5253/api/Usuarios/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "Correo": "3dlab@laboratorio.edu",
    "Contraseña": "LMS3dLab"
  }'

# Respuesta:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJmMWQyYTI2YS1hMjA4LTQ0NTEtOWNlYi1kYWRmOTJkZWY3ZTAiLCJlbWFpbCI6IjNkbGFiQGxhYm9yYXRvcmlvLmVkdSIsIm5vbWJyZSI6IkludGVncmFjacOzbiAzRExBQiIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFsdW1ubyIsImV4cCI6MjA3NzkzMjM2NiwiaXNzIjoiTE1TQmFja2VuZCIsImF1ZCI6IkxNU0JhY2tlbmRDbGllbnQifQ.xxxxxxxxxxxxx"
}

# 2. Usar token en requests
curl -X GET 'http://localhost:5253/api/Profile/me' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

**2. API Key (para endpoints /api/v2/3dlab/\*)**

Los endpoints específicos de 3DLAB usan autenticación por **API Key** en lugar de JWT.

**Implementación en `Laboratorio3DLabController.cs`:**

```csharp
private const string ApiKeyHeaderName = "X-3DLAB-Key";

private IActionResult? Autorizar()
{
    var apiKey = _options.ApiKey;

    if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) ||
        !string.Equals(providedKey, apiKey, StringComparison.Ordinal))
    {
        return Unauthorized("Header de autenticación inválido");
    }

    return null;
}

[HttpGet("preguntas")]
public async Task<IActionResult> ObtenerPreguntas([FromQuery] int evaluacionId)
{
    // Validar API Key
    var authResult = Autorizar();
    if (authResult != null)
    {
        return authResult;  // 401 Unauthorized
    }

    // Procesar request...
}
```

**Uso de API Key:**

```bash
curl -X GET 'http://localhost:5253/api/v2/3dlab/preguntas?evaluacionId=36' \
  -H 'X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION'
```

#### Comparación de Métodos de Autenticación

| Característica | JWT Token | API Key |
|----------------|-----------|---------|
| **Uso** | Endpoints estándar del LMS | Endpoints `/api/v2/3dlab/*` |
| **Header** | `Authorization: Bearer {token}` | `X-3DLAB-Key: {apiKey}` |
| **Duración** | 10 años (3DLAB), 25 min (otros) | Permanente hasta cambio manual |
| **Información** | Claims (userId, email, role) | Solo validación binaria |
| **Obtención** | Login con email/contraseña | Hardcodeada en configuración |

---

### Endpoints Disponibles

#### Base URL

```
http://135.148.148.88:5253/api/v2/3dlab
```

#### 1. GET /preguntas - Obtener Banco de Preguntas

**Descripción:** Retorna todas las preguntas disponibles para un laboratorio (banco completo).

**Autenticación:** API Key (`X-3DLAB-Key`)

**Request:**

```http
GET /api/v2/3dlab/preguntas?evaluacionId=36
Host: localhost:5253
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Parámetros:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `evaluacionId` | int | Sí | ID de la evaluación/laboratorio |

**Response (200 OK):**

```json
{
  "evaluacionId": 36,
  "cursoId": 1,
  "preguntas": [
    {
      "idPregunta": 125,
      "enunciado": "¿Cuál es el resultado de 2 + 2?",
      "opciones": {
        "a": "3",
        "b": "4",
        "c": "5",
        "d": "6"
      }
    },
    {
      "idPregunta": 126,
      "enunciado": "¿Cuál es la capital de Chile?",
      "opciones": {
        "a": "Valparaíso",
        "b": "Santiago",
        "c": "Concepción",
        "d": "La Serena"
      }
    }
  ]
}
```

**Errores:**

```json
// 401 Unauthorized
{
  "mensaje": "Header de autenticación inválido"
}

// 404 Not Found
{
  "mensaje": "Evaluación no encontrada o no es un laboratorio 3DLAB"
}
```

**Ejemplo cURL:**

```bash
curl -X GET 'http://135.148.148.88:5253/api/v2/3dlab/preguntas?evaluacionId=36' \
  -H 'X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION'
```

#### 2. GET /evaluaciones/{evaluacionId}/seleccion - Crear Sesión y Obtener Preguntas Aleatorias

**Descripción:** Crea una sesión de evaluación y retorna un subconjunto aleatorio de preguntas del banco.

**Autenticación:** API Key (`X-3DLAB-Key`)

**Request:**

```http
GET /api/v2/3dlab/evaluaciones/36/seleccion?alumnoId=f1d2a26a-a208-4451-9ceb-dadf92def7e0
Host: localhost:5253
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Parámetros:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `evaluacionId` | int | Sí | ID de la evaluación/laboratorio (ruta) |
| `alumnoId` | string | Sí | ID del usuario (UUID) |

**Response (200 OK):**

```json
{
  "sesionId": "qvwkH3TqU5ugCLy8raAp7qPyZhTy0E0T6Xioyhvn1PQ",
  "evaluacionId": 36,
  "cursoId": 1,
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "preguntas": [
    {
      "idPregunta": 129,
      "enunciado": "¿Cuántos meses tiene un año?",
      "opciones": {
        "a": "10",
        "b": "11",
        "c": "12",
        "d": "13"
      }
    },
    {
      "idPregunta": 132,
      "enunciado": "¿Cuál es el resultado de 3 × 3?",
      "opciones": {
        "a": "6",
        "b": "9",
        "c": "12",
        "d": "15"
      }
    }
  ]
}
```

**Campos importantes:**

- `sesionId`: Identificador único de la sesión (usar en POST /resultados)
- `preguntas`: Subconjunto aleatorio del banco (cantidad definida por `PreguntasPorSesionLaboratorio`)

**Ejemplo cURL:**

```bash
curl -X GET 'http://135.148.148.88:5253/api/v2/3dlab/evaluaciones/36/seleccion?alumnoId=f1d2a26a-a208-4451-9ceb-dadf92def7e0' \
  -H 'X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION'
```

#### 3. POST /resultados - Enviar Respuestas y Registrar Calificación

**Descripción:** Envía las respuestas del alumno, calcula la nota automáticamente y la registra en el sistema.

**Autenticación:** API Key (`X-3DLAB-Key`)

**Request:**

```http
POST /api/v2/3dlab/resultados
Host: localhost:5253
Content-Type: application/json
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION

{
  "sesionId": "qvwkH3TqU5ugCLy8raAp7qPyZhTy0E0T6Xioyhvn1PQ",
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "evaluacionId": 36,
  "respuestas": [
    {
      "preguntaId": 129,
      "seleccion": "c"
    },
    {
      "preguntaId": 132,
      "seleccion": "b"
    },
    {
      "preguntaId": 125,
      "seleccion": "b"
    }
  ]
}
```

**Body Parameters:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `sesionId` | string | Sí | ID de sesión obtenido en endpoint anterior |
| `alumnoId` | string | Sí | ID del usuario (UUID) |
| `evaluacionId` | int | Sí | ID de la evaluación/laboratorio |
| `respuestas` | array | Sí | Lista de respuestas |
| `respuestas[].preguntaId` | int | Sí | ID de la pregunta |
| `respuestas[].seleccion` | string | Sí | Opción seleccionada (a, b, c, o d) |

**Response (200 OK):**

```json
{
  "evaluacionId": 36,
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "puntajeObtenido": 30.0,
  "puntajeMaximo": 50.0,
  "calificacion": 60.0,
  "detalle": [
    {
      "preguntaId": 129,
      "enunciado": "¿Cuántos meses tiene un año?",
      "seleccion": "c",
      "esCorrecta": true,
      "puntosOtorgados": 10.0,
      "puntosPregunta": 10.0,
      "retroalimentacion": null
    },
    {
      "preguntaId": 132,
      "enunciado": "¿Cuál es el resultado de 3 × 3?",
      "seleccion": "b",
      "esCorrecta": true,
      "puntosOtorgados": 10.0,
      "puntosPregunta": 10.0,
      "retroalimentacion": null
    },
    {
      "preguntaId": 125,
      "enunciado": "¿Cuál es el resultado de 2 + 2?",
      "seleccion": "b",
      "esCorrecta": true,
      "puntosOtorgados": 10.0,
      "puntosPregunta": 10.0,
      "retroalimentacion": null
    }
  ],
  "fechaCalificacion": "2025-01-04T23:40:00Z"
}
```

**Cálculo de Calificación:**

```
Calificación (%) = (Puntaje Obtenido / Puntaje Máximo) × 100

Ejemplo:
- Puntaje Obtenido: 30 puntos
- Puntaje Máximo: 50 puntos
- Calificación: (30 / 50) × 100 = 60%
```

**Errores:**

```json
// 400 Bad Request - Validación
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Respuestas[0].Seleccion": [
      "The Seleccion field is required."
    ]
  }
}

// 404 Not Found
{
  "mensaje": "Sesión no encontrada"
}
```

**Ejemplo cURL:**

```bash
curl -X POST 'http://135.148.148.88:5253/api/v2/3dlab/resultados' \
  -H 'Content-Type: application/json' \
  -H 'X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION' \
  -d '{
    "sesionId": "qvwkH3TqU5ugCLy8raAp7qPyZhTy0E0T6Xioyhvn1PQ",
    "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
    "evaluacionId": 36,
    "respuestas": [
      {"preguntaId": 129, "seleccion": "c"},
      {"preguntaId": 132, "seleccion": "b"},
      {"preguntaId": 125, "seleccion": "b"}
    ]
  }'
```

#### 4. POST /token - Obtener Token JWT

**Descripción:** Obtiene un token JWT para el usuario de integración 3DLAB sin necesidad de proporcionar credenciales.

**Autenticación:** API Key (`X-3DLAB-Key`)

**Request:**

```http
POST /api/v2/3dlab/token
Host: localhost:5253
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Response (200 OK):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJmMWQyYTI2YS1hMjA4LTQ0NTEtOWNlYi1kYWRmOTJkZWY3ZTAiLCJlbWFpbCI6IjNkbGFiQGxhYm9yYXRvcmlvLmVkdSIsIm5vbWJyZSI6IkludGVncmFjacOzbiAzRExBQiIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFsdW1ubyIsImV4cCI6MjA3NzkzMjM2NiwiaXNzIjoiTE1TQmFja2VuZCIsImF1ZCI6IkxNU0JhY2tlbmRDbGllbnQifQ.xxxxxxxxxxxxx"
}
```

**Ejemplo cURL:**

```bash
curl -X POST 'http://135.148.148.88:5253/api/v2/3dlab/token' \
  -H 'X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION'
```

---

### Flujo de Integración Completo

#### Secuencia de Operaciones

```
┌──────────────┐
│   Unity App  │
└──────┬───────┘
       │
       │ 1. Autenticación (Opcional)
       │
       ▼
   POST /api/v2/3dlab/token
       │
       ├─────> Obtener JWT token
       │       (Expira en 10 años)
       │
       │ 2. Obtener Banco de Preguntas (Opcional)
       │
       ▼
   GET /api/v2/3dlab/preguntas?evaluacionId=36
       │
       ├─────> Recibir todas las preguntas
       │       (Para mostrar preview o info)
       │
       │ 3. Iniciar Sesión de Evaluación
       │
       ▼
   GET /api/v2/3dlab/evaluaciones/36/seleccion?alumnoId={userId}
       │
       ├─────> Crear sesión
       ├─────> Obtener sesionId
       └─────> Recibir preguntas aleatorias
       │
       │ 4. Usuario responde preguntas en Unity
       │    (Lógica de juego / simulación)
       │
       ▼
   POST /api/v2/3dlab/resultados
       │
       ├─────> Enviar respuestas
       ├─────> Calificación automática
       ├─────> Guardar nota en BD
       └─────> Recibir resultados detallados
       │
       ▼
   Mostrar calificación al usuario
   (Game Over / Results Screen)
```

#### Diagrama de Secuencia Detallado

```
Unity App          Backend API              Database
    │                   │                       │
    │ POST /token       │                       │
    │──────────────────>│                       │
    │                   │ Validar API Key       │
    │                   │                       │
    │   JWT Token       │                       │
    │<──────────────────│                       │
    │                   │                       │
    │ GET /preguntas    │                       │
    │──────────────────>│                       │
    │                   │ SELECT Preguntas      │
    │                   │──────────────────────>│
    │                   │<──────────────────────│
    │   Banco completo  │                       │
    │<──────────────────│                       │
    │                   │                       │
    │ GET /seleccion    │                       │
    │──────────────────>│                       │
    │                   │ SELECT Evaluacion     │
    │                   │──────────────────────>│
    │                   │<──────────────────────│
    │                   │                       │
    │                   │ Crear EvaluacionSesion│
    │                   │──────────────────────>│
    │                   │ INSERT sesion         │
    │                   │<──────────────────────│
    │                   │                       │
    │                   │ Selección aleatoria   │
    │                   │ de N preguntas        │
    │                   │                       │
    │   sesionId +      │                       │
    │   preguntas       │                       │
    │<──────────────────│                       │
    │                   │                       │
    │ [Usuario juega]   │                       │
    │                   │                       │
    │ POST /resultados  │                       │
    │──────────────────>│                       │
    │                   │ SELECT sesion         │
    │                   │──────────────────────>│
    │                   │<──────────────────────│
    │                   │                       │
    │                   │ Calificar respuestas  │
    │                   │ (comparar con DB)     │
    │                   │                       │
    │                   │ INSERT Calificacion   │
    │                   │──────────────────────>│
    │                   │<──────────────────────│
    │                   │                       │
    │   Resultados      │                       │
    │   detallados      │                       │
    │<──────────────────│                       │
    │                   │                       │
```

---

### Modelos de Datos

#### Estructura de Base de Datos

**Tablas principales:**

1. **Evaluaciones** (laboratorios)
2. **Preguntas**
3. **Opciones**
4. **EvaluacionSesiones** (sesiones de evaluación)
5. **Calificaciones** (notas registradas)

#### Evaluacion (Laboratorio 3DLAB)

```sql
CREATE TABLE "Evaluaciones" (
    "Id" INTEGER PRIMARY KEY,
    "Titulo" TEXT NOT NULL,
    "Descripcion" TEXT,
    "CursoId" INTEGER NOT NULL,
    "DocenteId" TEXT NOT NULL,
    "FechaCreacion" TIMESTAMP NOT NULL,
    "FechaInicio" TIMESTAMP NOT NULL,
    "FechaFin" TIMESTAMP,
    "TiempoLimiteMins" INTEGER NOT NULL DEFAULT 0,
    "Activa" BOOLEAN NOT NULL DEFAULT TRUE,
    "IntentosMaximos" INTEGER NOT NULL DEFAULT 0,
    "EsLaboratorio3DLab" BOOLEAN NOT NULL DEFAULT FALSE,
    "PreguntasMinimasLaboratorio" INTEGER,
    "PreguntasPorSesionLaboratorio" INTEGER,

    FOREIGN KEY ("CursoId") REFERENCES "Cursos"("Nrc"),
    FOREIGN KEY ("DocenteId") REFERENCES "AspNetUsers"("Id")
);
```

**Configuración del laboratorio de prueba:**

```csharp
new Evaluacion
{
    Id = 36,
    Titulo = "Laboratorio 3DLAB - Prueba de Integración",
    Descripcion = "Laboratorio de prueba para validar la integración con el sistema 3DLAB",
    CursoId = 1,  // Curso "3DLAB Laboratorio"
    FechaInicio = DateTime.UtcNow,
    FechaFin = DateTime.UtcNow.AddYears(10),  // Válido por 10 años
    TiempoLimiteMins = 0,  // ✅ Sin límite de tiempo
    IntentosMaximos = 0,   // ✅ Intentos ilimitados
    EsLaboratorio3DLab = true,  // ✅ Marca como laboratorio 3DLAB
    PreguntasMinimasLaboratorio = 5,
    PreguntasPorSesionLaboratorio = 5,  // 5 preguntas aleatorias por sesión
    Activa = true
}
```

#### Pregunta

```sql
CREATE TABLE "Preguntas" (
    "Id" INTEGER PRIMARY KEY,
    "EvaluacionId" INTEGER NOT NULL,
    "Texto" TEXT NOT NULL,
    "Puntos" DECIMAL(5,2) NOT NULL DEFAULT 10.00,
    "Orden" INTEGER NOT NULL,

    FOREIGN KEY ("EvaluacionId") REFERENCES "Evaluaciones"("Id") ON DELETE CASCADE
);
```

**Ejemplo:**

```csharp
new Pregunta
{
    Id = 125,
    EvaluacionId = 36,
    Texto = "¿Cuál es el resultado de 2 + 2?",
    Puntos = 10,
    Orden = 1
}
```

#### Opcion

```sql
CREATE TABLE "Opciones" (
    "Id" INTEGER PRIMARY KEY,
    "PreguntaId" INTEGER NOT NULL,
    "Texto" TEXT NOT NULL,
    "EsCorrecta" BOOLEAN NOT NULL DEFAULT FALSE,
    "Orden" INTEGER NOT NULL,

    FOREIGN KEY ("PreguntaId") REFERENCES "Preguntas"("Id") ON DELETE CASCADE
);
```

**Ejemplo:**

```csharp
new List<Opcion>
{
    new Opcion { Texto = "3", EsCorrecta = false, Orden = 1 },
    new Opcion { Texto = "4", EsCorrecta = true, Orden = 2 },  // ✅ Correcta
    new Opcion { Texto = "5", EsCorrecta = false, Orden = 3 },
    new Opcion { Texto = "6", EsCorrecta = false, Orden = 4 }
}
```

#### EvaluacionSesion

```sql
CREATE TABLE "EvaluacionSesiones" (
    "Id" TEXT PRIMARY KEY,  -- Base64 GUID
    "EvaluacionId" INTEGER NOT NULL,
    "AlumnoId" TEXT NOT NULL,
    "FechaCreacion" TIMESTAMP NOT NULL,

    FOREIGN KEY ("EvaluacionId") REFERENCES "Evaluaciones"("Id"),
    FOREIGN KEY ("AlumnoId") REFERENCES "AspNetUsers"("Id")
);
```

**Propósito:**
- Tracking de intentos
- Asociar respuestas con sesión específica
- Prevenir duplicación de resultados

**Generación de sesionId:**

```csharp
// Generar ID único (Base64 del GUID)
var sesionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
    .Replace("/", "_")
    .Replace("+", "-")
    .TrimEnd('=');

// Ejemplo: "qvwkH3TqU5ugCLy8raAp7qPyZhTy0E0T6Xioyhvn1PQ"
```

#### Calificacion

```sql
CREATE TABLE "Calificaciones" (
    "Id" INTEGER PRIMARY KEY,
    "EvaluacionId" INTEGER NOT NULL,
    "AlumnoId" TEXT NOT NULL,
    "Nota" DECIMAL(5,2) NOT NULL,
    "PuntajeObtenido" DECIMAL(10,2) NOT NULL,
    "PuntajeMaximo" DECIMAL(10,2) NOT NULL,
    "FechaCalificacion" TIMESTAMP NOT NULL,
    "Observaciones" TEXT,

    FOREIGN KEY ("EvaluacionId") REFERENCES "Evaluaciones"("Id"),
    FOREIGN KEY ("AlumnoId") REFERENCES "AspNetUsers"("Id")
);
```

**Cálculo de nota:**

```csharp
// Porcentaje de preguntas correctas
decimal porcentaje = (puntajeObtenido / puntajeMaximo) * 100;

// Insertar calificación
new Calificacion
{
    EvaluacionId = 36,
    AlumnoId = "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
    Nota = porcentaje,  // 60.0
    PuntajeObtenido = 30.0m,
    PuntajeMaximo = 50.0m,
    FechaCalificacion = DateTime.UtcNow
}
```

---

### Configuración Inicial del Sistema

#### Inicialización Automática

El sistema inicializa automáticamente los datos necesarios al iniciar el backend.

**Archivo:** `ThreeDLabDataInitializer.cs`

**Proceso de inicialización:**

1. **Verificar usuario de integración**
2. **Crear o verificar curso "3DLAB Laboratorio"** (NRC: 1)
3. **Inscribir usuario 3DLAB en el curso**
4. **Crear o verificar laboratorio de prueba** (ID: 36)
5. **Crear 8 preguntas de ejemplo**

**Log de inicialización:**

```
info: ThreeDLabDataInitializer[0]
      3DLAB: Usuario de integración verificado

info: ThreeDLabDataInitializer[0]
      3DLAB: Curso '3DLAB Laboratorio' ya existe (NRC: 1)

info: ThreeDLabDataInitializer[0]
      3DLAB: Usuario '3dlab@laboratorio.edu' ya está inscrito en el curso '3DLAB Laboratorio'

info: ThreeDLabDataInitializer[0]
      3DLAB: Ya existe un laboratorio 3DLAB para el curso '3DLAB Laboratorio' (ID: 36)

info: ThreeDLabDataInitializer[0]
      3DLAB: Inicialización de datos completada. Curso NRC: 1, Evaluación ID: 36
```

#### Datos de Prueba

**Curso:**
- **NRC:** 1
- **Nombre:** 3DLAB Laboratorio
- **Descripción:** Curso de prueba para integración con 3DLAB

**Laboratorio:**
- **ID:** 36
- **Título:** Laboratorio 3DLAB - Prueba de Integración
- **Intentos:** Ilimitados
- **Tiempo límite:** Sin límite
- **Preguntas por sesión:** 5 (aleatorias del banco de 8)

**Preguntas (Banco de 8):**

| ID | Pregunta | Respuesta Correcta |
|----|----------|-------------------|
| 125 | ¿Cuál es el resultado de 2 + 2? | b) 4 |
| 126 | ¿Cuál es la capital de Chile? | b) Santiago |
| 127 | ¿Cuántos días tiene una semana? | c) 7 |
| 128 | ¿Cuál es el color del cielo en un día despejado? | b) Azul |
| 129 | ¿Cuántos meses tiene un año? | c) 12 |
| 130 | ¿Cuál es el resultado de 10 / 2? | c) 5 |
| 131 | ¿En qué continente se encuentra Chile? | b) América |
| 132 | ¿Cuál es el resultado de 3 × 3? | b) 9 |

---

### Consideraciones de Producción

#### Seguridad

**1. Cambiar API Key en Producción**

```json
{
  "ThreeDLab": {
    "ApiKey": "TU-API-KEY-SUPER-SEGURA-Y-COMPLEJA-AQUI"
  }
}
```

**2. Usar HTTPS**

Asegurar que todas las comunicaciones usen HTTPS en producción.

**3. Rate Limiting**

Implementar límite de requests para prevenir abuso:

```csharp
// Ejemplo con AspNetCoreRateLimit
services.AddInMemoryRateLimiting();
services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*/api/v2/3dlab/*",
            Limit = 100,
            Period = "1m"
        }
    };
});
```

#### Monitoreo

**1. Logging de Actividad**

Implementar logs detallados:

```csharp
_logger.LogInformation("3DLAB: Sesión creada - EvaluacionId: {EvaluacionId}, AlumnoId: {AlumnoId}, SesionId: {SesionId}",
    evaluacionId, alumnoId, sesionId);

_logger.LogInformation("3DLAB: Calificación registrada - Nota: {Nota}%, Puntaje: {Obtenido}/{Maximo}",
    calificacion, puntajeObtenido, puntajeMaximo);
```

**2. Métricas de Uso**

Registrar métricas clave:
- Número de sesiones creadas por día
- Tiempo promedio de completación
- Tasa de éxito por pregunta
- Distribución de calificaciones

#### Escalabilidad

**1. Caché de Preguntas**

```csharp
// Implementar caché distribuido para banco de preguntas
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
});
```

**2. Separación de Servicios**

Considerar microservicios para alta demanda:
- Servicio de autenticación
- Servicio de evaluaciones
- Servicio de calificaciones

---

## Resumen

### AWS S3

- **URLs Prefirmadas**: Acceso temporal y seguro sin credenciales públicas
- **Encriptación AES-256**: Todos los archivos encriptados en reposo
- **Expiración 15 minutos**: Balance entre seguridad y usabilidad
- **Estructura organizada**: `uploads/{userId}/{subfolder}/{fileId}`

### Integración 3DLAB

- **Usuario fijo**: Email `3dlab@laboratorio.edu`, token de 10 años
- **Autenticación dual**: JWT para endpoints estándar, API Key para endpoints 3DLAB
- **4 Endpoints principales**: `/preguntas`, `/seleccion`, `/resultados`, `/token`
- **Laboratorio especial**: Sin límites de tiempo, intentos ilimitados, preguntas aleatorias
- **Calificación automática**: Sistema calcula y registra notas automáticamente

---

**Fecha de creación:** 2025-01-04
**Última actualización:** 2025-01-04
**Versión:** 1.0
