using System.ComponentModel.DataAnnotations;

namespace LMSBackend.API.Validation
{
    /// <summary>
    /// Validación de rango que permite un valor especial (como 0) cuando se cumple una condición
    /// </summary>
    public class RangoCondicionalAttribute : ValidationAttribute
    {
        private readonly int _min;
        private readonly int _max;
        private readonly int _valorEspecialPermitido;
        private readonly string _propertyCondicion;

        public RangoCondicionalAttribute(int min, int max, int valorEspecialPermitido, string propertyCondicion)
        {
            _min = min;
            _max = max;
            _valorEspecialPermitido = valorEspecialPermitido;
            _propertyCondicion = propertyCondicion;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult("El valor es requerido");
            }

            var valorInt = (int)value;

            // Obtener el valor de la propiedad de condición
            var propertyInfo = validationContext.ObjectType.GetProperty(_propertyCondicion);
            if (propertyInfo == null)
            {
                return new ValidationResult($"Propiedad '{_propertyCondicion}' no encontrada");
            }

            var condicionValue = propertyInfo.GetValue(validationContext.ObjectInstance);
            if (condicionValue is bool condicionBool && condicionBool)
            {
                // Si la condición es true, permitir el valor especial
                if (valorInt == _valorEspecialPermitido)
                {
                    return ValidationResult.Success;
                }
            }

            // Validación normal de rango
            if (valorInt < _min || valorInt > _max)
            {
                return new ValidationResult(ErrorMessage ?? $"El valor debe estar entre {_min} y {_max}");
            }

            return ValidationResult.Success;
        }
    }
}
