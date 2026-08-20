namespace Mentality.Infrastructure.AppLifecycle
{
    public interface IAppFlowService
    {
        void IniciarNuevaPartida(string sceneName = "SampleScene");
        void SalirJuego();
    }
}