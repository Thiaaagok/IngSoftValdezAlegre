using BE;

namespace BLL.Armado
{
    public interface IComputadoraBuilder06AV
    {
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

        Computadora06AV ObtenerComputadora();
    }
}
