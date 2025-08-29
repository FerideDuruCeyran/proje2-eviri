using ExcelUploader.Models;
using ExcelUploader.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExcelUploader.Services
{
    public class PortService : IPortService
    {
        private readonly ILogger<PortService> _logger;
        private readonly ApplicationDbContext _context;

        public PortService(ILogger<PortService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<List<DatabaseConnection>> GetAllConnectionsAsync()
        {
            try
            {
                return await Task.FromResult(_context.DatabaseConnections.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantıları alınırken hata oluştu");
                throw;
            }
        }

        public async Task<DatabaseConnection> GetConnectionByIdAsync(int id)
        {
            try
            {
                var connection = await Task.FromResult(_context.DatabaseConnections.FirstOrDefault(c => c.Id == id));
                if (connection == null)
                    throw new ArgumentException($"ID {id} ile bağlantı bulunamadı");
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı alınırken hata oluştu: {Id}", id);
                throw;
            }
        }

        public async Task<DatabaseConnection> CreateConnectionAsync(DatabaseConnection connection)
        {
            try
            {
                connection.CreatedDate = DateTime.UtcNow;
                connection.IsActive = true;
                
                _context.DatabaseConnections.Add(connection);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Yeni veritabanı bağlantısı oluşturuldu: {Name}", connection.Name);
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı oluşturulurken hata oluştu");
                throw;
            }
        }

        public async Task<bool> UpdateConnectionAsync(DatabaseConnection connection)
        {
            try
            {
                var existingConnection = await GetConnectionByIdAsync(connection.Id);
                existingConnection.Name = connection.Name;
                existingConnection.ServerName = connection.ServerName;
                existingConnection.Port = connection.Port;
                existingConnection.DatabaseName = connection.DatabaseName;
                existingConnection.Username = connection.Username;
                existingConnection.Password = connection.Password;
                existingConnection.IsActive = connection.IsActive;
                existingConnection.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Veritabanı bağlantısı güncellendi: {Id}", connection.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı güncellenirken hata oluştu: {Id}", connection.Id);
                throw;
            }
        }

        public async Task<bool> DeleteConnectionAsync(int id)
        {
            try
            {
                var connection = await GetConnectionByIdAsync(id);
                _context.DatabaseConnections.Remove(connection);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Veritabanı bağlantısı silindi: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı silinirken hata oluştu: {Id}", id);
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(DatabaseConnection connection)
        {
            try
            {
                var connectionString = BuildConnectionString(connection);
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();
                
                _logger.LogInformation("Veritabanı bağlantısı test edildi: {Name}", connection.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı test edilirken hata oluştu: {Name}", connection.Name);
                return false;
            }
        }

        public async Task<bool> TestConnectionByIdAsync(int id)
        {
            try
            {
                var connection = await GetConnectionByIdAsync(id);
                return await TestConnectionAsync(connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı test edilirken hata oluştu: {Id}", id);
                return false;
            }
        }

        public async Task<List<string>> GetDatabasesAsync(DatabaseConnection connection)
        {
            try
            {
                var connectionString = BuildConnectionString(connection, "master");
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();

                var databases = new List<string>();
                using var command = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4", sqlConnection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    databases.Add(reader.GetString(0));
                }

                return databases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanları alınırken hata oluştu: {Name}", connection.Name);
                throw;
            }
        }

        public async Task<List<string>> GetTablesAsync(DatabaseConnection connection, string databaseName)
        {
            try
            {
                var connectionString = BuildConnectionString(connection, databaseName);
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();

                var tables = new List<string>();
                using var command = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", sqlConnection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }

                return tables;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablolar alınırken hata oluştu: {Database}", databaseName);
                throw;
            }
        }

        public async Task<bool> ExecuteQueryAsync(DatabaseConnection connection, string databaseName, string query)
        {
            try
            {
                var connectionString = BuildConnectionString(connection, databaseName);
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();

                using var command = new SqlCommand(query, sqlConnection);
                await command.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Sorgu başarıyla çalıştırıldı: {Database}", databaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sorgu çalıştırılırken hata oluştu: {Database}", databaseName);
                throw;
            }
        }

        public async Task<object> ExecuteQueryWithResultAsync(DatabaseConnection connection, string databaseName, string query)
        {
            try
            {
                var connectionString = BuildConnectionString(connection, databaseName);
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();

                using var command = new SqlCommand(query, sqlConnection);
                using var reader = await command.ExecuteReaderAsync();
                
                var dataTable = new DataTable();
                dataTable.Load(reader);
                
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sorgu sonuçları alınırken hata oluştu: {Database}", databaseName);
                throw;
            }
        }

        private string BuildConnectionString(DatabaseConnection connection, string? databaseName = null)
        {
            var dbName = databaseName ?? connection.DatabaseName;
            return $"Server={connection.ServerName},{connection.Port};Database={dbName};User Id={connection.Username};Password={connection.Password};TrustServerCertificate=true;";
        }
    }
}
