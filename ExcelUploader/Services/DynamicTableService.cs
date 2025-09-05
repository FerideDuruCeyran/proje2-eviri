using Microsoft.EntityFrameworkCore;
using ExcelUploader.Data;
using ExcelUploader.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;

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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ServiceResult> CreateTableFromExcelAsync(string tableName, IFormFile file, string description)
        {
            try
            {
                // Read Excel file
                var (headers, rows) = await ReadExcelFileAsync(file);
                
                if (headers == null || !headers.Any())
                {
                    return ServiceResult.Failure("Excel dosyasından veri okunamadı. Dosya boş olabilir veya sütun başlıkları bulunamadı.");
                }

                if (rows == null || !rows.Any())
                {
                    return ServiceResult.Failure("Excel dosyasında veri satırı bulunamadı. Dosya boş olabilir.");
                }

                // Create table in database
                var createResult = await CreateTableAsync(tableName, headers);
                if (!createResult.IsSuccess)
                {
                    return ServiceResult.Failure(createResult.ErrorMessage);
                }

                // Insert data
                if (rows.Any())
                {
                    var insertResult = await InsertDataAsync(tableName, headers, rows);
                    if (!insertResult.IsSuccess)
                    {
                        return ServiceResult.Failure(insertResult.ErrorMessage);
                    }
                }

                // Save table metadata
                var dynamicTable = new DynamicTable
                {
                    TableName = tableName,
                    FileName = file.FileName,
                    Description = description,
                    UploadDate = DateTime.UtcNow,
                    RowCount = rows.Count,
                    ColumnCount = headers.Count,
                    IsProcessed = true
                };

                _context.DynamicTables.Add(dynamicTable);
                await _context.SaveChangesAsync();

                return TableCreationResult.Success(rows.Count, headers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table from Excel: {TableName}", tableName);
                return ServiceResult.Failure($"Tablo oluşturma hatası: {ex.Message}");
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
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    data.Add(row);
                }

                return ServiceResult<List<Dictionary<string, object>>>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data: {TableName}", tableName);
                return ServiceResult<List<Dictionary<string, object>>>.Failure($"Veri alma hatası: {ex.Message}");
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

                // Remove from metadata
                var table = await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == tableName);
                if (table != null)
                {
                    _context.DynamicTables.Remove(table);
                    await _context.SaveChangesAsync();
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {TableName}", tableName);
                return ServiceResult.Failure($"Tablo silme hatası: {ex.Message}");
            }
        }

        private Task<(List<string> headers, List<List<object>> rows)> ReadExcelFileAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            
            if (package.Workbook.Worksheets.Count == 0)
            {
                throw new ArgumentException("Excel dosyası boş veya geçersiz. Hiçbir sayfa bulunamadı.");
            }

            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
            {
                // Return empty data instead of throwing exception
                return Task.FromResult((new List<string>(), new List<List<object>>()));
            }

            var headers = new List<string>();
            var rows = new List<List<object>>();

            // Read headers (first row)
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                headers.Add(header);
            }

            // Read data rows
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var dataRow = new List<object>();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var value = worksheet.Cells[row, col].Value;
                    dataRow.Add(value ?? "");
                }
                rows.Add(dataRow);
            }

            return Task.FromResult((headers, rows));
        }

        private async Task<ServiceResult> CreateTableAsync(string tableName, List<string> headers)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var columns = string.Join(", ", headers.Select(h => $"[{h}] NVARCHAR(MAX)"));
                var sql = $"CREATE TABLE [{tableName}] ({columns})";

                using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Tablo oluşturma hatası: {ex.Message}");
            }
        }

        private async Task<ServiceResult> InsertDataAsync(string tableName, List<string> headers, List<List<object>> rows)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var columns = string.Join(", ", headers.Select(h => $"[{h}]"));
                var parameters = string.Join(", ", headers.Select(h => $"@{h}"));
                var sql = $"INSERT INTO [{tableName}] ({columns}) VALUES ({parameters})";

                using var command = new SqlCommand(sql, connection);
                
                foreach (var row in rows)
                {
                    command.Parameters.Clear();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@{headers[i]}", row[i] ?? DBNull.Value);
                    }
                    await command.ExecuteNonQueryAsync();
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Veri ekleme hatası: {ex.Message}");
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

    public class TableCreationResult : ServiceResult
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }

        public static TableCreationResult Success(int rowCount, int columnCount)
        {
            return new TableCreationResult 
            { 
                IsSuccess = true, 
                RowCount = rowCount, 
                ColumnCount = columnCount 
            };
        }

        public static new TableCreationResult Failure(string errorMessage)
        {
            return new TableCreationResult { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }
}
