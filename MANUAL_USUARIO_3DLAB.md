# 📘 MANUAL DE USUARIO - INTEGRACIÓN 3DLAB CON LMS

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Arquitectura de la Integración](#arquitectura-de-la-integración)
3. [Conceptos Clave](#conceptos-clave)
4. [Flujo de Comunicación Completo](#flujo-de-comunicación-completo)
5. [Guía de Implementación en Unity](#guía-de-implementación-en-unity)
6. [Referencia de Endpoints](#referencia-de-endpoints)
7. [Modelos de Datos](#modelos-de-datos)
8. [Ejemplos de Código](#ejemplos-de-código)
9. [Mejores Prácticas](#mejores-prácticas)
10. [FAQ - Preguntas Frecuentes](#faq---preguntas-frecuentes)

---

## Introducción

Este manual describe cómo integrar el sistema 3DLAB desarrollado en Unity con el LMS (Learning Management System) Backend. La integración permite que los laboratorios virtuales en Unity obtengan preguntas dinámicas del LMS y registren automáticamente las respuestas y calificaciones de los estudiantes.

### ¿Qué es un Laboratorio 3DLAB?

Un laboratorio 3DLAB es una evaluación especial que:
- Se ejecuta externamente en Unity (no dentro del navegador del LMS)
- No tiene límite de intentos
- No tiene límite de tiempo
- No tiene fecha de entrega específica
- Obtiene sus preguntas dinámicamente del sistema LMS
- Envía automáticamente los resultados al LMS

### Diferencia con Evaluaciones Normales

| Característica | Evaluación Normal | Laboratorio 3DLAB |
|----------------|-------------------|-------------------|
| **Plataforma** | Web (navegador) | Unity (aplicación externa) |
| **Intentos** | Limitados | Ilimitados |
| **Tiempo** | Con límite | Sin límite |
| **Preguntas** | Fijas en el momento | Dinámicas desde el LMS |
| **Calificación** | Automática en el sistema | Enviada desde Unity |

---

## Arquitectura de la Integración

```
┌─────────────────────┐
│                     │
│   UNITY (3DLAB)     │
│                     │
│  ┌───────────────┐  │
│  │ Laboratorio   │  │
│  │ Virtual       │  │
│  └───────────────┘  │
│         │           │
│         │ HTTP/REST │
│         ▼           │
└─────────┼───────────┘
          │
          │ API Calls
          │
┌─────────▼───────────┐
│                     │
│   LMS BACKEND       │
│  (ASP.NET Core)     │
│                     │
│  ┌───────────────┐  │
│  │ PostgreSQL DB │  │
│  │               │  │
│  │ - Usuarios    │  │
│  │ - Cursos      │  │
│  │ - Evaluaciones│  │
│  │ - Preguntas   │  │
│  │ - Resultados  │  │
│  └───────────────┘  │
│                     │
└─────────────────────┘
```

---

## Conceptos Clave

### 1. Usuario de Integración
- **Email:** `3dlab@laboratorio.edu`
- **Propósito:** Usuario de servicio dedicado para la integración
- **Rol:** Alumno (con permisos especiales)
- **Token:** Duración de 10 años

### 2. API Key
- **Header:** `X-3DLAB-Key`
- **Valor:** `3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION`
- **Uso:** Autenticación para endpoints específicos de 3DLAB

### 3. Curso de Pruebas
- **Nombre:** `3DLAB Laboratorio`
- **NRC:** `1`
- **Propósito:** Ambiente de pruebas dedicado

### 4. Sesión de Laboratorio
- **Definición:** Instancia única de un intento de laboratorio
- **ID:** `sesionId` - Identificador único generado al iniciar
- **Propósito:** Rastrear intentos individuales de estudiantes

### 5. Pool de Preguntas
- **Definición:** Conjunto completo de preguntas disponibles para un laboratorio
- **Selección:** El sistema puede seleccionar un subconjunto aleatorio

---

## Flujo de Comunicación Completo

### Diagrama de Secuencia

```
Unity 3DLAB          LMS Backend          Base de Datos
     │                    │                      │
     │  1. POST /login    │                      │
     ├───────────────────>│                      │
     │                    │  Validar credenciales│
     │                    ├─────────────────────>│
     │                    │<─────────────────────┤
     │  Token JWT         │                      │
     │<───────────────────┤                      │
     │                    │                      │
     │  2. GET /cursos    │                      │
     ├───────────────────>│                      │
     │                    │  Buscar cursos       │
     │                    ├─────────────────────>│
     │  Lista de cursos   │<─────────────────────┤
     │<───────────────────┤                      │
     │                    │                      │
     │  3. GET /preguntas │                      │
     ├───────────────────>│                      │
     │                    │  Obtener preguntas   │
     │                    ├─────────────────────>│
     │  Preguntas JSON    │<─────────────────────┤
     │<───────────────────┤                      │
     │                    │                      │
     │ [Usuario responde en Unity]               │
     │                    │                      │
     │  4. POST /resultados│                     │
     ├───────────────────>│                      │
     │                    │  Guardar resultados  │
     │                    ├─────────────────────>│
     │                    │<─────────────────────┤
     │  Confirmación      │                      │
     │<───────────────────┤                      │
```

### Explicación Paso a Paso

#### **Paso 1: Autenticación Inicial**
Cuando Unity inicia, debe autenticarse con el LMS para obtener un token JWT.

**¿Cuándo?** Al iniciar la aplicación Unity o al comenzar una sesión de laboratorio.

**¿Cómo?** Enviar credenciales del usuario 3DLAB al endpoint de login.

#### **Paso 2: Obtener Cursos Disponibles**
Una vez autenticado, Unity solicita la lista de cursos asignados al usuario.

**¿Por qué?** Para saber qué cursos están disponibles y sus identificadores (NRC).

#### **Paso 3: Listar Laboratorios del Curso**
Unity solicita todos los laboratorios disponibles en un curso específico.

**¿Por qué?** Para que el estudiante pueda seleccionar qué laboratorio realizar.

#### **Paso 4: Obtener Preguntas del Laboratorio**
Antes de iniciar, Unity obtiene todas las preguntas disponibles o una selección aleatoria.

**Opciones:**
- Pool completo: `/api/v2/3dlab/preguntas?evaluacionId=36`
- Selección aleatoria: `/api/v2/3dlab/evaluaciones/36/seleccion?alumnoId=xxx`

#### **Paso 5: Presentar Laboratorio en Unity**
Unity muestra las preguntas en el entorno 3D y espera las respuestas del estudiante.

**Responsabilidad de Unity:**
- Renderizar las preguntas de forma interactiva
- Capturar las respuestas del usuario
- Calcular la nota basándose en respuestas correctas

#### **Paso 6: Enviar Resultados al LMS**
Una vez completado el laboratorio, Unity envía las respuestas y la nota final.

**¿Qué enviar?**
- ID de la sesión (si se creó una)
- ID del estudiante
- ID de la evaluación
- Nota final calculada
- Respuestas individuales con opciones seleccionadas

---

## Guía de Implementación en Unity

### 1. Configuración Inicial

```csharp
// Configuración del LMS
public class LMSConfig
{
    public const string BASE_URL = "http://135.148.148.88:5253";
    public const string API_KEY = "3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION";

    // Credenciales del usuario de integración
    public const string SERVICE_EMAIL = "3dlab@laboratorio.edu";
    public const string SERVICE_PASSWORD = "LMS3dLab";
}
```

### 2. Clase de Autenticación

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class LMSAuthManager : MonoBehaviour
{
    private string jwtToken;

    [System.Serializable]
    public class LoginRequest
    {
        public string Correo;
        public string Contraseña;
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string token;
    }

    public IEnumerator Authenticate()
    {
        string url = LMSConfig.BASE_URL + "/api/Usuarios/login";

        LoginRequest loginData = new LoginRequest
        {
            Correo = LMSConfig.SERVICE_EMAIL,
            Contraseña = LMSConfig.SERVICE_PASSWORD
        };

        string jsonData = JsonUtility.ToJson(loginData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                jwtToken = response.token;
                Debug.Log("Autenticación exitosa. Token obtenido.");
            }
            else
            {
                Debug.LogError("Error de autenticación: " + request.error);
            }
        }
    }

    public string GetToken()
    {
        return jwtToken;
    }
}
```

### 3. Clase para Obtener Preguntas

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class LMSLaboratorioManager : MonoBehaviour
{
    [System.Serializable]
    public class Opcion
    {
        public int id;
        public string texto;
        public bool esCorrecta;
        public int orden;
    }

    [System.Serializable]
    public class Pregunta
    {
        public int id;
        public string texto;
        public int puntos;
        public int orden;
        public List<Opcion> opciones;
    }

    [System.Serializable]
    public class PreguntasResponse
    {
        public int evaluacionId;
        public string titulo;
        public List<Pregunta> preguntas;
    }

    public IEnumerator ObtenerPreguntas(int evaluacionId)
    {
        string url = $"{LMSConfig.BASE_URL}/api/v2/3dlab/preguntas?evaluacionId={evaluacionId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("X-3DLAB-Key", LMSConfig.API_KEY);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                PreguntasResponse response = JsonUtility.FromJson<PreguntasResponse>(request.downloadHandler.text);
                Debug.Log($"Obtenidas {response.preguntas.Count} preguntas");

                // Procesar preguntas
                foreach (var pregunta in response.preguntas)
                {
                    Debug.Log($"Pregunta: {pregunta.texto}");
                }
            }
            else
            {
                Debug.LogError("Error al obtener preguntas: " + request.error);
            }
        }
    }
}
```

### 4. Clase para Enviar Resultados

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class LMSResultadosManager : MonoBehaviour
{
    [System.Serializable]
    public class RespuestaDTO
    {
        public int preguntaId;
        public int opcionSeleccionadaId;
        public bool esCorrecta;
    }

    [System.Serializable]
    public class ResultadosRequest
    {
        public int sesionId;
        public string alumnoId;
        public int evaluacionId;
        public float notaFinal;
        public List<RespuestaDTO> respuestas;
    }

    [System.Serializable]
    public class ResultadosResponse
    {
        public bool exito;
        public string mensaje;
        public float notaRegistrada;
        public int intentoNumero;
    }

    public IEnumerator EnviarResultados(int sesionId, string alumnoId, int evaluacionId,
                                       float notaFinal, List<RespuestaDTO> respuestas)
    {
        string url = $"{LMSConfig.BASE_URL}/api/v2/3dlab/resultados";

        ResultadosRequest datos = new ResultadosRequest
        {
            sesionId = sesionId,
            alumnoId = alumnoId,
            evaluacionId = evaluacionId,
            notaFinal = notaFinal,
            respuestas = respuestas
        };

        string jsonData = JsonUtility.ToJson(datos);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-3DLAB-Key", LMSConfig.API_KEY);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ResultadosResponse response = JsonUtility.FromJson<ResultadosResponse>(request.downloadHandler.text);
                Debug.Log($"Resultados enviados exitosamente. Nota: {response.notaRegistrada}");
            }
            else
            {
                Debug.LogError("Error al enviar resultados: " + request.error);
            }
        }
    }
}
```

### 5. Flujo Completo en Unity

```csharp
using UnityEngine;
using System.Collections;

public class LaboratorioController : MonoBehaviour
{
    private LMSAuthManager authManager;
    private LMSLaboratorioManager laboratorioManager;
    private LMSResultadosManager resultadosManager;

    private int evaluacionId = 36; // ID del laboratorio de prueba
    private string alumnoId = "f1d2a26a-a208-4451-9ceb-dadf92def7e0"; // ID del estudiante

    void Start()
    {
        authManager = gameObject.AddComponent<LMSAuthManager>();
        laboratorioManager = gameObject.AddComponent<LMSLaboratorioManager>();
        resultadosManager = gameObject.AddComponent<LMSResultadosManager>();

        StartCoroutine(IniciarLaboratorio());
    }

    IEnumerator IniciarLaboratorio()
    {
        // Paso 1: Autenticar
        Debug.Log("1. Autenticando con el LMS...");
        yield return authManager.Authenticate();

        if (string.IsNullOrEmpty(authManager.GetToken()))
        {
            Debug.LogError("No se pudo autenticar. Abortando.");
            yield break;
        }

        // Paso 2: Obtener preguntas
        Debug.Log("2. Obteniendo preguntas del laboratorio...");
        yield return laboratorioManager.ObtenerPreguntas(evaluacionId);

        // Paso 3: Presentar laboratorio al estudiante
        Debug.Log("3. Presentando laboratorio en Unity...");
        // Aquí va tu lógica de Unity para mostrar las preguntas

        // Simular que el estudiante completó el laboratorio
        yield return new WaitForSeconds(2);

        // Paso 4: Enviar resultados
        Debug.Log("4. Enviando resultados al LMS...");

        var respuestas = new System.Collections.Generic.List<LMSResultadosManager.RespuestaDTO>
        {
            new LMSResultadosManager.RespuestaDTO
            {
                preguntaId = 1,
                opcionSeleccionadaId = 2,
                esCorrecta = true
            }
        };

        yield return resultadosManager.EnviarResultados(
            sesionId: 0, // 0 si no se creó sesión previamente
            alumnoId: alumnoId,
            evaluacionId: evaluacionId,
            notaFinal: 85.5f,
            respuestas: respuestas
        );

        Debug.Log("Laboratorio completado exitosamente!");
    }
}
```

---

## Referencia de Endpoints

### Resumen de Endpoints

| Método | Endpoint | Autenticación | Descripción |
|--------|----------|---------------|-------------|
| POST | `/api/Usuarios/login` | ❌ Ninguna | Obtener token JWT |
| GET | `/api/curso-usuarios/cursos` | 🔑 JWT Bearer | Listar cursos asignados |
| GET | `/api/Evaluaciones/curso/{id}` | 🔑 JWT Bearer | Listar evaluaciones de un curso |
| GET | `/api/v2/3dlab/preguntas` | 🔐 API Key | Obtener todas las preguntas |
| GET | `/api/v2/3dlab/evaluaciones/{id}/seleccion` | 🔐 API Key | Crear sesión y obtener selección |
| POST | `/api/v2/3dlab/resultados` | 🔐 API Key | Registrar resultados |

### Detalles Completos

#### 1. Login
```http
POST /api/Usuarios/login HTTP/1.1
Host: 135.148.148.88:5253
Content-Type: application/json

{
  "Correo": "3dlab@laboratorio.edu",
  "Contraseña": "LMS3dLab"
}
```

**Respuesta Exitosa (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### 2. Obtener Cursos
```http
GET /api/curso-usuarios/cursos HTTP/1.1
Host: 135.148.148.88:5253
Authorization: Bearer {token}
```

**Respuesta Exitosa (200):**
```json
{
  "operacionExitosa": true,
  "dato": [
    {
      "nrc": 1,
      "nombre": "3DLAB Laboratorio",
      "activo": true
    }
  ]
}
```

#### 3. Listar Evaluaciones
```http
GET /api/Evaluaciones/curso/1 HTTP/1.1
Host: 135.148.148.88:5253
Authorization: Bearer {token}
```

**Respuesta Exitosa (200):**
```json
{
  "operacionExitosa": true,
  "dato": [
    {
      "id": 36,
      "titulo": "Laboratorio 3DLAB - Prueba de Integración",
      "esLaboratorio3DLab": true
    }
  ]
}
```

#### 4. Obtener Preguntas
```http
GET /api/v2/3dlab/preguntas?evaluacionId=36 HTTP/1.1
Host: 135.148.148.88:5253
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Respuesta Exitosa (200):**
```json
{
  "evaluacionId": 36,
  "titulo": "Laboratorio 3DLAB - Prueba de Integración",
  "preguntas": [...]
}
```

#### 5. Crear Sesión
```http
GET /api/v2/3dlab/evaluaciones/36/seleccion?alumnoId=f1d2a26a-a208-4451-9ceb-dadf92def7e0 HTTP/1.1
Host: 135.148.148.88:5253
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION
```

**Respuesta Exitosa (200):**
```json
{
  "sesionId": 42,
  "evaluacionId": 36,
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "cantidadPreguntas": 5,
  "preguntas": [...]
}
```

#### 6. Enviar Resultados
```http
POST /api/v2/3dlab/resultados HTTP/1.1
Host: 135.148.148.88:5253
Content-Type: application/json
X-3DLAB-Key: 3DLAB-SECRET-KEY-CHANGE-IN-PRODUCTION

{
  "sesionId": 42,
  "alumnoId": "f1d2a26a-a208-4451-9ceb-dadf92def7e0",
  "evaluacionId": 36,
  "notaFinal": 85.5,
  "respuestas": [...]
}
```

**Respuesta Exitosa (200):**
```json
{
  "exito": true,
  "mensaje": "Resultados registrados correctamente",
  "notaRegistrada": 85.5
}
```

---

## Modelos de Datos

### Pregunta
```json
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
    }
  ]
}
```

### Respuesta
```json
{
  "preguntaId": 1,
  "opcionSeleccionadaId": 2,
  "esCorrecta": true
}
```

### Request de Resultados
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
    }
  ]
}
```

---

## Mejores Prácticas

### 1. Manejo de Errores

```csharp
if (request.result != UnityWebRequest.Result.Success)
{
    if (request.responseCode == 401)
    {
        Debug.LogError("Error de autenticación. Token inválido o expirado.");
        // Re-autenticar
    }
    else if (request.responseCode == 404)
    {
        Debug.LogError("Recurso no encontrado. Verificar IDs.");
    }
    else if (request.responseCode == 500)
    {
        Debug.LogError("Error del servidor. Reintentar más tarde.");
    }
    else
    {
        Debug.LogError($"Error: {request.error}");
    }
}
```

### 2. Caché de Token

```csharp
public class TokenCache
{
    private static string cachedToken;
    private static System.DateTime tokenExpiry;

    public static void SaveToken(string token)
    {
        cachedToken = token;
        tokenExpiry = System.DateTime.UtcNow.AddYears(10); // Token de 10 años
    }

    public static string GetToken()
    {
        if (System.DateTime.UtcNow < tokenExpiry)
        {
            return cachedToken;
        }
        return null; // Token expirado
    }

    public static bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(cachedToken) &&
               System.DateTime.UtcNow < tokenExpiry;
    }
}
```

### 3. Manejo de Respuestas Asíncronas

```csharp
public class AsyncHelper
{
    public delegate void OnSuccess<T>(T result);
    public delegate void OnError(string error);

    public static IEnumerator Request<T>(UnityWebRequest request,
                                        OnSuccess<T> onSuccess,
                                        OnError onError)
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
            onSuccess?.Invoke(result);
        }
        else
        {
            onError?.Invoke(request.error);
        }
    }
}
```

### 4. Validación de Datos

```csharp
public static bool ValidarPregunta(Pregunta pregunta)
{
    if (pregunta == null)
    {
        Debug.LogError("Pregunta es null");
        return false;
    }

    if (string.IsNullOrEmpty(pregunta.texto))
    {
        Debug.LogError("Pregunta sin texto");
        return false;
    }

    if (pregunta.opciones == null || pregunta.opciones.Count < 2)
    {
        Debug.LogError("Pregunta debe tener al menos 2 opciones");
        return false;
    }

    bool tieneRespuestaCorrecta = pregunta.opciones.Exists(o => o.esCorrecta);
    if (!tieneRespuestaCorrecta)
    {
        Debug.LogError("Pregunta debe tener al menos una respuesta correcta");
        return false;
    }

    return true;
}
```

---

## FAQ - Preguntas Frecuentes

### ¿Cuánto dura el token de autenticación?
El token del usuario 3DLAB tiene una duración de **10 años**. No es necesario renovarlo frecuentemente.

### ¿Puedo usar el mismo token para múltiples sesiones?
Sí, el token es reutilizable hasta su expiración. Se recomienda guardarlo en caché.

### ¿Qué sucede si envío resultados con un sesionId = 0?
El sistema creará una nueva sesión automáticamente y registrará los resultados.

### ¿Puedo obtener las respuestas correctas desde Unity?
Sí, el campo `esCorrecta` en cada opción indica si es la respuesta correcta.

### ¿Cómo sé el ID del estudiante?
Debes solicitarlo al estudiante al iniciar el laboratorio, o implementar un sistema de login en Unity.

### ¿Puedo crear mis propias preguntas desde Unity?
No, las preguntas deben ser creadas por el docente en el sistema LMS.

### ¿Qué pasa si pierdo la conexión a internet durante el laboratorio?
Se recomienda guardar las respuestas localmente en Unity y enviarlas cuando se recupere la conexión.

### ¿Puedo probar la integración sin afectar datos reales?
Sí, usa el curso "3DLAB Laboratorio" (NRC: 1) y el laboratorio de prueba (ID: 36).

---

## Contacto y Soporte

Para asistencia técnica o dudas sobre la integración, contactar al equipo de desarrollo del LMS Backend.

---

**Versión:** 1.0
**Última actualización:** 04 de Noviembre de 2025
**Autor:** Equipo de Desarrollo LMS Backend
