using Microsoft.EntityFrameworkCore;
using ExcelUploader.Data;
using ExcelUploader.Models;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;

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

                // Check if table already exists
                var tableExists = await CheckIfTableExistsAsync(tableName);
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                List<string> cleanedColumnNames;

                if (tableExists)
                {
                    _logger.LogInformation("Table {TableName} already exists, will insert data into existing table", tableName);
                    
                    // Get existing table column names
                    cleanedColumnNames = await GetExistingTableColumnsAsync(connection, tableName);
                    
                    if (cleanedColumnNames == null || !cleanedColumnNames.Any())
                    {
                        return ServiceResult.Failure($"Mevcut tablo {tableName} sütun bilgileri alınamadı");
                    }
                }
                else
                {
                    _logger.LogInformation("Table {TableName} does not exist, creating new table", tableName);
                    
                    // Generate cleaned column names and create table SQL
                    var (createTableSql, columnNames) = GenerateCreateTableSqlWithColumnNames(tableName, headers, columnDataTypes);
                    cleanedColumnNames = columnNames;
                    
                    _logger.LogInformation("Create table SQL: {Sql}", createTableSql);

                    using var command = new SqlCommand(createTableSql, connection);
                    await command.ExecuteNonQueryAsync();

                    _logger.LogInformation("Table {TableName} created successfully", tableName);
                }

                // Insert data if rows exist
                if (rows.Any())
                {
                    var insertResult = await InsertDataAsync(connection, tableName, headers, cleanedColumnNames, rows);
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

        private async Task<bool> CheckIfTableExistsAsync(string tableName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = 'dbo'";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);

                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if table {TableName} exists", tableName);
                return false;
            }
        }

        private async Task<List<string>> GetExistingTableColumnsAsync(SqlConnection connection, string tableName)
        {
            try
            {
                var sql = @"
                    SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = 'dbo' 
                    AND COLUMN_NAME != 'Id'
                    ORDER BY ORDINAL_POSITION";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);

                var columns = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString("COLUMN_NAME"));
                }

                return columns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting columns for table {TableName}", tableName);
                return null;
            }
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
}
