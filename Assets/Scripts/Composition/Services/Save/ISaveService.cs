namespace Composition.Interfaces
{
    public interface ISaveService
    {
        SaveConfigSO Config { get; }
        bool Save<T>(T data, string customFileName = null);
        T Load<T>(string customFileName = null);
        bool HasSave(string customFileName = null);
        bool DeleteSave(string customFileName = null);
        string GetSaveFilePath(string customFileName = null);
    }
}