using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface IPortService
    {
        Task<List<DatabaseConnection>> GetAllConnectionsAsync();
        Task<DatabaseConnection> GetConnectionByIdAsync(int id);
        Task<DatabaseConnection> CreateConnectionAsync(DatabaseConnection connection);
        Task<bool> UpdateConnectionAsync(DatabaseConnection connection);
        Task<bool> DeleteConnectionAsync(int id);
        Task<bool> TestConnectionAsync(DatabaseConnection connection);
        Task<bool> TestConnectionByIdAsync(int id);
        Task<List<string>> GetDatabasesAsync(DatabaseConnection connection);
        Task<List<string>> GetTablesAsync(DatabaseConnection connection, string databaseName);
        Task<bool> ExecuteQueryAsync(DatabaseConnection connection, string databaseName, string query);
        Task<object> ExecuteQueryWithResultAsync(DatabaseConnection connection, string databaseName, string query);
    }
}
