using Microsoft.EntityFrameworkCore;
using ExcelUploader.Data;
using ExcelUploader.Models;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace ExcelUploader.Services
{
    public class DynamicTableService : IDynamicTableService
    {
        private readonly ILogger<DynamicTableService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public DynamicTableService(ILogger<DynamicTableService> logger, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<ServiceResult> CreateTableFromExcelAsync(string tableName, List<string> headers, List<List<object>> rows, List<ColumnDataTypeAnalysis> columnDataTypes)
        {
            try
            {
                _logger.LogInformation("Creating table {TableName} with {ColumnCount} columns and {RowCount} rows", 
                    tableName, headers.Count, rows.Count);

                // Create table SQL
                var createTableSql = GenerateCreateTableSql(tableName, headers, columnDataTypes);
                _logger.LogInformation("Create table SQL: {Sql}", createTableSql);

                // Execute create table
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Table {TableName} created successfully", tableName);

                // Insert data if rows exist
                if (rows.Any())
                {
                    var insertResult = await InsertDataAsync(connection, tableName, headers, rows);
                    if (!insertResult.IsSuccess)
                    {
                        return ServiceResult.Failure($"Veri eklenirken hata oluştu: {insertResult.ErrorMessage}");
                    }
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table {TableName}", tableName);
                return ServiceResult.Failure($"Tablo oluşturulurken hata oluştu: {ex.Message}");
            }
        }

        private string GenerateCreateTableSql(string tableName, List<string> headers, List<ColumnDataTypeAnalysis> columnDataTypes)
        {
            var columns = new List<string>();
            
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                var dataType = i < columnDataTypes.Count ? columnDataTypes[i].DetectedDataType : "nvarchar(255)";
                
                // Clean column name
                var cleanColumnName = CleanColumnName(header);
                
                columns.Add($"[{cleanColumnName}] {dataType}");
            }

            var sql = $@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{tableName}]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[{tableName}] (
                        [Id] int IDENTITY(1,1) PRIMARY KEY,
                        {string.Join(",\n                        ", columns)}
                    )
                END";

            return sql;
        }

        private string CleanColumnName(string columnName)
        {
            // Remove special characters and ensure valid SQL identifier
            var clean = System.Text.RegularExpressions.Regex.Replace(columnName, @"[^a-zA-Z0-9_]", "_");
            
            // Ensure it doesn't start with a number
            if (char.IsDigit(clean[0]))
            {
                clean = "Col_" + clean;
            }
            
            // Limit length
            if (clean.Length > 128)
            {
                clean = clean.Substring(0, 128);
            }
            
            return clean;
        }

        private async Task<ServiceResult> InsertDataAsync(SqlConnection connection, string tableName, List<string> headers, List<List<object>> rows)
        {
            try
            {
                if (!rows.Any()) return ServiceResult.Success();

                // Prepare column names
                var columnNames = headers.Select(h => $"[{CleanColumnName(h)}]").ToList();
                var columnsSql = string.Join(", ", columnNames);
                var parametersSql = string.Join(", ", columnNames.Select(c => "@" + c.Trim('[', ']')));

                var insertSql = $"INSERT INTO [{tableName}] ({columnsSql}) VALUES ({parametersSql})";
                
                _logger.LogInformation("Inserting {RowCount} rows into {TableName}", rows.Count, tableName);

                using var command = new SqlCommand(insertSql, connection);
                
                // Add parameters
                foreach (var header in headers)
                {
                    var cleanName = CleanColumnName(header);
                    command.Parameters.Add($"@{cleanName}", SqlDbType.NVarChar);
                }

                // Insert rows in batches
                const int batchSize = 1000;
                for (int i = 0; i < rows.Count; i += batchSize)
                {
                    var batch = rows.Skip(i).Take(batchSize);
                    
                    foreach (var row in batch)
                    {
                        // Set parameter values
                        for (int j = 0; j < headers.Count; j++)
                        {
                            var cleanName = CleanColumnName(headers[j]);
                            var value = j < row.Count ? row[j] : null;
                            command.Parameters[$"@{cleanName}"].Value = value ?? DBNull.Value;
                        }
                        
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    _logger.LogInformation("Inserted batch {BatchNumber} ({ProcessedRows} rows processed)", 
                        (i / batchSize) + 1, Math.Min(i + batchSize, rows.Count));
                }

                _logger.LogInformation("Successfully inserted {RowCount} rows into {TableName}", rows.Count, tableName);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into table {TableName}", tableName);
                return ServiceResult.Failure($"Veri eklenirken hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<Dictionary<string, object>>>> GetTableDataAsync(string tableName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = $"SELECT TOP 1000 * FROM [{tableName}]";
                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var data = new List<Dictionary<string, object>>();
                var columns = new List<string>();

                // Get column names
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }

                // Read data
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    data.Add(row);
                }

                return ServiceResult<List<Dictionary<string, object>>>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from table {TableName}", tableName);
                return ServiceResult<List<Dictionary<string, object>>>.Failure($"Tablo verisi alınırken hata oluştu: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteTableAsync(string tableName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = $"DROP TABLE IF EXISTS [{tableName}]";
                using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table {TableName}", tableName);
                return ServiceResult.Failure($"Tablo silinirken hata oluştu: {ex.Message}");
            }
        }
    }

    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

        public static ServiceResult Success()
        {
            return new ServiceResult { IsSuccess = true };
        }

        public static ServiceResult Failure(string errorMessage)
        {
            return new ServiceResult { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T Data { get; set; }

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T> { IsSuccess = true, Data = data };
        }

        public static new ServiceResult<T> Failure(string errorMessage)
        {
            return new ServiceResult<T> { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }
}
