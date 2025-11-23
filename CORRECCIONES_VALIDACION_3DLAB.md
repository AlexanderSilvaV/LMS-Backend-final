# Correcciones Aplicadas - Validación de Laboratorios 3DLAB

## Problema Identificado

Al intentar crear un laboratorio 3DLAB desde el frontend, se producía un error 400:

```json
{
  "errors": {
    "IntentosMaximos": ["Los intentos máximos deben estar entre 1 y 10"],
    "TiempoLimiteMins": ["El tiempo límite debe estar entre 5 y 300 minutos"]
  }
}
```

**Causa**: Las validaciones en los DTOs y constraints de base de datos no permitían valores de 0, que son necesarios para laboratorios 3DLAB (intentos ilimitados y sin límite de tiempo).

---

## Correcciones Aplicadas

### 1. **Validación Personalizada Creada**

**Archivo**: `LMSBackend.API/Validation/RangoCondicionalAttribute.cs`

- Creado atributo de validación personalizado `RangoCondicionalAttribute`
- Permite un valor especial (como 0) cuando se cumple una condición
- Valida rangos normales cuando la condición es false

### 2. **DTOs Actualizados**

#### `EvaluacionCreacionDTO.cs`
- Agregadas propiedades:
  - `EsLaboratorio3DLab` (bool)
  - `PreguntasMinimasLaboratorio` (int)
  - `PreguntasPorSesionLaboratorio` (int)
- Validaciones actualizadas:
  - `TiempoLimiteMins`: Permite 0 si `EsLaboratorio3DLab = true`
  - `IntentosMaximos`: Permite 0 si `EsLaboratorio3DLab = true`

#### `EvaluacionEdicionDTO.cs`
- Mismas propiedades y validaciones agregadas

#### `EvaluacionDTO.cs`
- Agregadas propiedades de laboratorio 3DLAB para respuestas

### 3. **Servicio de Evaluaciones Actualizado**

**Archivo**: `Services/EvaluacionService.cs`

#### Método `CrearEvaluacionAsync`:
- Validación de preguntas modificada: Permite crear evaluaciones sin preguntas iniciales si `EsLaboratorio3DLab = true`
- Mapeo de propiedades 3DLAB agregado al crear evaluación
- Código actualizado:
```csharp
// Antes: Siempre requería preguntas
if (dto.Preguntas == null || !dto.Preguntas.Any())

// Después: Permite crear sin preguntas para 3DLAB
if (!dto.EsLaboratorio3DLab && (dto.Preguntas == null || !dto.Preguntas.Any()))
```

#### Método `ActualizarEvaluacionAsync`:
- Mapeo de propiedades 3DLAB agregado

#### Mapeos de DTOs:
- Todos los mapeos a `EvaluacionDTO` actualizados para incluir propiedades 3DLAB

### 4. **Constraints de Base de Datos Actualizados**

#### ApplicationDbContext.cs
```csharp
modelBuilder.Entity<Evaluacion>()
    .ToTable(t => {
        t.HasCheckConstraint("CK_Evaluacion_TiempoLimite",
            "((\"EsLaboratorio3DLab\" = TRUE AND \"TiempoLimiteMins\" = 0) OR (\"EsLaboratorio3DLab\" = FALSE AND \"TiempoLimiteMins\" > 0 AND \"TiempoLimiteMins\" <= 300))");
        t.HasCheckConstraint("CK_Evaluacion_IntentosMaximos",
            "((\"EsLaboratorio3DLab\" = TRUE AND \"IntentosMaximos\" = 0) OR (\"EsLaboratorio3DLab\" = FALSE AND \"IntentosMaximos\" > 0 AND \"IntentosMaximos\" <= 10))");
    });
```

#### Script SQL Ejecutado
**Archivo**: `update_3dlab_constraints.sql`

Constraints aplicados directamente a la base de datos:
- Permite `IntentosMaximos = 0` cuando `EsLaboratorio3DLab = TRUE`
- Permite `TiempoLimiteMins = 0` cuando `EsLaboratorio3DLab = TRUE`
- Mantiene validaciones originales cuando `EsLaboratorio3DLab = FALSE`

---

## Comportamiento Resultante

### Para Evaluaciones Normales (EsLaboratorio3DLab = FALSE)
- `IntentosMaximos`: Debe estar entre 1 y 10
- `TiempoLimiteMins`: Debe estar entre 5 y 300
- Debe tener al menos 1 pregunta

### Para Laboratorios 3DLAB (EsLaboratorio3DLab = TRUE)
- `IntentosMaximos`: Debe ser exactamente 0 (intentos ilimitados)
- `TiempoLimiteMins`: Debe ser exactamente 0 (sin límite de tiempo)
- Puede crearse sin preguntas iniciales
- `PreguntasMinimasLaboratorio`: Define cuántas preguntas mínimas debe tener
- `PreguntasPorSesionLaboratorio`: Define cuántas preguntas se seleccionan por sesión

---

## Archivos Modificados

1. ✅ `Validation/RangoCondicionalAttribute.cs` (creado)
2. ✅ `DTOs/EvaluacionCreacionDTO.cs` (actualizado)
3. ✅ `DTOs/EvaluacionEdicionDTO.cs` (actualizado)
4. ✅ `DTOs/EvaluacionDTO.cs` (actualizado)
5. ✅ `Services/EvaluacionService.cs` (actualizado)
6. ✅ `Data/ApplicationDbContext.cs` (actualizado)
7. ✅ `update_3dlab_constraints.sql` (creado y ejecutado)

---

## Pruebas para Frontend

Ahora el frontend puede enviar:

```json
{
  "titulo": "Laboratorio 3DLAB Test",
  "descripcion": "Descripción del laboratorio",
  "cursoId": 99999,
  "fechaInicio": "2025-01-27T00:00:00",
  "fechaFin": "2025-12-31T23:59:59",
  "tiempoLimiteMins": 0,
  "intentosMaximos": 0,
  "esLaboratorio3DLab": true,
  "preguntasMinimasLaboratorio": 5,
  "preguntasPorSesionLaboratorio": 5,
  "preguntas": []
}
```

Y el backend:
✅ Aceptará la validación
✅ Creará la evaluación correctamente
✅ Permitirá agregar preguntas posteriormente

---

## Estado Final

✅ **Aplicación corriendo sin errores**
✅ **Constraints actualizados en base de datos**
✅ **Validaciones funcionando correctamente**
✅ **DTOs con todas las propiedades necesarias**
✅ **Servicios mapeando correctamente**

La aplicación está lista para crear laboratorios 3DLAB desde el frontend.

---

## Comandos para Reiniciar (si es necesario)

```bash
# Detener procesos
pkill -f "dotnet.*LMSBackend"

# Iniciar aplicación
cd /root/elweno/LMS-Backend-Laboratorio/LMSBackend.API
dotnet run

# Ver logs de 3DLAB
dotnet run 2>&1 | grep "3DLAB"
```

---

**Fecha**: 27 de Octubre de 2025
**Estado**: ✅ Resuelto y Operacional
