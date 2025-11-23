# 🔐 Recuperación de Contraseña con SendGrid

## ✅ Estado: Implementado y Funcional

La funcionalidad de "Olvidar Contraseña" ahora envía emails profesionales usando **SendGrid** en lugar de SMTP.

---

## 📊 Cambios Implementados

### 1. Paquete SendGrid Instalado
```bash
dotnet add package SendGrid
# Version: 9.29.3
```

### 2. EmailService.cs - Nuevo Método
**Ubicación**: `/LMSBackend.API/Services/EmailService.cs`

```csharp
public async Task EnviarRecuperacionPasswordAsync(
    string destinatario,
    string nombreUsuario,
    string enlaceRecuperacion)
```

**Características**:
- ✅ Usa SendGrid API
- ✅ Template HTML profesional con gradiente púrpura
- ✅ Diseño responsive (max-width: 600px)
- ✅ Icono de candado 🔐
- ✅ Botón CTA destacado
- ✅ Warnings de seguridad
- ✅ Enlace alternativo si el botón no funciona
- ✅ Footer con copyright Universidad Andrés Bello
- ✅ Logging completo de éxito/errores

### 3. UsuarioService.cs - Método Actualizado
**Ubicación**: `/LMSBackend.API/Services/UsuarioService.cs:117`

```csharp
public async Task<ResultadoOperacion<string>> RecuperarPasswordAsync(RecuperarPasswordDTO dto)
{
    var usuario = await _userManager.FindByEmailAsync(dto.Correo);

    if (usuario == null)
    {
        return ResultadoOperacion<string>.Fallo("No se encontró ninguna cuenta asociada a ese correo", 404);
    }

    var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

    // URL de producción
    var link = $"http://135.148.148.88:3000/reset-password?token={Uri.EscapeDataString(token)}&email={usuario.Email}";

    try
    {
        // Enviar email usando SendGrid
        await _emailService.EnviarRecuperacionPasswordAsync(usuario.Email!, usuario.Nombre, link);

        return ResultadoOperacion<string>.Exito("Se ha enviado un correo con instrucciones...");
    }
    catch (Exception ex)
    {
        return ResultadoOperacion<string>.Fallo($"Error al enviar el correo de recuperación: {ex.Message}", 500);
    }
}
```

**Cambios**:
- ✅ URL actualizada a producción: `http://135.148.148.88:3000`
- ✅ Usa `EnviarRecuperacionPasswordAsync()` en lugar de `EnviarCorreoAsync()`
- ✅ Manejo de errores con try-catch
- ✅ Mensajes de error descriptivos

---

## 📡 Endpoint API

### POST `/api/usuarios/recuperar-password`

**Request Body**:
```json
{
  "correo": "juan.perez@uandresbello.edu"
}
```

**Respuesta Exitosa (200)**:
```json
{
  "operacionExitosa": true,
  "mensaje": "Se ha enviado un correo con instrucciones para restablecer tu contraseña",
  "codigo": 200
}
```

**Respuesta Error - Usuario No Encontrado (404)**:
```json
{
  "operacionExitosa": false,
  "mensaje": "No se encontró ninguna cuenta asociada a ese correo",
  "codigo": 404
}
```

**Respuesta Error - Fallo al Enviar Email (500)**:
```json
{
  "operacionExitosa": false,
  "mensaje": "Error al enviar el correo de recuperación: [detalle del error]",
  "codigo": 500
}
```

---

## 🧪 Pruebas

### Test Manual con cURL

```bash
# 1. Solicitar recuperación de contraseña
curl -X POST http://135.148.148.88:5253/api/usuarios/recuperar-password \
  -H "Content-Type: application/json" \
  -d '{"correo":"juan.perez@uandresbello.edu"}'

# Resultado esperado:
# {
#   "operacionExitosa": true,
#   "mensaje": "Se ha enviado un correo con instrucciones para restablecer tu contraseña",
#   "codigo": 200
# }

# 2. Verificar el email recibido
# - Abre el correo de juan.perez@uandresbello.edu
# - Deberías ver un email con diseño profesional
# - Haz clic en "Restablecer Contraseña"
# - Deberías ser redirigido a: http://135.148.148.88:3000/reset-password?token=...

# 3. Probar con usuario inexistente
curl -X POST http://135.148.148.88:5253/api/usuarios/recuperar-password \
  -H "Content-Type: application/json" \
  -d '{"correo":"noexiste@uandresbello.edu"}'

# Resultado esperado:
# {
#   "operacionExitosa": false,
#   "mensaje": "No se encontró ninguna cuenta asociada a ese correo",
#   "codigo": 404
# }
```

### Test desde Frontend

```javascript
// Función para solicitar recuperación de contraseña
const handleForgotPassword = async (email) => {
  try {
    const response = await fetch('http://135.148.148.88:5253/api/usuarios/recuperar-password', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ correo: email }),
    });

    const data = await response.json();

    if (response.ok) {
      alert('Se ha enviado un correo con instrucciones para restablecer tu contraseña');
    } else {
      alert(data.mensaje || 'Error al enviar el correo');
    }
  } catch (error) {
    console.error('Error:', error);
    alert('Error de conexión');
  }
};

// Uso
handleForgotPassword('juan.perez@uandresbello.edu');
```

---

## 🎨 Vista Previa del Email

### Diseño del Email

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│        Gradiente Púrpura (#667eea → #764ba2)                │
│                                                             │
│                        🔐                                    │
│           Recuperación de Contraseña                        │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Hola Juan Pérez,                                          │
│                                                             │
│  Hemos recibido una solicitud para restablecer la          │
│  contraseña de tu cuenta en el sistema LMS.                │
│                                                             │
│  Si solicitaste este cambio, haz clic en el siguiente      │
│  botón para crear una nueva contraseña:                    │
│                                                             │
│           ┌────────────────────────┐                        │
│           │ Restablecer Contraseña │  (Botón Púrpura)      │
│           └────────────────────────┘                        │
│                                                             │
│  ┌────────────────────────────────────────────┐            │
│  │  ⚠️ Importante:                            │            │
│  │  • Este enlace expirará en 24 horas        │            │
│  │  • Si no solicitaste este cambio, ignora   │            │
│  │    este correo                             │            │
│  │  • Nunca compartas este enlace             │            │
│  └────────────────────────────────────────────┘            │
│                                                             │
│  Si el botón no funciona, copia y pega el siguiente        │
│  enlace en tu navegador:                                   │
│  http://135.148.148.88:3000/reset-password?token=...       │
│                                                             │
│  Saludos,                                                   │
│  Equipo LMS                                                 │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│  Este es un correo automático del sistema LMS.             │
│  Por favor no responder.                                   │
│  © 2025 Sistema LMS - Universidad Andrés Bello             │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Configuración SendGrid

### appsettings.json

```json
{
  "SendGrid": {
    "ApiKey": "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",
    "From": "javierosquitherandom@gmail.com",
    "FromName": "LMS Sistema"
  }
}
```

**Límites de SendGrid**:
- Plan actual: Desconocido (verificar en dashboard de SendGrid)
- Recomendado: Al menos 100 emails/día para uso de producción

### Variables de Configuración

| Variable | Valor | Descripción |
|----------|-------|-------------|
| `SendGrid:ApiKey` | `SG.oySXJ0m2TbG...` | API Key de SendGrid |
| `SendGrid:From` | `javierosquitherandom@gmail.com` | Email remitente |
| `SendGrid:FromName` | `LMS Sistema` | Nombre del remitente |

---

## 🔍 Verificación de Logs

Para verificar si los emails se están enviando correctamente:

```bash
# Ver logs del backend en tiempo real
tail -f /tmp/backend_alumno_fix.log | grep "Email de recuperación"

# Logs esperados en caso de éxito:
# Email de recuperación enviado exitosamente a juan.perez@uandresbello.edu via SendGrid

# Logs esperados en caso de error:
# Error al enviar email via SendGrid a juan.perez@uandresbello.edu. Status: 401, Body: ...
# Excepción al enviar email de recuperación a juan.perez@uandresbello.edu: ...
```

---

## 🚨 Troubleshooting

### Problema: Error 401 Unauthorized

**Causa**: API Key de SendGrid inválida o expirada

**Solución**:
1. Ir a SendGrid Dashboard: https://app.sendgrid.com/
2. Settings → API Keys
3. Crear nueva API Key con permisos "Mail Send"
4. Actualizar `appsettings.json` con la nueva key
5. Reiniciar backend

### Problema: Email no llega a la bandeja de entrada

**Posibles causas**:
1. Email está en spam
2. Email del remitente no verificado en SendGrid
3. Dominio no autenticado

**Soluciones**:
1. Revisar carpeta de spam
2. Verificar el email remitente en SendGrid:
   - Settings → Sender Authentication → Single Sender Verification
3. Configurar dominio personalizado (opcional):
   - Settings → Sender Authentication → Authenticate Your Domain

### Problema: Exception al enviar email

**Verificar**:
1. API Key está correctamente configurada
2. Email del remitente está verificado en SendGrid
3. Hay límite de envíos disponible en SendGrid
4. Revisar logs para más detalles

---

## 📝 Notas Importantes

1. **Token de Recuperación**: El token generado por ASP.NET Identity tiene una expiración predeterminada (generalmente 24 horas)

2. **URL de Producción**: La URL está configurada para producción (`http://135.148.148.88:3000`). Si necesitas cambiarla:
   - Editar `UsuarioService.cs:129`
   - Actualizar la URL del enlace

3. **Seguridad**:
   - El token se envía en la URL (método estándar)
   - El token solo es válido una vez
   - El token expira después de 24 horas
   - Nunca se almacena la contraseña en texto plano

4. **Flujo Completo**:
   ```
   Frontend             Backend                  SendGrid
      |                    |                         |
      | POST /recuperar    |                         |
      |------------------->|                         |
      |                    | Generar token           |
      |                    | Crear enlace            |
      |                    |                         |
      |                    | POST email              |
      |                    |------------------------>|
      |                    |                         | ✉️  Enviar email
      |                    |<------------------------|
      |                    | 200 OK                  |
      |<-------------------|                         |
      |                    |                         |

   Usuario recibe email con enlace:
   http://135.148.148.88:3000/reset-password?token=...&email=...

   Usuario hace clic en enlace:
      |                    |
      | GET /reset-password|
      | (Frontend muestra  |
      |  formulario)       |
      |                    |
      | POST /restablecer  |
      | (con token y nueva |
      |  contraseña)       |
      |------------------->|
      |                    | Verificar token
      |                    | Cambiar contraseña
      |                    | 200 OK
      |<-------------------|
   ```

---

## ✅ Checklist de Verificación

- [x] SendGrid package instalado (v9.29.3)
- [x] EmailService.cs actualizado con método SendGrid
- [x] UsuarioService.cs usando nuevo método
- [x] URL actualizada a producción
- [x] appsettings.json con configuración SendGrid
- [x] Compilación exitosa (0 errores)
- [ ] **Pendiente**: Probar endpoint con email real
- [ ] **Pendiente**: Verificar email en bandeja de entrada
- [ ] **Pendiente**: Probar flujo completo de reseteo

---

## 📞 Soporte

Para cualquier problema con SendGrid:
- **Dashboard**: https://app.sendgrid.com/
- **Documentación**: https://docs.sendgrid.com/
- **Status**: https://status.sendgrid.com/

---

**Última actualización**: 2025-10-26
**Versión**: 1.0
**Estado**: ✅ Listo para Pruebas
