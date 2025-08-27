namespace ExcelUploader.Services
{
    public interface IPortService
    {
        int GetAvailablePort(int startPort = 5000);
        bool IsPortAvailable(int port);
        int GetCurrentPort();
        string GetBaseUrl();
        Task<bool> TestPortAsync(int port);
    }
}
