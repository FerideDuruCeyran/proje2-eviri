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

                var createTableSql = GenerateCreateTableSql(dynamicTable);
                using var command = new SqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SQL table: {TableName}", dynamicTable.TableName);
                return false;
            }
        }

        public async Task<bool> InsertDataAsync(DynamicTable dynamicTable, List<Dictionary<string, object>> data)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                foreach (var row in data)
                {
                    var insertSql = GenerateInsertSql(dynamicTable, row);
                    using var command = new SqlCommand(insertSql, connection);
                    
                    // Add parameters
                    foreach (var kvp in row)
                    {
                        var column = dynamicTable.Columns.FirstOrDefault(c => c.ColumnName == kvp.Key);
                        if (column != null)
                        {
                            var parameterName = $"@{kvp.Key}";
                            var parameter = command.Parameters.AddWithValue(parameterName, kvp.Value ?? DBNull.Value);
                            
                            // Set parameter type if needed
                            if (column.DataType == "nvarchar" && column.MaxLength.HasValue)
                            {
                                parameter.SqlDbType = SqlDbType.NVarChar;
                                parameter.Size = column.MaxLength.Value;
                            }
                        }
                    }
                    
                    await command.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into table: {TableName}", dynamicTable.TableName);
                return false;
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

        public async Task<bool> DeleteTableAsync(int id)
        {
            try
            {
                var table = await GetTableByIdAsync(id);
                if (table == null) return false;

                // Drop SQL table
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var dropTableSql = $"IF OBJECT_ID('[{table.TableName}]', 'U') IS NOT NULL DROP TABLE [{table.TableName}]";
                using var command = new SqlCommand(dropTableSql, connection);
                await command.ExecuteNonQueryAsync();

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

        public async Task<List<Dictionary<string, object>>> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var offset = (page - 1) * pageSize;
                var selectSql = $"SELECT * FROM [{tableName}] ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                
                using var command = new SqlCommand(selectSql, connection);
                using var reader = await command.ExecuteReaderAsync();

                var data = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        if (columnName != null)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[columnName] = value ?? DBNull.Value;
                        }
                    }
                    data.Add(row);
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data: {TableName}", tableName);
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<int> GetTableDataCountAsync(string tableName)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var countSql = $"SELECT COUNT(*) FROM [{tableName}]";
                using var command = new SqlCommand(countSql, connection);
                var count = await command.ExecuteScalarAsync();
                
                return Convert.ToInt32(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data count: {TableName}", tableName);
                return 0;
            }
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

                if (format.ToLower() == "xlsx")
                {
                    return await ExportToXlsxAsync(data, table);
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

        private async Task AnalyzeXlsxFileAsync(IFormFile file, List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)
        {
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets[0];

            if (worksheet == null) return;

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
        }

        private async Task AnalyzeXlsFileAsync(IFormFile file, List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)
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

        private string GenerateCreateTableSql(DynamicTable dynamicTable)
        {
            var columnDefinitions = dynamicTable.Columns
                .OrderBy(c => c.ColumnOrder)
                .Select(c => $"[{c.ColumnName}] {c.DataType} {(c.IsRequired ? "NOT NULL" : "NULL")}")
                .ToList();

            // Add Id column at the beginning
            columnDefinitions.Insert(0, "[Id] int IDENTITY(1,1) PRIMARY KEY");

            var createTableSql = $@"
                CREATE TABLE [{dynamicTable.TableName}] (
                    {string.Join(",\n                    ", columnDefinitions)}
                )";

            return createTableSql;
        }

        private string GenerateInsertSql(DynamicTable dynamicTable, Dictionary<string, object> rowData)
        {
            var columns = rowData.Keys.Select(k => $"[{k}]").ToList();
            var parameters = rowData.Keys.Select(k => $"@{k}").ToList();

            var insertSql = $@"
                INSERT INTO [{dynamicTable.TableName}] ({string.Join(", ", columns)})
                VALUES ({string.Join(", ", parameters)})";

            return insertSql;
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
