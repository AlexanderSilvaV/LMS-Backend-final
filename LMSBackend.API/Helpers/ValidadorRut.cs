using System;
using System.Linq;
using System.Text.RegularExpressions; // Para validar el formato del RUT con expresiones regulares

namespace LMSBackend.API.Helpers
{
    // Enumeración que indica el resultado de la validación del RUT
    public enum ResultadoRut
    {
        Valido,                          // RUT válido
        FormatoInvalido,                 // RUT con formato incorrecto
        DigitoVerificadorIncorrecto      // RUT con dígito verificador incorrecto
    }

    public static class ValidadorRut
    {
        /// Valida el formato y dígito verificador del RUT chileno
        /// <param name="rut">RUT con o sin puntos y guión</param>
        /// <returns>ResultadoRut indicando validez o tipo de error</returns>
        public static ResultadoRut ValidarRut(string rut)
        {
            // Verifica si la entrada está vacía o es null
            if (string.IsNullOrWhiteSpace(rut))
                return ResultadoRut.FormatoInvalido;

            // Validar formato EXACTO con puntos y guión
            // Ejemplo válido: "12.345.678-5"
            var formato = new Regex(@"^[1-9]\d?\.\d{3}\.\d{3}-[\dkK]$");
            if (!formato.IsMatch(rut))
                return ResultadoRut.FormatoInvalido;

            // Elimina puntos y guión para calcular dígito verificador
            var limpio = rut.Replace(".", string.Empty).Replace("-", string.Empty).ToUpper();

            // Separa cuerpo y dígito verificador ingresado
            var cuerpo = limpio.Substring(0, limpio.Length - 1);
            var dvIngresado = limpio.Last();

            // Calcular dígito verificador según algoritmo oficial chileno
            int suma = 0;
            int multiplo = 2;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                suma += (cuerpo[i] - '0') * multiplo; // Multiplica cada dígito por su factor
                multiplo = (multiplo == 7) ? 2 : multiplo + 1; // Ciclo de multiplicadores 2-7
            }

            int resto = suma % 11;
            char dvEsperado = resto switch
            {
                1 => 'K', // Caso especial dígito K
                0 => '0', // Caso resto 0
                _ => (char)('0' + (11 - resto)) // Resto normal
            };

            // Compara dígito calculado con dígito ingresado
            if (dvEsperado != dvIngresado)
                return ResultadoRut.DigitoVerificadorIncorrecto; // Retorna error específico

            // RUT válido en formato y dígito verificador
            return ResultadoRut.Valido;
        }
    }
}
