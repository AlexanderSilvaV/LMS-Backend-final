
namespace LMSBackend.API.Helpers // Declarando espacio logico de Helpers
{
    public class ResultadoOperacion<T> // Declarando ResultadoOperacion, para poder enviar mensajes sobre operaciones que se hagan.
    {
        public bool OperacionExitosa { get; } // Declarando campo Exito y su tipo bool
        public string Mensaje { get; } // Declarando campo mensaje, para que nos ayude a mostrar mensajes de error o exito.
        public int Codigo { get; } // Declarando el codigo que retornara la operación, utilizando HTTP Code
        public T Dato { get; } // Declarando el dato que necesitamos .

        private ResultadoOperacion(bool exito, string mensaje, int codigo, T dato)
        {
            OperacionExitosa = exito;
            Mensaje = mensaje;
            Codigo = codigo;
            Dato = dato;

            if (exito && dato == null) // Validación de dato no puede ser nullo si la instancia fue un exito
            {
                throw new ArgumentNullException(nameof(dato), "Dato no puede ser null si la operación fue exitosa");
            }

            if (!exito && string.IsNullOrWhiteSpace(mensaje)) // Validación de mensaje vacio si no hay exito en la instancia
            {
                throw new ArgumentException("El mensaje de error no puede estar vacío cuando la operación falla.", nameof(mensaje));
            }

        }
        public static ResultadoOperacion<T> Exito(T dato, string mensaje = "Operación Exitosa", int codigo = 200) // Exito en la instancia
        {
            return new ResultadoOperacion<T>(true, mensaje, codigo, dato);
        }

        public static ResultadoOperacion<T> Fallo(string mensaje, int codigo) // Fallo en la instancia
        {
            return new ResultadoOperacion<T>(false, mensaje, codigo, default!);
        }
    }
}
