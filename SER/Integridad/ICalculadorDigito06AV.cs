namespace SER.Integridad
{
    /// <summary>
    /// Estrategia intercambiable de cálculo de dígito. Implementaciones:
    /// CalculadorHexadecimal06AV (spec, por defecto), CalculadorModulo1106AV, CalculadorSha25606AV.
    /// </summary>
    public interface ICalculadorDigito06AV
    {
        string Calcular(string datos);
    }
}
