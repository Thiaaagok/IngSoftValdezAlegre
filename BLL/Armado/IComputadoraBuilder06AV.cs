using BE;

namespace BLL.Armado
{
    /// <summary>
    /// PATRÓN BUILDER — rol BUILDER (interfaz abstracta).
    ///
    /// Declara los pasos para construir una <see cref="Computadora06AV"/>, una
    /// operación por cada bahía del equipo. No dice en qué ORDEN se ejecutan (eso es
    /// responsabilidad del Director, <see cref="ArmadorComputadora06AV"/>) ni cómo
    /// queda representado el producto final (eso lo decide cada ConcreteBuilder).
    ///
    /// Participantes del patrón en PCFORGE:
    ///   Builder ............ IComputadoraBuilder06AV
    ///   ConcreteBuilder .... ComputadoraEstandarBuilder06AV, ComputadoraConfigurableBuilder06AV
    ///   Director ........... ArmadorComputadora06AV
    ///   Product ............ BE.Computadora06AV
    ///   Client ............. VentasBLL06AV, ModelosEstandarBLL06AV
    /// </summary>
    public interface IComputadoraBuilder06AV
    {
        /// <summary>Descarta cualquier armado a medio hacer y empieza un equipo vacío.</summary>
        void Reiniciar();

        void ColocarProcesador(Componente06AV procesador);
        void ColocarPlacaMadre(Componente06AV placaMadre);
        void ColocarMemoriaRam(Componente06AV memoria);
        void ColocarAlmacenamiento(Componente06AV disco);
        void ColocarPlacaDeVideo(Componente06AV placaDeVideo);
        void ColocarFuente(Componente06AV fuente);
        void ColocarGabinete(Componente06AV gabinete);
        void ColocarRefrigeracion(Componente06AV refrigeracion);
        void ColocarAdicional(Componente06AV adicional);

        /// <summary>
        /// Entrega el producto terminado (GetResult en la nomenclatura de GoF).
        /// Falla si el equipo no está completo. Después de entregarlo, el builder
        /// queda vacío y listo para armar otro.
        /// </summary>
        Computadora06AV ObtenerComputadora();
    }
}
