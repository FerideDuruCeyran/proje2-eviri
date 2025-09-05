using Microsoft.EntityFrameworkCore;
using ExcelUploader.Data;
using ExcelUploader.Models;
using System.Data;
using Microsoft.Data.SqlClient;
<<<<<<< HEAD
using OfficeOpenXml;
=======
using System.Data.SqlTypes;
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2

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

<<<<<<< HEAD
                if (rows == null || !rows.Any())
                {
                    return ServiceResult.Failure("Excel dosyasında veri satırı bulunamadı. Dosya boş olabilir.");
                }
=======
                // Generate cleaned column names and create table SQL
                var (createTableSql, cleanedColumnNames) = GenerateCreateTableSqlWithColumnNames(tableName, headers, columnDataTypes);
                _logger.LogInformation("Create table SQL: {Sql}", createTableSql);
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2

                // Create table in database
                var createResult = await CreateTableAsync(tableName, headers);
                if (!createResult.IsSuccess)
                {
                    return ServiceResult.Failure(createResult.ErrorMessage);
                }

                // Insert data
                if (rows.Any())
                {
<<<<<<< HEAD
                    var insertResult = await InsertDataAsync(tableName, headers, rows);
=======
                    var insertResult = await InsertDataAsync(connection, tableName, headers, cleanedColumnNames, rows);
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
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
<<<<<<< HEAD
                _logger.LogError(ex, "Error creating table from Excel: {TableName}", tableName);
                return ServiceResult.Failure($"Tablo oluşturma hatası: {ex.Message}");
=======
                _logger.LogError(ex, "Error creating table {TableName}", tableName);
                return ServiceResult.Failure($"Tablo oluşturulurken hata oluştu: {ex.Message}");
            }
        }

        private (string sql, List<string> columnNames) GenerateCreateTableSqlWithColumnNames(string tableName, List<string> headers, List<ColumnDataTypeAnalysis> columnDataTypes)
        {
            var columns = new List<string>();
            var usedColumnNames = new HashSet<string>();
            var cleanedColumnNames = new List<string>();
            
            _logger.LogInformation("Generating SQL for table {TableName} with {HeaderCount} headers", tableName, headers.Count);
            
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                var dataType = i < columnDataTypes.Count ? columnDataTypes[i].DetectedDataType : "nvarchar(255)";
                
                // Clean column name and ensure uniqueness
                var cleanColumnName = GetUniqueColumnName(header, usedColumnNames);
                usedColumnNames.Add(cleanColumnName);
                cleanedColumnNames.Add(cleanColumnName);
                
                _logger.LogInformation("Header {Index}: '{OriginalHeader}' -> '{CleanColumnName}'", i, header, cleanColumnName);
                
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

            _logger.LogInformation("Generated SQL: {Sql}", sql);
            return (sql, cleanedColumnNames);
        }

        private string GenerateCreateTableSql(string tableName, List<string> headers, List<ColumnDataTypeAnalysis> columnDataTypes)
        {
            var (sql, _) = GenerateCreateTableSqlWithColumnNames(tableName, headers, columnDataTypes);
            return sql;
        }

        private string CleanColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return "Column_1";

            var clean = columnName.Trim();
            
            // Convert Turkish characters to English equivalents
            clean = clean.Replace('ç', 'c').Replace('Ç', 'C');
            clean = clean.Replace('ğ', 'g').Replace('Ğ', 'G');
            clean = clean.Replace('ı', 'i').Replace('İ', 'I');
            clean = clean.Replace('ö', 'o').Replace('Ö', 'O');
            clean = clean.Replace('ş', 's').Replace('Ş', 'S');
            clean = clean.Replace('ü', 'u').Replace('Ü', 'U');

            // Replace whitespace with underscore
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", "_");
            
            // Remove or replace other special characters that are not valid in SQL identifiers
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"[^a-zA-Z0-9_]", "_");
            
            // Remove consecutive underscores
            clean = System.Text.RegularExpressions.Regex.Replace(clean, @"_+", "_");
            
            // Remove leading and trailing underscores
            clean = clean.Trim('_');
            
            // Ensure it doesn't start with a number
            if (clean.Length > 0 && char.IsDigit(clean[0]))
            {
                clean = "Col_" + clean;
            }
            
            // If empty after cleaning, provide a default name
            if (string.IsNullOrEmpty(clean))
            {
                clean = "Column_1";
            }
            
            // Ensure it starts with a letter or underscore
            if (clean.Length > 0 && !char.IsLetter(clean[0]) && clean[0] != '_')
            {
                clean = "Col_" + clean;
            }
            
            // Limit length
            if (clean.Length > 100)
            {
                clean = clean.Substring(0, 100);
            }
            
            return clean;
        }

        private string GetUniqueColumnName(string originalName, HashSet<string> usedNames)
        {
            var baseName = CleanColumnName(originalName);
            
            // If baseName is empty or null, create a default column name
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "Column";
            }
            
            var uniqueName = baseName;
            var counter = 1;
            
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }
            
            return uniqueName;
        }

        private async Task<ServiceResult> InsertDataAsync(SqlConnection connection, string tableName, List<string> headers, List<string> cleanedColumnNames, List<List<object>> rows)
        {
            try
            {
                if (!rows.Any()) return ServiceResult.Success();

                // Prepare column names using the cleaned names from table creation
                var columnNames = cleanedColumnNames.Select(c => $"[{c}]").ToList();
                var columnsSql = string.Join(", ", columnNames);
                var parametersSql = string.Join(", ", cleanedColumnNames.Select(c => "@" + c));

                var insertSql = $"INSERT INTO [{tableName}] ({columnsSql}) VALUES ({parametersSql})";
                
                _logger.LogInformation("Inserting {RowCount} rows into {TableName}", rows.Count, tableName);

                using var command = new SqlCommand(insertSql, connection);
                
                // Add parameters with proper SQL types based on column data types
                for (int i = 0; i < headers.Count; i++)
                {
                    var cleanName = cleanedColumnNames[i];
                    var sqlType = GetSqlParameterType(headers[i], rows);
                    command.Parameters.Add($"@{cleanName}", sqlType);
                }

                // Insert rows in batches
                const int batchSize = 1000;
                for (int i = 0; i < rows.Count; i += batchSize)
                {
                    var batch = rows.Skip(i).Take(batchSize);
                    
                    foreach (var row in batch)
                    {
                        // Set parameter values with proper type conversion
                        for (int j = 0; j < headers.Count; j++)
                        {
                            var cleanName = cleanedColumnNames[j];
                            var value = j < row.Count ? row[j] : null;
                            var convertedValue = ConvertValueForSql(value, command.Parameters[$"@{cleanName}"].SqlDbType);
                            command.Parameters[$"@{cleanName}"].Value = convertedValue ?? DBNull.Value;
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
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

        private SqlDbType GetSqlParameterType(string originalColumnName, List<List<object>> rows)
        {
            // We need to find the column index by matching the original header name
            // Since we don't have access to headers here, we'll use a more conservative approach
            // and analyze all data to determine the most appropriate type
            
            // For now, let's be more conservative and default to string for most cases
            // unless we're very confident about the data type
            
            // Check if it's a date column by name (handle both Turkish and English)
            var lowerColumnName = originalColumnName.ToLowerInvariant();
            if (lowerColumnName.Contains("tarih") || lowerColumnName.Contains("date") || 
                lowerColumnName.Contains("zaman") || lowerColumnName.Contains("time") ||
                lowerColumnName.Contains("baslangic") || lowerColumnName.Contains("bitis") ||
                lowerColumnName.Contains("start") || lowerColumnName.Contains("end") ||
                lowerColumnName.Contains("dogum") || lowerColumnName.Contains("birth"))
            {
                // Only use DateTime2 if the column name strongly suggests it's a date
                return SqlDbType.DateTime2;
            }
            
            // For all other cases, use NVarChar to avoid conversion issues
            return SqlDbType.NVarChar;
        }

        private object ConvertValueForSql(object value, SqlDbType sqlType)
        {
            if (value == null) return null;

            try
            {
                switch (sqlType)
                {
                    case SqlDbType.DateTime2:
                        if (value is DateTime dt) return dt;
                        if (value is string strVal)
                        {
                            // Handle empty or whitespace strings
                            if (string.IsNullOrWhiteSpace(strVal)) return null;
                            
                            // Handle Excel date serial numbers
                            if (double.TryParse(strVal, out var serialNumber) && serialNumber > 0)
                            {
                                try
                                {
                                    // Excel dates start from January 1, 1900
                                    var excelDate = DateTime.FromOADate(serialNumber);
                                    return excelDate;
                                }
                                catch
                                {
                                    // If conversion fails, return null instead of original value
                                    return null;
                                }
                            }
                            
                            if (DateTime.TryParse(strVal, out var parsedDate)) return parsedDate;
                            
                            // If all conversions fail, return null instead of original value
                            return null;
                        }
                        // For other types that can't be converted to DateTime, return null
                        return null;

                    case SqlDbType.Int:
                        if (value is int i) return i;
                        if (value is long l && l >= int.MinValue && l <= int.MaxValue) return (int)l;
                        if (value is double d && d >= int.MinValue && d <= int.MaxValue) return (int)d;
                        if (value is string strVal4)
                        {
                            if (string.IsNullOrWhiteSpace(strVal4)) return null;
                            if (int.TryParse(strVal4, out var intResult)) return intResult;
                        }
                        return null;

                    case SqlDbType.Decimal:
                        if (value is decimal dec) return dec;
                        if (value is double d2) return (decimal)d2;
                        if (value is float f) return (decimal)f;
                        if (value is int i2) return (decimal)i2;
                        if (value is long l2) return (decimal)l2;
                        if (value is string strVal5)
                        {
                            if (string.IsNullOrWhiteSpace(strVal5)) return null;
                            if (decimal.TryParse(strVal5, out var decResult)) return decResult;
                        }
                        return null;

                    case SqlDbType.Bit:
                        if (value is bool b) return b;
                        if (value is string strVal6)
                        {
                            if (string.IsNullOrWhiteSpace(strVal6)) return null;
                            if (bool.TryParse(strVal6, out var boolResult)) return boolResult;
                            // Handle common boolean representations
                            var lower = strVal6.ToLower().Trim();
                            if (lower == "1" || lower == "yes" || lower == "y" || lower == "true" || lower == "evet") return true;
                            if (lower == "0" || lower == "no" || lower == "n" || lower == "false" || lower == "hayır") return false;
                        }
                        return null;

                    case SqlDbType.NVarChar:
                    default:
                        return value?.ToString();
                }
            }
            catch
            {
                // If any conversion fails, return null instead of throwing
                return null;
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
