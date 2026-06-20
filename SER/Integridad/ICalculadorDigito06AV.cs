namespace SER.Integridad
{
    /// <summary>
    /// Estrategia de cálculo de dígito/hash intercambiable.
    /// Implementaciones disponibles: CalculadorModulo1106AV (académico) y CalculadorSha25606AV (robusto).
    /// </summary>
    public interface ICalculadorDigito06AV
    {
        /// <summary>
        /// Recibe un string (contenido de una fila o concatenación de dígitos)
        /// y devuelve su representación calculada.
        /// </summary>
        string Calcular(string datos);
    }
}
