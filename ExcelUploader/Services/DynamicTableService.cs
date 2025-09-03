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
        private readonly IExcelAnalyzerService _excelAnalyzerService;

        public DynamicTableService(ApplicationDbContext context, IConfiguration configuration, ILogger<DynamicTableService> logger, IExcelAnalyzerService excelAnalyzerService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _excelAnalyzerService = excelAnalyzerService;
        }

        public async Task<DynamicTable> CreateTableFromExcelAsync(IFormFile file, string uploadedBy, int? databaseConnectionId = null, string? description = null)
        {
            try
            {
                // Generate table name from file name
                var tableName = GenerateTableName(file.FileName);
                _logger.LogInformation("Generated table name: {TableName} from file: {FileName}", tableName, file.FileName);
                
                // Check if exact table exists in database
                var exactTableExists = await CheckExactTableExistsAsync(tableName, databaseConnectionId);
                _logger.LogInformation("Exact table exists check result: {ExactTableExists} for table: {TableName}", exactTableExists, tableName);
                
                if (exactTableExists)
                {
                    // Table exists - insert data into existing table
                    var insertedRows = await InsertDataIntoExistingTableAsync(file, tableName, databaseConnectionId);
                    
                    // Create or update tracking record
                    var existingTableId = await GetTableIdByNameAsync(tableName);
                    DynamicTable dynamicTable;
                    
                    if (existingTableId.HasValue)
                    {
                        // Update existing tracking record
                        dynamicTable = await GetTableByIdAsync(existingTableId.Value);
                        if (dynamicTable != null)
                        {
                            dynamicTable.RowCount = insertedRows;
                            dynamicTable.IsProcessed = true;
                            dynamicTable.ProcessedDate = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        // Create new tracking record for existing table
                        var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                        dynamicTable = new DynamicTable
                        {
                            TableName = tableName,
                            FileName = file.FileName,
                            UploadedBy = uploadedBy,
                            Description = description ?? string.Empty,
                            RowCount = insertedRows,
                            ColumnCount = headers.Count,
                            UploadDate = DateTime.UtcNow,
                            IsProcessed = true,
                            ProcessedDate = DateTime.UtcNow
                        };
                        
                        // Create table columns for tracking
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
                        
                        _context.DynamicTables.Add(dynamicTable);
                        await _context.SaveChangesAsync();
                    }
                    
                    _logger.LogInformation("Data inserted into existing table: {TableName}, {InsertedRows} rows", tableName, insertedRows);
                    return dynamicTable;
                }
                else
                {
                    // Table doesn't exist - create new table
                    var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                    var allData = await ReadAllExcelDataAsync(file);
                    
                    // Create DynamicTable entity
                    var dynamicTable = new DynamicTable
                    {
                        TableName = tableName,
                        FileName = file.FileName,
                        UploadedBy = uploadedBy,
                        Description = description ?? string.Empty,
                        RowCount = allData.Count,
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
                    var sqlTableCreated = await CreateSqlTableAsync(dynamicTable, databaseConnectionId);
                    if (!sqlTableCreated)
                    {
                        throw new InvalidOperationException("SQL table creation failed");
                    }

                    // Insert data into newly created table
                    var dataInserted = await InsertDataAsync(dynamicTable, allData, databaseConnectionId);
                    if (!dataInserted)
                    {
                        throw new InvalidOperationException("Data insertion failed");
                    }

                    // Mark as processed
                    dynamicTable.IsProcessed = true;
                    dynamicTable.ProcessedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New table created and data inserted: {TableName}", dynamicTable.TableName);
                    return dynamicTable;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dynamic table from Excel file: {FileName}", file.FileName);
                throw;
            }
        }

        // Helper method to check if exact table exists in database
        public async Task<bool> CheckExactTableExistsAsync(string tableName, int? databaseConnectionId = null)
        {
            try
            {
                string connectionString;
                
                if (databaseConnectionId.HasValue)
                {
                    // Use specific database connection
                    var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId.Value);
                    if (dbConnection == null)
                        throw new ArgumentException($"Database connection with ID {databaseConnectionId.Value} not found");
                    
                    connectionString = $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
                }
                else
                {
                    // Use default connection
                    connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Default connection string not found");
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Simple check for exact table name (case-insensitive)
                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE LOWER(TABLE_NAME) = LOWER(@TableName)";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);

                var count = await command.ExecuteScalarAsync();
                var exists = Convert.ToInt32(count) > 0;
                
                _logger.LogInformation("Table '{TableName}' exists check result: {Exists}", tableName, exists);
                
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if exact table exists: {TableName}", tableName);
                return false;
            }
        }

        // Get existing table ID by name
        public async Task<int?> GetTableIdByNameAsync(string tableName)
        {
            try
            {
                // Simple exact match
                var table = await _context.DynamicTables
                    .FirstOrDefaultAsync(t => t.TableName == tableName);
                
                return table?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table ID by name: {TableName}", tableName);
                return null;
            }
        }

        public async Task<bool> CreateSqlTableAsync(DynamicTable dynamicTable, int? databaseConnectionId = null)
        {
            try
            {
                string connectionString;
                
                if (databaseConnectionId.HasValue)
                {
                    // Use specific database connection
                    var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId.Value);
                    if (dbConnection == null)
                        throw new ArgumentException($"Database connection with ID {databaseConnectionId.Value} not found");
                    
                    connectionString = $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
                }
                else
                {
                    // Use default connection
                    connectionString = _configuration.GetConnectionString("DefaultConnection");
                }
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if table already exists
                var tableExists = await CheckExactTableExistsAsync(dynamicTable.TableName, databaseConnectionId);
                if (tableExists)
                {
                    // Table already exists, don't create it
                    _logger.LogInformation("Table already exists: {TableName}, skipping creation", dynamicTable.TableName);
                    return true;
                }

                // Build CREATE TABLE SQL
                var createTableSql = BuildCreateTableSql(dynamicTable);
                
                using var command = new SqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("SQL table created successfully: {TableName} using connection {ConnectionId}", 
                    dynamicTable.TableName, databaseConnectionId ?? 0);
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
            // Handle full data type strings from DetermineDataType
            var lowerDataType = dataType.ToLower();
            
            if (lowerDataType.StartsWith("nvarchar(") || lowerDataType.StartsWith("varchar("))
                return dataType; // Return as-is
            if (lowerDataType.StartsWith("decimal("))
                return dataType; // Return as-is
            if (lowerDataType == "datetime2" || lowerDataType == "datetime")
                return "DATETIME2";
            if (lowerDataType == "int" || lowerDataType == "bigint")
                return "INT";
            if (lowerDataType == "bit")
                return "BIT";
            if (lowerDataType == "uniqueidentifier")
                return "UNIQUEIDENTIFIER";
            
            // Handle simple type names
            return lowerDataType switch
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

        public async Task<bool> InsertDataAsync(DynamicTable dynamicTable, List<Dictionary<string, object>> data, int? databaseConnectionId = null)
        {
            try
            {
                string connectionString;
                
                if (databaseConnectionId.HasValue)
                {
                    // Use specific database connection
                    var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId.Value);
                    if (dbConnection == null)
                        throw new ArgumentException($"Database connection with ID {databaseConnectionId.Value} not found");
                    
                    connectionString = $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
                }
                else
                {
                    // Use default connection
                    connectionString = _configuration.GetConnectionString("DefaultConnection");
                }
                
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

                // Insert data rows with batch processing for better performance
                var batchSize = 1000; // Process in batches of 1000 rows
                var totalRows = data.Count;
                
                for (int batchStart = 0; batchStart < totalRows; batchStart += batchSize)
                {
                    var batchEnd = Math.Min(batchStart + batchSize, totalRows);
                    var batch = data.Skip(batchStart).Take(batchSize).ToList();
                    
                    foreach (var row in batch)
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
                    
                    _logger.LogInformation("Processed batch {BatchNumber} for table {TableName}, inserted {ProcessedRows}/{TotalRows} rows", 
                        (batchStart / batchSize) + 1, dynamicTable.TableName, batchEnd, totalRows);
                }

                _logger.LogInformation("Data inserted successfully into table: {TableName}, {TotalRows} rows", dynamicTable.TableName, totalRows);
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

        public async Task<(List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)> AnalyzeExcelFileAsync(IFormFile file)
        {
            // ExcelAnalyzerService kullanarak dosyayı analiz et
            var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(file);
                
            return (analysisResult.Headers, analysisResult.DataTypes, analysisResult.SampleData);
        }

        private string GenerateTableName(string fileName)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var tableName = nameWithoutExtension
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_");
            
            // Remove timestamp patterns from the table name
            tableName = System.Text.RegularExpressions.Regex.Replace(tableName, @"_\d{8}_\d{6}$", "");
            tableName = System.Text.RegularExpressions.Regex.Replace(tableName, @"_\d{8}_\d{4}$", "");
            tableName = System.Text.RegularExpressions.Regex.Replace(tableName, @"_\d{8}$", "");
            
            return tableName;
        }

        public string SanitizeColumnName(string name)
        {
            // Remove invalid characters and replace with underscores
            var sanitized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            
            // Ensure it starts with a letter
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "Col_" + sanitized;
            
            // Limit length to 128 characters (SQL Server column name limit)
            if (sanitized.Length > 128)
            {
                sanitized = sanitized.Substring(0, 128);
                // Ensure it doesn't end with underscore
                sanitized = sanitized.TrimEnd('_');
            }
            
            // If empty after sanitization, provide a default name
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                
            return sanitized;
        }

        // New method to read all data from Excel file
        public async Task<List<Dictionary<string, object>>> ReadAllExcelDataAsync(IFormFile file)
        {
            var allData = new List<Dictionary<string, object>>();

            if (file.FileName.EndsWith(".xlsx"))
            {
                await ReadAllXlsxDataAsync(file, allData);
            }
            else if (file.FileName.EndsWith(".xls"))
            {
                await ReadAllXlsDataAsync(file, allData);
            }

            return allData;
        }

        private Task ReadAllXlsxDataAsync(IFormFile file, List<Dictionary<string, object>> allData)
        {
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets[0];

            if (worksheet == null) return Task.CompletedTask;

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            if (rowCount < 2) return Task.CompletedTask; // Need at least header + 1 data row

            // Read headers (first row - A1 starts from row 1, col 1)
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = GetCellValue(worksheet, 1, col);
                if (!string.IsNullOrEmpty(headerValue))
                {
                    headers.Add(headerValue);
                }
                else
                {
                    headers.Add($"Column{col}");
                }
            }

            // Read all data rows (starting from row 2)
            for (int row = 2; row <= rowCount; row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    // Don't convert null to empty string, keep null values
                    rowData[headers[col - 1]] = cellValue;
                }
                allData.Add(rowData);
            }
            
            return Task.CompletedTask;
        }

        private Task ReadAllXlsDataAsync(IFormFile file, List<Dictionary<string, object>> allData)
        {
            using var stream = file.OpenReadStream();
            IWorkbook workbook;
            
            // For .xls files, always use HSSFWorkbook
            workbook = new HSSFWorkbook(stream);

            var sheet = workbook.GetSheetAt(0);
            var rowCount = sheet.LastRowNum;

            if (rowCount < 1) return Task.CompletedTask; // Need at least header + 1 data row

            // Read headers (first row - A1 starts from row 0, col 0)
            var headers = new List<string>();
            var headerRow = sheet.GetRow(0);
            if (headerRow != null)
            {
                for (int col = 0; col < headerRow.LastCellNum; col++)
                {
                    var cell = headerRow.GetCell(col);
                    var headerValue = GetNpoiCellValue(cell);
                    if (headerValue != null && !string.IsNullOrEmpty(headerValue.ToString()))
                    {
                        headers.Add(headerValue.ToString());
                    }
                    else
                    {
                        headers.Add($"Column{col + 1}");
                    }
                }
            }

            // Read all data rows (starting from row 1)
            for (int row = 1; row <= rowCount; row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow == null) continue;

                var rowData = new Dictionary<string, object>();
                for (int col = 0; col < headers.Count; col++)
                {
                    var cell = sheetRow.GetCell(col);
                    var cellValue = GetNpoiCellValue(cell);
                    // Don't convert null to empty string, keep null values
                    rowData[headers[col]] = cellValue;
                }
                allData.Add(rowData);
            }
            
            return Task.CompletedTask;
        }

        // Simplified method to insert data into existing table
        public async Task<int> InsertDataIntoExistingTableAsync(IFormFile file, string existingTableName, int? databaseConnectionId = null)
        {
            try
            {
                string connectionString;
                
                if (databaseConnectionId.HasValue)
                {
                    // Use specific database connection
                    var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId.Value);
                    if (dbConnection == null)
                        throw new ArgumentException($"Database connection with ID {databaseConnectionId.Value} not found");
                    
                    connectionString = $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
                }
                else
                {
                    // Use default connection
                    connectionString = _configuration.GetConnectionString("DefaultConnection");
                }
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Read Excel data
                var (headers, dataTypes, _) = await AnalyzeExcelFileAsync(file);
                var allData = await ReadAllExcelDataAsync(file);

                // Find the actual table name (case-insensitive)
                var actualTableName = await FindActualTableNameAsync(connection, existingTableName);
                if (string.IsNullOrEmpty(actualTableName))
                {
                    throw new InvalidOperationException($"Table '{existingTableName}' not found in database");
                }

                _logger.LogInformation("Found actual table name: '{ActualTableName}' for requested name: '{RequestedName}'", actualTableName, existingTableName);

                // Get existing table structure from database
                var existingColumns = await GetTableColumnsAsync(connection, actualTableName);
                if (!existingColumns.Any())
                {
                    throw new InvalidOperationException($"Table '{actualTableName}' not found or has no columns");
                }

                _logger.LogInformation("Found {ColumnCount} columns in existing table '{TableName}': {Columns}", 
                    existingColumns.Count, actualTableName, string.Join(", ", existingColumns));
                _logger.LogInformation("Excel file has {HeaderCount} headers: {Headers}", 
                    headers.Count, string.Join(", ", headers));
                _logger.LogInformation("Excel file has {DataRowCount} data rows", allData.Count);

                // Simple column matching - only exact matches
                var matchingColumns = new List<(string tableColumn, string excelHeader, int excelIndex)>();
                
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    var sanitizedHeader = SanitizeColumnName(header);
                    
                    // Only exact match (case-insensitive)
                    var matchingColumn = existingColumns.FirstOrDefault(c => 
                        string.Equals(c, sanitizedHeader, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingColumn != null)
                    {
                        matchingColumns.Add((matchingColumn, header, i));
                        _logger.LogInformation("Matched Excel header '{ExcelHeader}' to table column '{TableColumn}'", header, matchingColumn);
                    }
                    else
                    {
                        _logger.LogWarning("Excel header '{ExcelHeader}' not found in table columns", header);
                    }
                }

                if (!matchingColumns.Any())
                {
                    throw new InvalidOperationException($"No matching columns found between Excel headers and table '{existingTableName}' columns");
                }

                // Build INSERT statement for matching columns only
                var columnNames = string.Join(", ", matchingColumns.Select(mc => $"[{mc.tableColumn}]"));
                var parameterNames = string.Join(", ", matchingColumns.Select(mc => $"@{mc.tableColumn}"));

                var insertSql = $"INSERT INTO [{actualTableName}] ({columnNames}) VALUES ({parameterNames})";
                _logger.LogInformation("Using INSERT SQL: {InsertSql}", insertSql);

                using var command = new SqlCommand(insertSql, connection);
                
                // Add parameters for matching columns
                foreach (var matchingColumn in matchingColumns)
                {
                    command.Parameters.AddWithValue($"@{matchingColumn.tableColumn}", DBNull.Value);
                }

                // Insert data rows
                var insertedRows = 0;
                
                foreach (var row in allData)
                {
                    // Set values for matching columns
                    foreach (var matchingColumn in matchingColumns)
                    {
                        var headerName = matchingColumn.excelHeader;
                        var columnName = matchingColumn.tableColumn;
                        var excelIndex = matchingColumn.excelIndex;
                        
                        if (row.ContainsKey(headerName))
                        {
                            var value = row[headerName];
                            if (value == null || string.IsNullOrEmpty(value.ToString()))
                            {
                                command.Parameters[$"@{columnName}"].Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters[$"@{columnName}"].Value = ConvertValue(value.ToString() ?? "", dataTypes[excelIndex]);
                            }
                        }
                        else
                        {
                            command.Parameters[$"@{columnName}"].Value = DBNull.Value;
                        }
                    }
                    
                    await command.ExecuteNonQueryAsync();
                    insertedRows++;
                }

                _logger.LogInformation("Data insertion completed successfully into existing table: {TableName}, {InsertedRows} rows", 
                    existingTableName, insertedRows);
                return insertedRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into existing table: {TableName}. Error: {ErrorMessage}", existingTableName, ex.Message);
                throw;
            }
        }

        // Helper method to find actual table name (case-insensitive)
        private async Task<string?> FindActualTableNameAsync(SqlConnection connection, string tableName)
        {
            try
            {
                var sql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE LOWER(TABLE_NAME) = LOWER(@TableName)";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return reader.GetString(0);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding actual table name: {TableName}", tableName);
                return null;
            }
        }

        // Helper method to get table columns from database
        private async Task<List<string>> GetTableColumnsAsync(SqlConnection connection, string tableName)
        {
            var columns = new List<string>();
            
            var sql = @"
                SELECT COLUMN_NAME 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName 
                ORDER BY ORDINAL_POSITION";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TableName", tableName);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }

        private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            return cell.Value?.ToString();
        }

        private object GetNpoiCellValue(ICell? cell)
        {
            if (cell == null) return null;
            
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue;
                    return cell.NumericCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Formula:
                    return cell.StringCellValue;
                default:
                    return null;
            }
        }

        // Interface implementation methods
        public async Task<bool> CheckTableExistsForInsertAsync(string tableName, int? databaseConnectionId = null)
        {
            return await CheckExactTableExistsAsync(tableName, databaseConnectionId);
        }

        public async Task<int> InsertDataIntoExistingTableWithSameNameAsync(IFormFile file, string tableName, int? databaseConnectionId = null)
        {
            return await InsertDataIntoExistingTableAsync(file, tableName, databaseConnectionId);
        }
    }
}
