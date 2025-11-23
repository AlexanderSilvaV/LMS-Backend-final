// Client-side helper to ensure there's a session/token for an evaluation.
// It will reuse a session stored in sessionStorage when possible, validate it
// against the backend, and fall back to starting a new session.
import { evaluacionService } from './evaluacion-service';

export interface EvaluacionSessionInfo {
  token: string;
  numeroIntento: number;
  fechaInicio?: string;
  tiempoLimiteMins?: number;
}

const storageKey = (evaluacionId: number) => `evaluacion_session_${evaluacionId}`;

export async function ensureEvaluacionSession(evaluacionId: number): Promise<EvaluacionSessionInfo> {
  // Run only in browser
  if (typeof window === 'undefined' || !window.sessionStorage) {
    // Fallback: just call iniciarEvaluacion
    const dto: any = await evaluacionService.iniciarEvaluacion(evaluacionId);
    return {
      token: dto.token,
      numeroIntento: dto.numeroIntento ?? dto.NumeroIntento,
      fechaInicio: dto.fechaInicio ?? dto.FechaInicio,
      tiempoLimiteMins: dto.tiempoLimiteMins ?? dto.TiempoLimiteMins
    };
  }

  const key = storageKey(evaluacionId);
  const raw = sessionStorage.getItem(key);
  if (raw) {
    try {
      const parsed: EvaluacionSessionInfo = JSON.parse(raw);
      if (parsed?.token) {
        // Validate token by requesting the evaluation payload for realizar
        try {
          await evaluacionService.obtenerEvaluacionParaRealizar(evaluacionId, parsed.token);
          sessionStorage.setItem('evaluacion_token', parsed.token);
          sessionStorage.setItem(key, JSON.stringify(parsed));
          return parsed;
        } catch (err) {
          // token invalid/expired - continue to create new
          console.warn('Evaluacion session token invalid, creating new:', err);
        }
      }
    } catch (e) {
      console.warn('Failed to parse stored evaluacion session:', e);
    }
  }

  // Create new session
  const dto: any = await evaluacionService.iniciarEvaluacion(evaluacionId);
  const info: EvaluacionSessionInfo = {
    token: dto.token,
    numeroIntento: dto.numeroIntento ?? dto.NumeroIntento,
    fechaInicio: dto.fechaInicio ?? dto.FechaInicio,
    tiempoLimiteMins: dto.tiempoLimiteMins ?? dto.TiempoLimiteMins
  };

  try {
    sessionStorage.setItem(key, JSON.stringify(info));
    // Also keep backwards-compatible key used elsewhere
    sessionStorage.setItem('evaluacion_token', info.token);
  } catch (e) {
    console.warn('Could not persist evaluacion session in sessionStorage:', e);
  }

  return info;
}
