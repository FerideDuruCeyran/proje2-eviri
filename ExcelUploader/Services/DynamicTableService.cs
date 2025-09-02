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
                // Generate table name
                var tableName = GenerateTableName(file.FileName);
                _logger.LogInformation("Generated table name: {TableName} from file: {FileName}", tableName, file.FileName);
                
                // Check if table exists in database (exact name or similar name)
                var exactTableExists = await CheckExactTableExistsAsync(tableName, databaseConnectionId);
                _logger.LogInformation("Exact table exists check result: {ExactTableExists} for table: {TableName}", exactTableExists, tableName);
                
                if (exactTableExists)
                {
                    // Table already exists in database, find the actual table name and insert data directly
                    var actualTableName = await FindExistingTableNameAsync(tableName, databaseConnectionId);
                    if (actualTableName == null)
                    {
                        throw new InvalidOperationException($"Table '{tableName}' not found in database");
                    }
                    
                    var insertedRows = await InsertDataIntoExistingTableAsync(file, actualTableName, databaseConnectionId);
                    
                    // Create or get DynamicTable record for tracking
                    var trackingTableId = await GetTableIdByNameAsync(tableName);
                    DynamicTable trackingTable;
                    
                    if (trackingTableId.HasValue)
                    {
                        // Get existing DynamicTable record
                        trackingTable = await GetTableByIdAsync(trackingTableId.Value);
                        if (trackingTable == null)
                        {
                            throw new InvalidOperationException("Mevcut tablo bulunamadı");
                        }
                        trackingTable.RowCount = insertedRows;
                        trackingTable.IsProcessed = true;
                        trackingTable.ProcessedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Create new DynamicTable record for tracking
                        var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                        trackingTable = new DynamicTable
                        {
                            TableName = actualTableName,
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
                            trackingTable.Columns.Add(column);
                        }
                        
                        _context.DynamicTables.Add(trackingTable);
                        await _context.SaveChangesAsync();
                    }
                    
                    _logger.LogInformation("Data inserted into existing table: {TableName}, {InsertedRows} rows", actualTableName, insertedRows);
                    return trackingTable;
                }
                
                // Check if table exists in our tracking system
                var tableExists = await TableExistsAsync(tableName, databaseConnectionId);
                var existingTableId = await GetTableIdByNameAsync(tableName);
                
                DynamicTable dynamicTable;
                
                if (tableExists && existingTableId.HasValue)
                {
                    // Table exists in tracking system, get existing table
                    dynamicTable = await GetTableByIdAsync(existingTableId.Value);
                    if (dynamicTable == null)
                    {
                        throw new InvalidOperationException("Mevcut tablo bulunamadı");
                    }
                    
                    // Read Excel data for insertion (use all data, not just sample)
                    var allData = await ReadAllExcelDataAsync(file);
                    
                    // Insert data into existing table
                    var dataInserted = await InsertDataAsync(dynamicTable, allData, databaseConnectionId);
                    if (!dataInserted)
                    {
                        throw new InvalidOperationException("Data insertion failed");
                    }
                    
                    // Update row count
                    dynamicTable.RowCount = allData.Count;
                    dynamicTable.IsProcessed = true;
                    dynamicTable.ProcessedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Data inserted into existing table: {TableName}, {RowCount} rows", dynamicTable.TableName, allData.Count);
                }
                else
                {
                    // Read Excel headers and determine data types
                    var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                    var allData = await ReadAllExcelDataAsync(file);
                    
                    // Create DynamicTable entity
                    dynamicTable = new DynamicTable
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

                    // Create SQL table (or find existing table with similar name)
                    var sqlTableCreated = await CreateSqlTableAsync(dynamicTable, databaseConnectionId);
                    if (!sqlTableCreated)
                    {
                        throw new InvalidOperationException("SQL table creation failed");
                    }

                    // Check if we found an existing table with similar name
                    if (await TableExistsAsync(dynamicTable.TableName, databaseConnectionId))
                    {
                        // Insert data into existing table
                        var insertedRows = await InsertDataIntoExistingTableAsync(file, dynamicTable.TableName, databaseConnectionId);
                        dynamicTable.RowCount = insertedRows;
                        _logger.LogInformation("Data inserted into existing table with similar name: {TableName}, {InsertedRows} rows", dynamicTable.TableName, insertedRows);
                    }
                    else
                    {
                        // Insert data into newly created table
                        var dataInserted = await InsertDataAsync(dynamicTable, allData, databaseConnectionId);
                        if (!dataInserted)
                        {
                            throw new InvalidOperationException("Data insertion failed");
                        }
                    }

                    _logger.LogInformation("New table created and data inserted: {TableName}", dynamicTable.TableName);
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

        // New method for two-stage process: Stage 1 - Create table structure only
        public async Task<DynamicTable> CreateTableStructureAsync(IFormFile file, string uploadedBy, int? databaseConnectionId = null, string? description = null)
        {
            try
            {
                _logger.LogInformation("CreateTableStructureAsync called with file: {FileName}, User: {User}", file.FileName, uploadedBy);
                
                // Generate table name
                var tableName = GenerateTableName(file.FileName);
                _logger.LogInformation("Generated table name: {TableName}", tableName);
                
                // Check if table already exists
                _logger.LogInformation("Checking if table exists: {TableName}", tableName);
                var tableExists = await TableExistsAsync(tableName, databaseConnectionId);
                var existingTableId = await GetTableIdByNameAsync(tableName);
                _logger.LogInformation("Table exists: {TableExists}, Existing table ID: {ExistingTableId}", tableExists, existingTableId);
                
                DynamicTable dynamicTable;
                
                if (tableExists && existingTableId.HasValue)
                {
                    _logger.LogInformation("Table exists, getting existing table with ID: {TableId}", existingTableId.Value);
                    // Table exists, get existing table
                    dynamicTable = await GetTableByIdAsync(existingTableId.Value);
                    if (dynamicTable == null)
                    {
                        _logger.LogError("Existing table not found with ID: {TableId}", existingTableId.Value);
                        throw new InvalidOperationException("Mevcut tablo bulunamadı");
                    }
                    
                    _logger.LogInformation("Existing table found: {TableName}", dynamicTable.TableName);
                    return dynamicTable;
                }
                else
                {
                    _logger.LogInformation("Creating new table structure for: {TableName}", tableName);
                    
                    // Read Excel headers and determine data types
                    _logger.LogInformation("Analyzing Excel file: {FileName}", file.FileName);
                    var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                    _logger.LogInformation("Excel analysis completed. Headers: {HeaderCount}, Sample data rows: {SampleDataCount}", headers.Count, sampleData.Count);
                    
                    // Create DynamicTable entity
                    dynamicTable = new DynamicTable
                    {
                        TableName = tableName,
                        FileName = file.FileName,
                        UploadedBy = uploadedBy,
                        Description = description ?? string.Empty,
                        RowCount = sampleData.Count,
                        ColumnCount = headers.Count,
                        UploadDate = DateTime.UtcNow,
                        IsProcessed = false // Not processed yet
                    };

                    _logger.LogInformation("Created DynamicTable entity: {TableName}, Columns: {ColumnCount}", dynamicTable.TableName, dynamicTable.ColumnCount);

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
                        _logger.LogDebug("Added column: {ColumnName} ({DataType})", column.ColumnName, column.DataType);
                    }

                    // Save to database
                    _logger.LogInformation("Saving DynamicTable to database");
                    _context.DynamicTables.Add(dynamicTable);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("DynamicTable saved to database with ID: {TableId}", dynamicTable.Id);

                    // Create SQL table structure only
                    _logger.LogInformation("Creating SQL table structure for: {TableName}", dynamicTable.TableName);
                    var sqlTableCreated = await CreateSqlTableAsync(dynamicTable, databaseConnectionId);
                    if (!sqlTableCreated)
                    {
                        _logger.LogError("SQL table creation failed for: {TableName}", dynamicTable.TableName);
                        throw new InvalidOperationException("SQL table creation failed");
                    }

                    _logger.LogInformation("SQL table created successfully: {TableName}", dynamicTable.TableName);

                    // Check if CreateSqlTableAsync found an existing table and updated the table name
                    if (dynamicTable.TableName != tableName)
                    {
                        _logger.LogInformation("Table name was updated from {OriginalName} to {NewName}", tableName, dynamicTable.TableName);
                        
                        // An existing table was found, we need to check if we already have a tracking record
                        var existingTrackingTable = await _context.DynamicTables
                            .FirstOrDefaultAsync(t => t.TableName == dynamicTable.TableName);
                        
                        if (existingTrackingTable != null)
                        {
                            // Use the existing tracking record
                            _logger.LogInformation("Using existing tracking record for table: {TableName}", dynamicTable.TableName);
                            return existingTrackingTable;
                        }
                        else
                        {
                            // Update our tracking record to use the existing table name
                            _logger.LogInformation("Updated tracking record to use existing table: {TableName}", dynamicTable.TableName);
                        }
                    }

                    _logger.LogInformation("Table structure created successfully: {TableName}", dynamicTable.TableName);
                }
                
                return dynamicTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table structure from Excel file: {FileName}. Error: {ErrorMessage}", file.FileName, ex.Message);
                throw;
            }
        }

        // Helper method to extract base table name without timestamp
        private string GetBaseTableName(string tableName)
        {
            // Remove timestamp pattern like _YYYYMMDD_HHMMSS or _YYYYMMDD_HHMM
            var baseName = System.Text.RegularExpressions.Regex.Replace(tableName, @"_\d{8}_\d{6}$", "");
            baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"_\d{8}_\d{4}$", "");
            baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"_\d{8}$", "");
            return baseName;
        }

        // Helper method to find the actual existing table name
        public async Task<string?> FindExistingTableNameAsync(string tableName, int? databaseConnectionId = null)
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

                // Check for both the exact table name and with Excel_ prefix
                var sql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @TableName OR TABLE_NAME = @TableNameWithPrefix";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@TableNameWithPrefix", $"Excel_{tableName}");

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return reader.GetString(0);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding existing table name: {TableName}", tableName);
                return null;
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

                // First, let's check what tables exist with similar names for debugging
                var debugSql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME LIKE @TableNamePattern
                    ORDER BY TABLE_NAME";

                using var debugCommand = new SqlCommand(debugSql, connection);
                debugCommand.Parameters.AddWithValue("@TableNamePattern", $"%{tableName}%");
                
                var existingTables = new List<string>();
                using var debugReader = await debugCommand.ExecuteReaderAsync();
                while (await debugReader.ReadAsync())
                {
                    existingTables.Add(debugReader.GetString(0));
                }
                
                _logger.LogInformation("Found tables with similar names to '{TableName}': {ExistingTables}", tableName, string.Join(", ", existingTables));

                // Check for both the exact table name and with Excel_ prefix
                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @TableName OR TABLE_NAME = @TableNameWithPrefix";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@TableNameWithPrefix", $"Excel_{tableName}");

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

        // Helper method to find existing table with similar name
        private async Task<string?> FindExistingTableWithSimilarNameAsync(SqlConnection connection, string baseTableName)
        {
            try
            {
                var sql = @"
                    SELECT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME LIKE @BaseTableName + '%'
                    ORDER BY TABLE_NAME";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BaseTableName", baseTableName);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return reader.GetString(0);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding existing table with similar name: {BaseTableName}", baseTableName);
                return null;
            }
        }

        // Check if table exists in database
        public async Task<bool> TableExistsAsync(string tableName, int? databaseConnectionId = null)
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

                // Get base table name without timestamp
                var baseTableName = GetBaseTableName(tableName);

                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME LIKE @BaseTableName + '%'";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BaseTableName", baseTableName);

                var count = await command.ExecuteScalarAsync();
                return Convert.ToInt32(count) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if table exists: {TableName}", tableName);
                return false;
            }
        }

        // Get existing table ID by name
        public async Task<int?> GetTableIdByNameAsync(string tableName)
        {
            try
            {
                // Get base table name without timestamp
                var baseTableName = GetBaseTableName(tableName);
                
                // Get all tables and filter in memory to avoid EF translation issues
                var allTables = await _context.DynamicTables.ToListAsync();
                var table = allTables.FirstOrDefault(t => GetBaseTableName(t.TableName) == baseTableName);
                
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
                _logger.LogInformation("CreateSqlTableAsync called for table: {TableName}, Connection ID: {ConnectionId}", 
                    dynamicTable.TableName, databaseConnectionId);
                
                string connectionString;
                
                if (databaseConnectionId.HasValue)
                {
                    // Use specific database connection
                    _logger.LogInformation("Using specific database connection ID: {ConnectionId}", databaseConnectionId.Value);
                    var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId.Value);
                    if (dbConnection == null)
                    {
                        _logger.LogError("Database connection not found with ID: {ConnectionId}", databaseConnectionId.Value);
                        throw new ArgumentException($"Database connection with ID {databaseConnectionId.Value} not found");
                    }
                    
                    connectionString = $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
                    _logger.LogInformation("Using custom connection string for server: {ServerName}:{Port}", dbConnection.ServerName, dbConnection.Port);
                }
                else
                {
                    // Use default connection
                    _logger.LogInformation("Using default connection string");
                    connectionString = _configuration.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        _logger.LogError("Default connection string is null or empty");
                        throw new InvalidOperationException("Default connection string not found");
                    }
                }
                
                _logger.LogInformation("Opening database connection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully");

                // First check if exact table name already exists
                _logger.LogInformation("Checking if exact table exists: {TableName}", dynamicTable.TableName);
                var exactTableExists = await CheckExactTableExistsAsync(dynamicTable.TableName, databaseConnectionId);
                if (exactTableExists)
                {
                    _logger.LogInformation("Exact table exists, finding actual table name");
                    // Find the actual existing table name
                    var actualTableName = await FindExistingTableNameAsync(dynamicTable.TableName, databaseConnectionId);
                    if (actualTableName != null)
                    {
                        // Update the dynamic table to use the actual existing table name
                        dynamicTable.TableName = actualTableName;
                        _logger.LogInformation("Exact table name already exists: {ActualTableName}, will use for data insertion", actualTableName);
                        return true; // Return true to indicate "table is ready for data insertion"
                    }
                }

                // Check if a table with similar name already exists
                _logger.LogInformation("Checking for table with similar name");
                var baseTableName = GetBaseTableName(dynamicTable.TableName);
                var existingTableName = await FindExistingTableWithSimilarNameAsync(connection, baseTableName);
                
                if (!string.IsNullOrEmpty(existingTableName))
                {
                    // Table with similar name exists, update the dynamic table to use existing table name
                    dynamicTable.TableName = existingTableName;
                    _logger.LogInformation("Found existing table with similar name: {ExistingTableName}, will use for data insertion", existingTableName);
                    return true; // Return true to indicate "table is ready for data insertion"
                }

                // Build CREATE TABLE SQL
                _logger.LogInformation("Building CREATE TABLE SQL for: {TableName}", dynamicTable.TableName);
                var createTableSql = BuildCreateTableSql(dynamicTable);
                _logger.LogDebug("CREATE TABLE SQL: {Sql}", createTableSql);
                
                using var command = new SqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("SQL table created successfully: {TableName} using connection {ConnectionId}", 
                    dynamicTable.TableName, databaseConnectionId ?? 0);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SQL table: {TableName}. Error: {ErrorMessage}", dynamicTable.TableName, ex.Message);
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

        public async Task<List<object>> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50, int? databaseConnectionId = null)
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

                // First check if table exists
                var tableExistsSql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @TableName";
                
                using var tableCheckCommand = new SqlCommand(tableExistsSql, connection);
                tableCheckCommand.Parameters.AddWithValue("@TableName", tableName);
                var tableExists = await tableCheckCommand.ExecuteScalarAsync();
                
                if (Convert.ToInt32(tableExists) == 0)
                {
                    throw new InvalidOperationException($"Table '{tableName}' does not exist in the database");
                }

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

        public async Task<(List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)> AnalyzeExcelFileAsync(IFormFile file)
        {
            try
            {
                _logger.LogInformation("AnalyzeExcelFileAsync called with file: {FileName}", file.FileName);
                
                // ExcelAnalyzerService kullanarak dosyayı analiz et
                var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(file);
                
                _logger.LogInformation("Excel analysis completed. Headers: {HeaderCount}, Data types: {DataTypeCount}, Sample data: {SampleDataCount}", 
                    analysisResult.Headers.Count, analysisResult.DataTypes.Count, analysisResult.SampleData.Count);
                
                return (analysisResult.Headers, analysisResult.DataTypes, analysisResult.SampleData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing Excel file: {FileName}. Error: {ErrorMessage}", file.FileName, ex.Message);
                throw;
            }
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
                if (!string.IsNullOrEmpty(headerValue))
                {
                    headers.Add(headerValue);
                }
                else
                {
                    headers.Add($"Column{col}");
                }
            }

            // Analyze data types and collect sample data
            for (int row = 2; row <= Math.Min(rowCount, 100); row++) // Sample first 100 rows
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    rowData[headers[col - 1]] = cellValue ?? string.Empty;
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
            
            // For .xls files, always use HSSFWorkbook
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
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        headers.Add(headerValue);
                    }
                    else
                    {
                        headers.Add($"Column{col + 1}");
                    }
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
                    rowData[headers[col]] = cellValue ?? string.Empty;
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
            return sanitizedName; // Remove Excel_ prefix to match existing table names
        }

        private string SanitizeTableName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Table_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // Türkçe karakterleri İngilizce karşılıklarına çevir
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            var sanitized = name;
            
            // Türkçe karakterleri değiştir
            foreach (var kvp in turkishToEnglish)
            {
                sanitized = sanitized.Replace(kvp.Key, kvp.Value);
            }

            // Boşlukları alt çizgi ile değiştir
            sanitized = sanitized.Replace(" ", "_");
            
            // Geçersiz karakterleri alt çizgi ile değiştir (sadece harf, rakam ve alt çizgi bırak)
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9_]", "_");
            
            // Birden fazla alt çizgiyi tek alt çizgiye çevir
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"_+", "_");
            
            // Başındaki ve sonundaki alt çizgileri kaldır
            sanitized = sanitized.Trim('_');
            
            // İlk karakter rakam ise başına alt çizgi ekle (SQL Server requirement)
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;
            
            // Boşsa varsayılan isim ver
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Table_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                
            return sanitized;
        }

        public string SanitizeColumnName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // Türkçe karakterleri İngilizce karşılıklarına çevir
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}, {'i', 'i'}
            };

            var sanitized = name;
            
            // Türkçe karakterleri değiştir
            foreach (var kvp in turkishToEnglish)
            {
                sanitized = sanitized.Replace(kvp.Key, kvp.Value);
            }

            // Boşlukları alt çizgi ile değiştir
            sanitized = sanitized.Replace(" ", "_");
            
            // Geçersiz karakterleri alt çizgi ile değiştir (sadece harf, rakam ve alt çizgi bırak)
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[^a-zA-Z0-9_]", "_");
            
            // Birden fazla alt çizgiyi tek alt çizgiye çevir
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"_+", "_");
            
            // Başındaki ve sonundaki alt çizgileri kaldır
            sanitized = sanitized.Trim('_');
            
            // İlk karakter rakam ise başına "Col_" ekle
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
                sanitized = "Col_" + sanitized;
            
            // 128 karakter sınırını kontrol et (SQL Server column name limit)
            if (sanitized.Length > 128)
            {
                sanitized = sanitized.Substring(0, 128);
                // Alt çizgi ile bitiyorsa kaldır
                sanitized = sanitized.TrimEnd('_');
            }
            
            // Boşsa varsayılan isim ver
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "Column_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                
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
                    return cell.StringCellValue ?? string.Empty;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString();
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    // For formulas, try to get the calculated value
                    try
                    {
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                                return cell.StringCellValue ?? string.Empty;
                            case CellType.Numeric:
                                if (DateUtil.IsCellDateFormatted(cell))
                                    return cell.DateCellValue.ToString();
                                return cell.NumericCellValue.ToString();
                            case CellType.Boolean:
                                return cell.BooleanCellValue.ToString();
                            default:
                                return cell.StringCellValue ?? string.Empty;
                        }
                    }
                    catch
                    {
                        return cell.StringCellValue ?? string.Empty;
                    }
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

        // New method for two-stage process: Stage 2 - Insert data into existing table
        public async Task<bool> InsertDataIntoTableAsync(int tableId, IFormFile file, int? databaseConnectionId = null)
        {
            try
            {
                // Get the dynamic table
                var dynamicTable = await _context.DynamicTables
                    .Include(t => t.Columns.OrderBy(c => c.ColumnOrder))
                    .FirstOrDefaultAsync(t => t.Id == tableId);

                if (dynamicTable == null)
                {
                    throw new ArgumentException($"Table with ID {tableId} not found");
                }

                // Read Excel data
                var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);

                // Validate that headers match the table structure
                var tableColumns = dynamicTable.Columns.OrderBy(c => c.ColumnOrder).ToList();
                if (headers.Count != tableColumns.Count)
                {
                    throw new InvalidOperationException($"Column count mismatch. Expected: {tableColumns.Count}, Found: {headers.Count}");
                }

                // Validate column names match
                for (int i = 0; i < headers.Count; i++)
                {
                    var expectedColumn = tableColumns[i];
                    var actualHeader = headers[i];
                    
                    if (SanitizeColumnName(actualHeader) != expectedColumn.ColumnName)
                    {
                        throw new InvalidOperationException($"Column mismatch at position {i + 1}. Expected: {expectedColumn.ColumnName}, Found: {SanitizeColumnName(actualHeader)}");
                    }
                }

                // Insert data
                var dataInserted = await InsertDataAsync(dynamicTable, sampleData, databaseConnectionId);
                if (!dataInserted)
                {
                    throw new InvalidOperationException("Data insertion failed");
                }

                // Mark as processed
                dynamicTable.IsProcessed = true;
                dynamicTable.ProcessedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Data inserted successfully into table: {TableName}", dynamicTable.TableName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into table ID: {TableId}", tableId);
                throw;
            }
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

            // Read headers (first row)
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
                    rowData[headers[col - 1]] = cellValue ?? string.Empty;
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

            // Read headers (first row)
            var headers = new List<string>();
            var headerRow = sheet.GetRow(0);
            if (headerRow != null)
            {
                for (int col = 0; col < headerRow.LastCellNum; col++)
                {
                    var cell = headerRow.GetCell(col);
                    var headerValue = GetNpoiCellValue(cell);
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        headers.Add(headerValue);
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
                    rowData[headers[col]] = cellValue ?? string.Empty;
                }
                allData.Add(rowData);
            }
            
            return Task.CompletedTask;
        }

        // Enhanced method to insert data into existing table by name with auto-column management
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

                // Read Excel data - use all data instead of just sample data
                var (headers, dataTypes, _) = await AnalyzeExcelFileAsync(file);
                var allData = await ReadAllExcelDataAsync(file);

                // Get existing table structure from database
                var existingColumns = await GetTableColumnsAsync(connection, existingTableName);
                if (!existingColumns.Any())
                {
                    throw new InvalidOperationException($"Table '{existingTableName}' not found or has no columns");
                }

                _logger.LogInformation("Found {ColumnCount} columns in existing table '{TableName}': {Columns}", 
                    existingColumns.Count, existingTableName, string.Join(", ", existingColumns));
                _logger.LogInformation("Excel file has {HeaderCount} headers: {Headers}", 
                    headers.Count, string.Join(", ", headers));
                _logger.LogInformation("Excel file has {DataRowCount} data rows", allData.Count);

                // Find matching columns and missing columns with improved matching logic
                var matchingColumns = new List<(string tableColumn, string excelHeader, int excelIndex)>();
                var missingColumns = new List<(string excelHeader, string dataType, int excelIndex)>();
                
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    var sanitizedHeader = SanitizeColumnName(header);
                    
                    // Try multiple matching strategies:
                    // 1. Exact match (case-insensitive)
                    var matchingColumn = existingColumns.FirstOrDefault(c => 
                        string.Equals(c, header, StringComparison.OrdinalIgnoreCase));
                    
                    // 2. Sanitized name match
                    if (matchingColumn == null)
                    {
                        matchingColumn = existingColumns.FirstOrDefault(c => 
                            string.Equals(c, sanitizedHeader, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    // 3. Partial match (contains)
                    if (matchingColumn == null)
                    {
                        matchingColumn = existingColumns.FirstOrDefault(c => 
                            c.Contains(header, StringComparison.OrdinalIgnoreCase) || 
                            header.Contains(c, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    if (matchingColumn != null)
                    {
                        matchingColumns.Add((matchingColumn, header, i));
                        _logger.LogInformation("Matched Excel header '{ExcelHeader}' to table column '{TableColumn}'", header, matchingColumn);
                    }
                    else
                    {
                        // Column doesn't exist in table - will be added
                        missingColumns.Add((header, dataTypes[i], i));
                        _logger.LogInformation("Excel header '{ExcelHeader}' not found in table, will be added as new column", header);
                    }
                }

                // Auto-add missing columns to the existing table
                if (missingColumns.Any())
                {
                    _logger.LogInformation("Adding {MissingColumnCount} missing columns to table '{TableName}': {MissingColumns}", 
                        missingColumns.Count, existingTableName, string.Join(", ", missingColumns.Select(mc => mc.excelHeader)));
                    
                    foreach (var missingColumn in missingColumns)
                    {
                        var baseColumnName = SanitizeColumnName(missingColumn.excelHeader);
                        var columnName = baseColumnName;
                        var counter = 1;
                        
                        // Check if column name already exists and generate unique name
                        while (existingColumns.Any(c => string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase)))
                        {
                            columnName = $"{baseColumnName}_{counter}";
                            counter++;
                        }
                        
                        var sqlDataType = GetSqlDataType(missingColumn.dataType, null);
                        
                        // Use TRY-CATCH to handle potential errors gracefully
                        try
                        {
                            var alterSql = $"ALTER TABLE [{existingTableName}] ADD [{columnName}] {sqlDataType}";
                            using var alterCommand = new SqlCommand(alterSql, connection);
                            await alterCommand.ExecuteNonQueryAsync();
                            
                            _logger.LogInformation("Successfully added column '{ColumnName}' with type '{DataType}' to table '{TableName}'", 
                                columnName, sqlDataType, existingTableName);
                            
                            // Add the new column to existing columns list
                            existingColumns.Add(columnName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to add column '{ColumnName}' to table '{TableName}': {Error}", 
                                columnName, existingTableName, ex.Message);
                            // Continue with other columns
                        }
                    }
                }

                // Now all Excel columns should have matching table columns
                if (!matchingColumns.Any() && !missingColumns.Any())
                {
                    throw new InvalidOperationException($"No matching columns found between Excel headers and table '{existingTableName}' columns");
                }

                // Build INSERT statement for ALL columns (matching + newly added)
                var allColumns = new List<string>();
                
                // Add matching columns
                allColumns.AddRange(matchingColumns.Select(mc => mc.tableColumn));
                
                // Add newly added columns with their actual names
                foreach (var missingColumn in missingColumns)
                {
                    var baseColumnName = SanitizeColumnName(missingColumn.excelHeader);
                    var columnName = baseColumnName;
                    var counter = 1;
                    
                    // Find the actual column name that was created
                    while (!existingColumns.Any(c => string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase)))
                    {
                        columnName = $"{baseColumnName}_{counter}";
                        counter++;
                    }
                    
                    allColumns.Add(columnName);
                }
                
                var columnNames = string.Join(", ", allColumns.Select(c => $"[{c}]"));
                var parameterNames = string.Join(", ", allColumns.Select(c => $"@{c}"));

                var insertSql = $"INSERT INTO [{existingTableName}] ({columnNames}) VALUES ({parameterNames})";
                _logger.LogInformation("Using INSERT SQL: {InsertSql}", insertSql);

                using var command = new SqlCommand(insertSql, connection);
                
                // Add parameters for all columns
                foreach (var column in allColumns)
                {
                    command.Parameters.AddWithValue($"@{column}", DBNull.Value);
                }

                // Insert data rows with batch processing for better performance
                var insertedRows = 0;
                var batchSize = 1000; // Process in batches of 1000 rows
                
                for (int batchStart = 0; batchStart < allData.Count; batchStart += batchSize)
                {
                    var batchEnd = Math.Min(batchStart + batchSize, allData.Count);
                    var batch = allData.Skip(batchStart).Take(batchSize - batchStart).ToList();
                    
                    foreach (var row in batch)
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
                        
                        // Set values for newly added columns
                        foreach (var missingColumn in missingColumns)
                        {
                            var headerName = missingColumn.excelHeader;
                            var baseColumnName = SanitizeColumnName(missingColumn.excelHeader);
                            var columnName = baseColumnName;
                            var counter = 1;
                            
                            // Find the actual column name that was created
                            while (!existingColumns.Any(c => string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase)))
                            {
                                columnName = $"{baseColumnName}_{counter}";
                                counter++;
                            }
                            
                            var excelIndex = missingColumn.excelIndex;
                            
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
                    
                    _logger.LogInformation("Processed batch {BatchNumber}, inserted {InsertedRows} rows so far", 
                        (batchStart / batchSize) + 1, insertedRows);
                }

                _logger.LogInformation("Data insertion completed successfully into existing table: {TableName}, {InsertedRows} rows, {MatchingColumns} matching columns, {MissingColumns} added columns", 
                    existingTableName, insertedRows, matchingColumns.Count, missingColumns.Count);
                return insertedRows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into existing table: {TableName}. Error: {ErrorMessage}", existingTableName, ex.Message);
                throw;
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

        // Check if table with same name already exists and return existing table info
        public async Task<(bool exists, DynamicTable? existingTable, string? actualTableName)> CheckTableExistsForInsertAsync(string tableName, int? databaseConnectionId = null)
        {
            try
            {
                _logger.LogInformation("Checking if table exists for insert: {TableName}", tableName);
                
                // First check if exact table exists in database
                var exactTableExists = await CheckExactTableExistsAsync(tableName, databaseConnectionId);
                if (exactTableExists)
                {
                    _logger.LogInformation("Exact table exists in database: {TableName}", tableName);
                    var actualTableName = await FindExistingTableNameAsync(tableName, databaseConnectionId);
                    return (true, null, actualTableName);
                }
                
                // Check if table exists in tracking system
                var existingTableId = await GetTableIdByNameAsync(tableName);
                if (existingTableId.HasValue)
                {
                    _logger.LogInformation("Table exists in tracking system with ID: {TableId}", existingTableId.Value);
                    var existingTable = await GetTableByIdAsync(existingTableId.Value);
                    return (true, existingTable, existingTable?.TableName);
                }
                
                // Check for similar table names
                var baseTableName = GetBaseTableName(tableName);
                var connectionString = databaseConnectionId.HasValue 
                    ? await GetConnectionStringAsync(databaseConnectionId.Value)
                    : _configuration.GetConnectionString("DefaultConnection");
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                var similarTableName = await FindExistingTableWithSimilarNameAsync(connection, baseTableName);
                
                if (!string.IsNullOrEmpty(similarTableName))
                {
                    _logger.LogInformation("Found similar table: {SimilarTableName} for base name: {BaseTableName}", similarTableName, baseTableName);
                    return (true, null, similarTableName);
                }
                
                _logger.LogInformation("No existing table found for: {TableName}", tableName);
                return (false, null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if table exists for insert: {TableName}", tableName);
                throw;
            }
        }

        // Insert data into existing table with same name
        public async Task<(bool success, int insertedRows, string? errorMessage)> InsertDataIntoExistingTableWithSameNameAsync(IFormFile file, string tableName, int? databaseConnectionId = null)
        {
            try
            {
                _logger.LogInformation("Inserting data into existing table with same name: {TableName}", tableName);
                
                // Check if table exists
                var (exists, existingTable, actualTableName) = await CheckTableExistsForInsertAsync(tableName, databaseConnectionId);
                
                if (!exists)
                {
                    _logger.LogWarning("Table does not exist: {TableName}", tableName);
                    return (false, 0, $"Tablo '{tableName}' bulunamadı");
                }
                
                // Use actual table name if found
                var targetTableName = actualTableName ?? tableName;
                _logger.LogInformation("Using target table name: {TargetTableName}", targetTableName);
                
                // Insert data into the existing table
                var insertedRows = await InsertDataIntoExistingTableAsync(file, targetTableName, databaseConnectionId);
                
                // Update tracking record if exists
                if (existingTable != null)
                {
                    existingTable.RowCount = insertedRows;
                    existingTable.IsProcessed = true;
                    existingTable.ProcessedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated existing tracking record for table: {TableName}", existingTable.TableName);
                }
                else
                {
                    // Create new tracking record for the existing table
                    var userName = "System"; // You might want to pass this as parameter
                    var (headers, dataTypes, sampleData) = await AnalyzeExcelFileAsync(file);
                    
                    var newTrackingTable = new DynamicTable
                    {
                        TableName = targetTableName,
                        FileName = file.FileName,
                        UploadedBy = userName,
                        Description = $"Veri eklendi - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
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
                        newTrackingTable.Columns.Add(column);
                    }
                    
                    _context.DynamicTables.Add(newTrackingTable);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new tracking record for existing table: {TableName}", targetTableName);
                }
                
                _logger.LogInformation("Successfully inserted {InsertedRows} rows into existing table: {TableName}", insertedRows, targetTableName);
                return (true, insertedRows, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into existing table: {TableName}", tableName);
                return (false, 0, $"Veri eklenirken hata oluştu: {ex.Message}");
            }
        }

        private async Task<string> GetConnectionStringAsync(int databaseConnectionId)
        {
            var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId);
            if (dbConnection == null)
                throw new ArgumentException($"Database connection with ID {databaseConnectionId} not found");
            
            return $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
        }


    }
}
