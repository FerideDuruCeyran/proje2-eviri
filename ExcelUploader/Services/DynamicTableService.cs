using ExcelUploader.Models;
using ExcelUploader.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ExcelUploader.Services
{
    public class DynamicTableService : IDynamicTableService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DynamicTableService> _logger;

        public DynamicTableService(ApplicationDbContext context, IConfiguration configuration, ILogger<DynamicTableService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DynamicTable> CreateTableFromExcelAsync(IFormFile file, string uploadedBy, string? description = null)
        {
            try
            {
                // Generate unique table name
                var tableName = GenerateTableName(file.FileName);
                
                // Read Excel headers and determine data types
                var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                
                // Create DynamicTable entity
                var dynamicTable = new DynamicTable
                {
                    TableName = tableName,
                    FileName = file.FileName,
                    UploadedBy = uploadedBy,
                    Description = description,
                    RowCount = sampleData.Count,
                    ColumnCount = headers.Count,
                    UploadDate = DateTime.UtcNow
                };

                // Create table columns
                for (int i = 0; i < headers.Count; i++)
                {
                    var column = new TableColumn
                    {
                        ColumnName = SanitizeColumnName(headers[i]),
                        DisplayName = headers[i],
                        DataType = dataTypes[i],
                        ColumnOrder = i + 1,
                        MaxLength = dataTypes[i] == "nvarchar" ? 1000 : null,
                        IsRequired = false,
                        IsUnique = false
                    };
                    
                    dynamicTable.Columns.Add(column);
                }

                // Save to database
                _context.DynamicTables.Add(dynamicTable);
                await _context.SaveChangesAsync();

                // Create SQL table
                var sqlTableCreated = await CreateSqlTableAsync(dynamicTable);
                if (!sqlTableCreated)
                {
                    throw new InvalidOperationException("SQL table creation failed");
                }

                // Insert data
                var dataInserted = await InsertDataAsync(dynamicTable, sampleData);
                if (!dataInserted)
                {
                    throw new InvalidOperationException("Data insertion failed");
                }

                // Mark as processed
                dynamicTable.IsProcessed = true;
                dynamicTable.ProcessedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return dynamicTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dynamic table from Excel file: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<bool> CreateSqlTableAsync(DynamicTable dynamicTable)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Build CREATE TABLE SQL
                var createTableSql = BuildCreateTableSql(dynamicTable);
                
                using var command = new SqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("SQL table created successfully: {TableName}", dynamicTable.TableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SQL table: {TableName}", dynamicTable.TableName);
                return false;
            }
        }

        private string BuildCreateTableSql(DynamicTable dynamicTable)
        {
            var columns = dynamicTable.Columns.OrderBy(c => c.ColumnOrder);
            var columnDefinitions = columns.Select(c => 
                $"[{c.ColumnName}] {GetSqlDataType(c.DataType, c.MaxLength)} {(c.IsRequired ? "NOT NULL" : "NULL")}"
            );

            return $@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{dynamicTable.TableName}')
                BEGIN
                    CREATE TABLE [{dynamicTable.TableName}] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        {string.Join(",\n                        ", columnDefinitions)},
                        [CreatedAt] DATETIME2 DEFAULT GETDATE(),
                        [UpdatedAt] DATETIME2 DEFAULT GETDATE()
                    )
                END";
        }

        private string GetSqlDataType(string dataType, int? maxLength)
        {
            return dataType.ToLower() switch
            {
                "string" => maxLength.HasValue ? $"NVARCHAR({maxLength.Value})" : "NVARCHAR(MAX)",
                "int" => "INT",
                "decimal" => "DECIMAL(18,2)",
                "datetime" => "DATETIME2",
                "boolean" => "BIT",
                "guid" => "UNIQUEIDENTIFIER",
                _ => "NVARCHAR(MAX)"
            };
        }

        public async Task<bool> InsertDataAsync(DynamicTable dynamicTable, List<Dictionary<string, object>> data)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Build INSERT statement
                var columns = dynamicTable.Columns.OrderBy(c => c.ColumnOrder).ToList();
                var columnNames = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));
                var parameterNames = string.Join(", ", columns.Select(c => $"@{c.ColumnName}"));

                var insertSql = $"INSERT INTO [{dynamicTable.TableName}] ({columnNames}) VALUES ({parameterNames})";

                using var command = new SqlCommand(insertSql, connection);
                
                // Add parameters
                foreach (var column in columns)
                {
                    command.Parameters.AddWithValue($"@{column.ColumnName}", DBNull.Value);
                }

                // Insert data rows
                foreach (var row in data)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        var column = columns[i];
                        var columnName = column.ColumnName;
                        
                        if (row.ContainsKey(columnName))
                        {
                            var value = row[columnName];
                            if (value == null || string.IsNullOrEmpty(value.ToString()))
                            {
                                command.Parameters[$"@{columnName}"].Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters[$"@{columnName}"].Value = ConvertValue(value.ToString() ?? "", column.DataType);
                            }
                        }
                        else
                        {
                            command.Parameters[$"@{columnName}"].Value = DBNull.Value;
                        }
                    }
                    
                    await command.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Data inserted successfully into table: {TableName}", dynamicTable.TableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into table: {TableName}", dynamicTable.TableName);
                return false;
            }
        }

        private object ConvertValue(string value, string dataType)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;

            return dataType.ToLower() switch
            {
                "int" => int.TryParse(value, out var intResult) ? intResult : DBNull.Value,
                "decimal" => decimal.TryParse(value, out var decimalResult) ? decimalResult : DBNull.Value,
                "datetime" => DateTime.TryParse(value, out var dateResult) ? dateResult : DBNull.Value,
                "boolean" => bool.TryParse(value, out var boolResult) ? boolResult : DBNull.Value,
                "guid" => Guid.TryParse(value, out var guidResult) ? guidResult : DBNull.Value,
                _ => value
            };
        }

        public async Task<List<object>> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var offset = (page - 1) * pageSize;
                var sql = $@"
                    SELECT *
                    FROM [{tableName}]
                    ORDER BY Id
                    OFFSET {offset} ROWS
                    FETCH NEXT {pageSize} ROWS ONLY";

                using var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var results = new List<object>();
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
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columns[i]] = value;
                    }
                    results.Add(row);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data: {TableName}", tableName);
                throw;
            }
        }

        public async Task<int> GetTableRowCountAsync(string tableName)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = $"SELECT COUNT(*) FROM [{tableName}]";
                using var command = new SqlCommand(sql, connection);
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table row count: {TableName}", tableName);
                return 0;
            }
        }

        public async Task<bool> DeleteTableAsync(int id)
        {
            try
            {
                var table = await GetTableByIdAsync(id);
                if (table == null) return false;

                // Drop SQL table
                var result = await DeleteTableAsync(table.TableName);
                if (!result) return false;

                // Remove from database
                _context.DynamicTables.Remove(table);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteTableAsync(string tableName)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = $"DROP TABLE IF EXISTS [{tableName}]";
                using var command = new SqlCommand(sql, connection);
                
                await command.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Table deleted successfully: {TableName}", tableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {TableName}", tableName);
                return false;
            }
        }

        public async Task<int> GetTableDataCountAsync(string tableName)
        {
            return await GetTableRowCountAsync(tableName);
        }

        public async Task<bool> ExecuteSqlQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query: {Sql}", sql);
                return false;
            }
        }

        public async Task<DataTable> ExecuteSqlQueryWithResultAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(sql, connection);
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                using var adapter = new SqlDataAdapter(command);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query with result: {Sql}", sql);
                throw;
            }
        }

        public async Task<List<DynamicTable>> GetAllTablesAsync()
        {
            return await _context.DynamicTables
                .Include(t => t.Columns)
                .OrderByDescending(t => t.UploadDate)
                .ToListAsync();
        }

        public async Task<DynamicTable?> GetTableByIdAsync(int id)
        {
            return await _context.DynamicTables
                .Include(t => t.Columns)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<DynamicTable?> GetTableByNameAsync(string tableName)
        {
            return await _context.DynamicTables
                .Include(t => t.Columns)
                .FirstOrDefaultAsync(t => t.TableName == tableName);
        }

        public async Task<bool> UpdateTableDataAsync(string tableName, int rowId, Dictionary<string, object> data)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var setClause = string.Join(", ", data.Keys.Select(k => $"[{k}] = @{k}"));
                var updateSql = $"UPDATE [{tableName}] SET {setClause} WHERE Id = @Id";
                
                using var command = new SqlCommand(updateSql, connection);
                command.Parameters.AddWithValue("@Id", rowId);
                
                foreach (var kvp in data)
                {
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table data: {TableName}, RowId: {RowId}", tableName, rowId);
                return false;
            }
        }

        public async Task<bool> DeleteTableDataAsync(string tableName, int rowId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var deleteSql = $"DELETE FROM [{tableName}] WHERE Id = @Id";
                using var command = new SqlCommand(deleteSql, connection);
                command.Parameters.AddWithValue("@Id", rowId);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table data: {TableName}, RowId: {RowId}", tableName, rowId);
                return false;
            }
        }

        public async Task<byte[]> ExportTableDataAsync(string tableName, string format = "xlsx")
        {
            try
            {
                var data = await GetTableDataAsync(tableName, 1, int.MaxValue);
                var table = await GetTableByNameAsync(tableName);
                
                if (table == null || !data.Any()) return new byte[0];

                // Convert List<object> to List<Dictionary<string, object>>
                var convertedData = data.Cast<Dictionary<string, object>>().ToList();

                if (format.ToLower() == "xlsx")
                {
                    return await ExportToXlsxAsync(convertedData, table);
                }
                
                throw new NotSupportedException($"Export format '{format}' is not supported");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting table data: {TableName}", tableName);
                return new byte[0];
            }
        }

        private async Task<(List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)> AnalyzeExcelFileAsync(IFormFile file)
        {
            var headers = new List<string>();
            var dataTypes = new List<string>();
            var sampleData = new List<Dictionary<string, object>>();

            if (file.FileName.EndsWith(".xlsx"))
            {
                await AnalyzeXlsxFileAsync(file, headers, dataTypes, sampleData);
            }
            else if (file.FileName.EndsWith(".xls"))
            {
                await AnalyzeXlsFileAsync(file, headers, dataTypes, sampleData);
            }

            return (headers, dataTypes, sampleData);
        }

        private Task AnalyzeXlsxFileAsync(IFormFile file, List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)
        {
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets[0];

            if (worksheet == null) return Task.CompletedTask;

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            // Read headers
            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = GetCellValue(worksheet, 1, col);
                headers.Add(headerValue ?? $"Column{col}");
            }

            // Analyze data types and collect sample data
            for (int row = 2; row <= Math.Min(rowCount, 100); row++) // Sample first 100 rows
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    rowData[headers[col - 1]] = cellValue;
                }
                sampleData.Add(rowData);
            }

            // Determine data types based on sample data
            for (int col = 0; col < colCount; col++)
            {
                var dataType = DetermineDataType(sampleData.Select(r => r.Values.ElementAt(col)).ToList());
                dataTypes.Add(dataType);
            }

            return Task.CompletedTask;
        }

        private Task AnalyzeXlsFileAsync(IFormFile file, List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)
        {
            using var stream = file.OpenReadStream();
            IWorkbook workbook;
            
            if (file.FileName.EndsWith(".xlsx"))
                workbook = new XSSFWorkbook(stream);
            else
                workbook = new HSSFWorkbook(stream);

            var sheet = workbook.GetSheetAt(0);
            var rowCount = Math.Min(sheet.LastRowNum, 100); // Sample first 100 rows

            // Read headers
            var headerRow = sheet.GetRow(0);
            if (headerRow != null)
            {
                for (int col = 0; col < headerRow.LastCellNum; col++)
                {
                    var cell = headerRow.GetCell(col);
                    var headerValue = GetNpoiCellValue(cell);
                    headers.Add(headerValue ?? $"Column{col + 1}");
                }
            }

            // Analyze data types and collect sample data
            for (int row = 1; row <= rowCount; row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow == null) continue;

                var rowData = new Dictionary<string, object>();
                for (int col = 0; col < headers.Count; col++)
                {
                    var cell = sheetRow.GetCell(col);
                    var cellValue = GetNpoiCellValue(cell);
                    rowData[headers[col]] = cellValue;
                }
                sampleData.Add(rowData);
            }

            // Determine data types based on sample data
            for (int col = 0; col < headers.Count; col++)
            {
                var dataType = DetermineDataType(sampleData.Select(r => r.Values.ElementAt(col)).ToList());
                dataTypes.Add(dataType);
            }

            return Task.CompletedTask;
        }

        private string DetermineDataType(List<object> values)
        {
            var nonNullValues = values.Where(v => v != null).ToList();
            if (!nonNullValues.Any()) return "nvarchar(255)";

            var firstNonNullValue = nonNullValues.First();
            
            if (firstNonNullValue is DateTime) return "datetime2";
            if (firstNonNullValue is int || firstNonNullValue is long) return "int";
            if (firstNonNullValue is decimal || firstNonNullValue is double || firstNonNullValue is float) return "decimal(18,2)";
            if (firstNonNullValue is bool) return "bit";
            
            // Check if it's a date string
            if (firstNonNullValue is string strValue)
            {
                if (DateTime.TryParse(strValue, out _)) return "datetime2";
                if (strValue.Length > 255) return "nvarchar(max)";
            }
            
            return "nvarchar(255)";
        }

        private string GenerateTableName(string fileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var sanitizedName = SanitizeTableName(baseName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"Excel_{sanitizedName}_{timestamp}";
        }

        private string SanitizeTableName(string name)
        {
            // Remove invalid characters and replace with underscores
            var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            // Ensure it starts with a letter
            if (char.IsDigit(sanitized[0]))
                sanitized = "Tbl_" + sanitized;
            return sanitized;
        }

        private string SanitizeColumnName(string name)
        {
            // Remove invalid characters and replace with underscores
            var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            // Ensure it starts with a letter
            if (char.IsDigit(sanitized[0]))
                sanitized = "Col_" + sanitized;
            return sanitized;
        }

        private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            return cell.Value?.ToString();
        }

        private string GetNpoiCellValue(ICell? cell)
        {
            if (cell == null) return string.Empty;
            
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString();
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    return cell.StringCellValue;
                default:
                    return string.Empty;
            }
        }

        private async Task<byte[]> ExportToXlsxAsync(List<Dictionary<string, object>> data, DynamicTable table)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Data");

            // Add headers
            var columns = table.Columns.OrderBy(c => c.ColumnOrder).ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = columns[i].DisplayName;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Add data
            for (int row = 0; row < data.Count; row++)
            {
                for (int col = 0; col < columns.Count; col++)
                {
                    var columnName = columns[col].ColumnName;
                    if (data[row].ContainsKey(columnName))
                    {
                        worksheet.Cells[row + 2, col + 1].Value = data[row][columnName];
                    }
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }
    }
}
