namespace Mentality.Application.Interfaces
{
    /// <summary>
    /// Dependency Inversion Principle (DIP).
    /// Abstracción para el guardado y carga de datos. La lógica de alto nivel
    /// dependerá de esta interfaz, no de la implementación específica (JSON, base de datos, PlayerPrefs).
    /// </summary>
    public interface ISaveService
    {
        void Save<T>(string key, T data);
        T Load<T>(string key, T defaultValue = default);
    }
}
