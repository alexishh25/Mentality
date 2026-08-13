namespace Mentality.Application.AppLifecycle
{
    public interface IAppFlowService
    {
        void IniciarNuevaPartida(string sceneName = "SampleScene");
        void SalirJuego();
    }
}