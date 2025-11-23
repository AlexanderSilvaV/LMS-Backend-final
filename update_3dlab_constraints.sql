-- Script para actualizar constraints de evaluaciones para soportar laboratorios 3DLAB

-- Eliminar constraints antiguos
ALTER TABLE "Evaluaciones" DROP CONSTRAINT IF EXISTS "CK_Evaluacion_IntentosMaximos";
ALTER TABLE "Evaluaciones" DROP CONSTRAINT IF EXISTS "CK_Evaluacion_TiempoLimite";

-- Crear nuevos constraints que permiten valores 0 para laboratorios 3DLAB
ALTER TABLE "Evaluaciones" ADD CONSTRAINT "CK_Evaluacion_IntentosMaximos"
    CHECK (("EsLaboratorio3DLab" = TRUE AND "IntentosMaximos" = 0) OR
           ("EsLaboratorio3DLab" = FALSE AND "IntentosMaximos" > 0 AND "IntentosMaximos" <= 10));

ALTER TABLE "Evaluaciones" ADD CONSTRAINT "CK_Evaluacion_TiempoLimite"
    CHECK (("EsLaboratorio3DLab" = TRUE AND "TiempoLimiteMins" = 0) OR
           ("EsLaboratorio3DLab" = FALSE AND "TiempoLimiteMins" > 0 AND "TiempoLimiteMins" <= 300));

-- Verificar constraints
SELECT constraint_name, check_clause
FROM information_schema.check_constraints
WHERE constraint_name IN ('CK_Evaluacion_IntentosMaximos', 'CK_Evaluacion_TiempoLimite');
