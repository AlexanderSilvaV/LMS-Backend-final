# 📋 INSTRUCCIONES DE INTEGRACIÓN - 3DLAB

## Información General del Sistema

**Sistema:** LMS Backend - Sistema de Gestión de Aprendizaje
**URL Base:** `http://135.148.148.88:5253`
**Versión API:** v2
**Fecha:** Noviembre 2025

---

## 🔐 Credenciales de Integración

### Usuario de Servicio 3DLAB

```json
{
  "email": "3dlab@laboratorio.edu",
  "password": "LMS3dLab",
  "rut": "99999999-9",
  "nombre": "Integración 3DLAB",
  "rol": "Alumno"
}
```

### API Key

**Header requerido para endpoints 3DLAB:**
```
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

---

## 🎓 Identificadores del Entorno de Pruebas

### Curso Asignado
- **Nombre:** `3DLAB Laboratorio`
- **NRC:** `1`
- **Descripción:** Curso de prueba para integración con 3DLAB

### Laboratorio de Prueba
- **ID de Evaluación:** `36`
- **Título:** `Laboratorio 3DLAB - Prueba de Integración`
- **Preguntas Disponibles:** 8 preguntas de ejemplo
- **Puntaje Total:** 80 puntos (10 puntos por pregunta)
- **Configuración:**
  - ❌ Sin límite de intentos (`IntentosMaximos: 0`)
  - ❌ Sin límite de tiempo (`TiempoLimiteMins: 0`)
  - ✅ Válido por 10 años
  - 📊 Preguntas mínimas por sesión: 5
  - 🔢 Preguntas por sesión: 5

---

## 🔗 Endpoints Disponibles

### Base URL
```
http://135.148.148.88:5253/api/v2/3dlab
```

---

### 1. 🔑 Obtener Token de Autenticación

**Endpoint:** `POST /api/Usuarios/login`

**Descripción:** Obtiene un token JWT para autenticar al usuario 3DLAB. Este token tiene una duración de 10 años.

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "Correo": "3dlab@laboratorio.edu",
  "Contraseña": "LMS3dLab"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Uso del Token:**
Una vez obtenido, incluir en todos los requests subsecuentes:
```
Authorization: Bearer {token}
```

---

### 2. 📚 Obtener Cursos Asignados

**Endpoint:** `GET /api/curso-usuarios/cursos`

**Descripción:** Lista todos los cursos asignados al usuario 3DLAB.

**Headers:**
```
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "operacionExitosa": true,
  "mensaje": "Cursos obtenidos correctamente",
  "codigo": 200,
  "dato": [
    {
      "nrc": 1,
      "nombre": "3DLAB Laboratorio",
      "descripcion": "Curso de prueba para integración con 3DLAB",
      "activo": true,
      "portadaUrl": null
    }
  ]
}
```

---

### 3. 🧪 Listar Laboratorios de un Curso

**Endpoint:** `GET /api/Evaluaciones/curso/{cursoId}`

**Descripción:** Obtiene todas las evaluaciones (incluyendo laboratorios) de un curso específico.

**Headers:**
```
Authorization: Bearer {token}
```

**Parámetros de Ruta:**
- `cursoId` (int): NRC del curso (ejemplo: `1`)

**Ejemplo de Request:**
```
GET http://135.148.148.88:5253/api/Evaluaciones/curso/1
```

**Response (200 OK):**
```json
{
  "operacionExitosa": true,
  "codigo": 200,
  "dato": [
    {
      "id": 36,
      "titulo": "Laboratorio 3DLAB - Prueba de Integración",
      "descripcion": "Laboratorio de prueba para validar la integración",
      "cursoId": 1,
      "activa": true,
      "esLaboratorio3DLab": true,
      "intentosMaximos": 0,
      "tiempoLimiteMins": 0,
      "preguntasMinimasLaboratorio": 5,
      "preguntasPorSesionLaboratorio": 5
    }
  ]
}
```

---

### 4. ❓ Obtener Preguntas del Laboratorio

**Endpoint:** `GET /api/v2/3dlab/preguntas?evaluacionId={id}`

**Descripción:** Obtiene el pool completo de preguntas disponibles para un laboratorio.

**Headers:**
```
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Parámetros de Query:**
- `evaluacionId` (int): ID de la evaluación (ejemplo: `36`)

**Ejemplo de Request:**
```
GET http://135.148.148.88:5253/api/v2/3dlab/preguntas?evaluacionId=36
```

**Response (200 OK):**
```json
{
  "evaluacionId": 36,
  "titulo": "Laboratorio 3DLAB - Prueba de Integración",
  "preguntas": [
    {
      "id": 1,
      "texto": "¿Cuál es el resultado de 2 + 2?",
      "puntos": 10,
      "orden": 1,
      "opciones": [
        {
          "id": 1,
          "texto": "3",
          "esCorrecta": false,
          "orden": 1
        },
        {
          "id": 2,
          "texto": "4",
          "esCorrecta": true,
          "orden": 2
        },
        {
          "id": 3,
          "texto": "5",
          "esCorrecta": false,
          "orden": 3
        },
        {
          "id": 4,
          "texto": "6",
          "esCorrecta": false,
          "orden": 4
        }
      ]
    }
  ]
}
```

---

### 5. 🎲 Crear Sesión y Obtener Selección de Preguntas

**Endpoint:** `GET /api/v2/3dlab/evaluaciones/{evaluacionId}/seleccion?alumnoId={id}`

**Descripción:** Crea una nueva sesión de laboratorio y devuelve una selección aleatoria de preguntas según la configuración del laboratorio.

**Headers:**
```
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Parámetros:**
- `evaluacionId` (ruta): ID de la evaluación (ejemplo: `36`)
- `alumnoId` (query): ID del estudiante (ejemplo: `f1d2a26a-a208-4451-9ceb-dadf92def7e0`)

**Ejemplo de Request:**
```
GET http://135.148.148.88:5253/api/v2/3dlab/evaluaciones/36/seleccion?alumnoId=f1d2a26a-a208-4451-9ceb-dadf92def7e0
```

**Response (200 OK):**
```json
{
  "sesionId": 42,
  "evaluacionId": 36,
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "cantidadPreguntas": 5,
  "preguntas": [
    {
      "id": 1,
      "texto": "¿Cuál es el resultado de 2 + 2?",
      "puntos": 10,
      "opciones": [...]
    },
    {
      "id": 3,
      "texto": "¿Cuántos días tiene una semana?",
      "puntos": 10,
      "opciones": [...]
    }
  ]
}
```

---

### 6. ✅ Enviar Respuestas y Nota Final

**Endpoint:** `POST /api/v2/3dlab/resultados`

**Descripción:** Registra las respuestas del estudiante y la nota final obtenida en Unity.

**Headers:**
```
Content-Type: application/json
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Request Body:**
```json
{
  "sesionId": 42,
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "evaluacionId": 36,
  "notaFinal": 85.5,
  "respuestas": [
    {
      "preguntaId": 1,
      "opcionSeleccionadaId": 2,
      "esCorrecta": true
    },
    {
      "preguntaId": 3,
      "opcionSeleccionadaId": 7,
      "esCorrecta": true
    },
    {
      "preguntaId": 5,
      "opcionSeleccionadaId": 12,
      "esCorrecta": false
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "exito": true,
  "mensaje": "Resultados registrados correctamente",
  "notaRegistrada": 85.5,
  "intentoNumero": 1
}
```

---

## 📊 Flujo Completo de Integración

### Paso 1: Autenticación
```bash
curl -X POST http://135.148.148.88:5253/api/Usuarios/login \
  -H "Content-Type: application/json" \
  -d '{
    "Correo": "3dlab@laboratorio.edu",
    "Contraseña": "LMS3dLab"
  }'
```

**Guardar el token obtenido para los siguientes pasos.**

---

### Paso 2: Obtener Cursos
```bash
curl -X GET http://135.148.148.88:5253/api/curso-usuarios/cursos \
  -H "Authorization: Bearer {TOKEN}"
```

**Identificar el NRC del curso "3DLAB Laboratorio".**

---

### Paso 3: Listar Laboratorios
```bash
curl -X GET http://135.148.148.88:5253/api/Evaluaciones/curso/1 \
  -H "Authorization: Bearer {TOKEN}"
```

**Identificar el ID del laboratorio deseado.**

---

### Paso 4: Obtener Preguntas
```bash
curl -X GET "http://135.148.148.88:5253/api/v2/3dlab/preguntas?evaluacionId=36" \
  -H "X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION"
```

**Mostrar las preguntas en Unity.**

---

### Paso 5: Crear Sesión y Obtener Selección
```bash
curl -X GET "http://135.148.148.88:5253/api/v2/3dlab/evaluaciones/36/seleccion?alumnoId=f1d2a26a-a208-4451-9ceb-dadf92def7e0" \
  -H "X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION"
```

**Guardar el sesionId para registrar los resultados.**

---

### Paso 6: Enviar Resultados
```bash
curl -X POST http://135.148.148.88:5253/api/v2/3dlab/resultados \
  -H "Content-Type: application/json" \
  -H "X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION" \
  -d '{
    "sesionId": 42,
    "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
    "evaluacionId": 36,
    "notaFinal": 85.5,
    "respuestas": [
      {
        "preguntaId": 1,
        "opcionSeleccionadaId": 2,
        "esCorrecta": true
      }
    ]
  }'
```

---

## ⚠️ Notas Importantes

1. **Token de Larga Duración:** El token generado para el usuario 3DLAB tiene una validez de 10 años. No es necesario renovarlo frecuentemente.

2. **API Key:** Todos los endpoints `/api/v2/3dlab/*` requieren el header `X-3DLAB-Key` en lugar de autenticación JWT.

3. **Autenticación Mixta:**
   - Endpoints generales (`/api/Evaluaciones`, `/api/curso-usuarios`) usan JWT Bearer token
   - Endpoints específicos de 3DLAB (`/api/v2/3dlab/*`) usan API Key

4. **IDs de Usuario:** El `alumnoId` debe ser el ID del estudiante real que está realizando el laboratorio, NO el usuario de integración.

5. **Sesiones:** Cada intento de laboratorio crea una nueva sesión. Guardar el `sesionId` para registrar resultados.

6. **Intentos Ilimitados:** Los laboratorios 3DLAB no tienen límite de intentos.

---

## 🆘 Solución de Problemas

### Error 401 Unauthorized
- **Causa:** Token expirado o inválido
- **Solución:** Generar un nuevo token usando el endpoint de login

### Error 400 Bad Request
- **Causa:** Formato JSON incorrecto o campos faltantes
- **Solución:** Verificar que todos los campos requeridos estén presentes

### Error 404 Not Found
- **Causa:** ID de evaluación o curso no existe
- **Solución:** Verificar los IDs usando los endpoints de listado

### Error 500 Internal Server Error
- **Causa:** Error en el servidor
- **Solución:** Contactar al equipo de desarrollo del LMS

---

## 📞 Contacto y Soporte

Para dudas o problemas con la integración, contactar al equipo de desarrollo del LMS Backend.

---

**Generado el:** 04 de Noviembre de 2025
**Versión del Documento:** 1.0
