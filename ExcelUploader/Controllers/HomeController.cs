using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Models;
using ExcelUploader.Services;
using ExcelUploader.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IDataImportService _dataImportService;
        private readonly IExcelService _excelService;
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(
            IDataImportService dataImportService, 
            IExcelService excelService, 
            IDynamicTableService dynamicTableService,
            ApplicationDbContext context,
            ILogger<HomeController> logger,
            IConfiguration configuration)
        {
            _dataImportService = dataImportService;
            _excelService = excelService;
            _dynamicTableService = dynamicTableService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return Redirect("/index.html");
        }

        [HttpGet]
        [Route("stats")]
        [Authorize]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var stats = new
                {
                    totalFiles = tables.Count,
                    totalRecords = tables.Sum(t => t.RowCount),
                    totalTables = tables.Count
                };
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stats");
                return StatusCode(500, new { error = "İstatistikler yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        [Route("recent-activities")]
        [Authorize]
        public async Task<IActionResult> RecentActivities(int page = 1, int pageSize = 10)
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var totalCount = tables.Count;
                
                var activities = tables
                    .OrderByDescending(t => t.UploadDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        type = "upload",
                        title = $"{t.FileName} dosyası yüklendi",
                        timestamp = t.UploadDate,
                        tableName = t.TableName,
                        rowCount = t.RowCount,
                        columnCount = t.ColumnCount,
                        isProcessed = t.IsProcessed
                    })
                    .ToList();

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    activities = activities,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent activities");
                return StatusCode(500, new { error = "Son aktiviteler yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        [Route("test-database")]
        [Authorize]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Test database connection
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get database info
                var databaseInfo = new
                {
                    Database = connection.Database,
                    Server = connection.DataSource,
                    State = connection.State.ToString()
                };

                return Ok(new
                {
                    isConnected = true,
                    databaseInfo = databaseInfo,
                    message = "Veritabanı bağlantısı başarılı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return Ok(new
                {
                    isConnected = false,
                    databaseInfo = new
                    {
                        Database = "Unknown",
                        Server = "Unknown",
                        State = "Disconnected"
                    },
                    message = "Veritabanı bağlantısı başarısız"
                });
            }
        }

        [HttpPost]
        [Route("preview")]
        [Authorize]
        public async Task<IActionResult> Preview([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { error = "Lütfen bir dosya seçin" });
            }

            try
            {
                // Validate file
                if (!await _excelService.ValidateExcelFileAsync(file))
                {
                    return BadRequest(new { error = "Geçersiz dosya formatı veya boyut" });
                }

                // Read Excel file and return preview data
                var previewData = await _excelService.GetExcelPreviewAsync(file);
                return Ok(previewData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing Excel file");
                return StatusCode(500, new { error = "Excel dosyası önizlenirken hata oluştu" });
            }
        }

        [HttpGet]
        [Route("dashboard")]
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var dashboardData = new DashboardViewModel
                {
                    TotalRecords = tables.Sum(t => t.RowCount),
                    ProcessedRecords = tables.Where(t => t.IsProcessed).Sum(t => t.RowCount),
                    PendingRecords = tables.Where(t => !t.IsProcessed).Sum(t => t.RowCount),
                    TotalGrantAmount = 0,
                    TotalPaidAmount = 0,
                    RecentUploads = new List<ExcelData>(),
                    MonthlyData = new List<ChartData>(),
                    DynamicTables = tables.Take(5).ToList()
                };
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet]
        [Route("upload")]
        [Authorize]
        public IActionResult Upload()
        {
            return Ok(new { message = "Upload endpoint ready" });
        }

        [HttpPost]
        [Route("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] UploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (model.ExcelFile == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                // Validate file
                if (!await _excelService.ValidateExcelFileAsync(model.ExcelFile))
                {
                    return BadRequest(new { error = "Geçersiz dosya formatı veya boyut" });
                }

                // Process Excel file and create dynamic table
                var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown";
                
                // Generate table name from file name
                var tableName = GenerateTableNameFromFileName(model.ExcelFile.FileName);
                
                // Handle DatabaseConnectionId - if it's 0, treat as null
                int? databaseConnectionId = model.DatabaseConnectionId;
                if (databaseConnectionId == 0)
                {
                    databaseConnectionId = null;
                }
                
                // Check if table exists in database (exact name or similar name)
                var exactTableExists = await _dynamicTableService.CheckExactTableExistsAsync(tableName, databaseConnectionId);
                var tableExistsInTracking = await _dynamicTableService.TableExistsAsync(tableName, databaseConnectionId);
                var existingTableId = await _dynamicTableService.GetTableIdByNameAsync(tableName);
                
                DynamicTable dynamicTable;
                string action;
                int insertedRows = 0;
                string actualTableName = tableName;
                
                if (exactTableExists)
                {
                    // Table already exists in database, find actual table name and insert data
                    actualTableName = await _dynamicTableService.FindExistingTableNameAsync(tableName, databaseConnectionId);
                    if (actualTableName == null)
                    {
                        return BadRequest(new { error = "Mevcut tablo bulunamadı" });
                    }
                    
                    // Insert data into existing table with auto-column management
                    insertedRows = await _dynamicTableService.InsertDataIntoExistingTableAsync(model.ExcelFile, actualTableName, databaseConnectionId);
                    
                    // Update or create tracking record
                    if (existingTableId.HasValue)
                    {
                        // Update existing tracking record
                        dynamicTable = await _dynamicTableService.GetTableByIdAsync(existingTableId.Value);
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
                        var (headers, dataTypes, sampleData) = await _dynamicTableService.AnalyzeExcelFileAsync(model.ExcelFile);
                        dynamicTable = new DynamicTable
                        {
                            TableName = actualTableName,
                            FileName = model.ExcelFile.FileName,
                            UploadedBy = userName,
                            Description = model.Description ?? string.Empty,
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
                                ColumnName = _dynamicTableService.SanitizeColumnName(headers[i]),
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
                    
                    action = "data_inserted_into_existing";
                }
                else if (tableExistsInTracking && existingTableId.HasValue)
                {
                    // Table exists in tracking system but not in database, create table and insert data
                    dynamicTable = await _dynamicTableService.GetTableByIdAsync(existingTableId.Value);
                    if (dynamicTable == null)
                    {
                        return BadRequest(new { error = "Mevcut tablo bulunamadı" });
                    }
                    
                    // Create SQL table and insert data
                    var success = await _dynamicTableService.InsertDataIntoTableAsync(existingTableId.Value, model.ExcelFile, databaseConnectionId);
                    if (!success)
                    {
                        return BadRequest(new { error = "Veriler mevcut tabloya eklenemedi" });
                    }
                    
                    action = "data_inserted_into_existing";
                    insertedRows = dynamicTable.RowCount;
                    actualTableName = dynamicTable.TableName;
                }
                else
                {
                    // Create new table and insert data
                    dynamicTable = await _dynamicTableService.CreateTableFromExcelAsync(
                        model.ExcelFile, 
                        userName, 
                        databaseConnectionId, 
                        model.Description ?? string.Empty);
                    action = "new_table_created";
                    insertedRows = dynamicTable?.RowCount ?? 0;
                    actualTableName = dynamicTable?.TableName ?? tableName;
                }

                if (dynamicTable == null)
                {
                    return BadRequest(new { error = "Excel dosyasından tablo oluşturulamadı" });
                }

                return Ok(new { 
                    message = action == "data_inserted_into_existing" 
                        ? $"Veriler mevcut tabloya başarıyla eklendi: {actualTableName}" 
                        : $"Yeni tablo oluşturuldu ve veriler eklendi: {actualTableName}", 
                    tableName = actualTableName,
                    rowCount = insertedRows,
                    tableId = dynamicTable.Id,
                    action = action,
                    tableExists = action == "data_inserted_into_existing",
                    columnsAdded = action == "data_inserted_into_existing" ? "Eksik sütunlar otomatik eklendi" : "Yeni tablo oluşturuldu"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading Excel file");
                return StatusCode(500, new { error = "Dosya yükleme sırasında hata oluştu", details = ex.Message });
            }
        }

        // New endpoint for two-stage process: Stage 1 - Create table structure only
        [HttpPost]
        [Route("create-table-structure")]
        [Authorize]
        public async Task<IActionResult> CreateTableStructure([FromForm] UploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (model.ExcelFile == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                // Validate file
                if (!await _excelService.ValidateExcelFileAsync(model.ExcelFile))
                {
                    return BadRequest(new { error = "Geçersiz dosya formatı veya boyut" });
                }

                // Process Excel file and create table structure only
                var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown";
                
                // Generate table name from file name
                var tableName = GenerateTableNameFromFileName(model.ExcelFile.FileName);
                
                // Handle DatabaseConnectionId - if it's 0, treat as null
                int? databaseConnectionId = model.DatabaseConnectionId;
                if (databaseConnectionId == 0)
                {
                    databaseConnectionId = null;
                }
                
                // Check if exact table already exists in database
                var exactTableExists = await _dynamicTableService.CheckExactTableExistsAsync(tableName, databaseConnectionId);
                var existingTableId = await _dynamicTableService.GetTableIdByNameAsync(tableName);
                
                DynamicTable dynamicTable;
                string action;
                
                if (exactTableExists)
                {
                    // Table exists in database, find the actual table name
                    var actualTableName = await _dynamicTableService.FindExistingTableNameAsync(tableName, databaseConnectionId);
                    if (actualTableName == null)
                    {
                        return BadRequest(new { error = "Mevcut tablo bulunamadı" });
                    }
                    
                    // Insert data directly into the existing table
                    var insertedRows = await _dynamicTableService.InsertDataIntoExistingTableAsync(model.ExcelFile, actualTableName, databaseConnectionId);
                    
                    // Check if we have a tracking record for this table
                    if (existingTableId.HasValue)
                    {
                        // Use existing tracking record and update row count
                        dynamicTable = await _dynamicTableService.GetTableByIdAsync(existingTableId.Value);
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
                        // Check if a tracking record already exists for this table name
                        var existingTrackingTable = await _context.DynamicTables
                            .FirstOrDefaultAsync(t => t.TableName == actualTableName);
                        
                        if (existingTrackingTable != null)
                        {
                            // Use existing tracking record and update row count
                            dynamicTable = existingTrackingTable;
                            dynamicTable.RowCount = insertedRows;
                            dynamicTable.IsProcessed = true;
                            dynamicTable.ProcessedDate = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            // Create a new tracking record for the existing table WITHOUT calling CreateTableStructureAsync
                            var (headers, dataTypes, sampleData) = await _dynamicTableService.AnalyzeExcelFileAsync(model.ExcelFile);
                            dynamicTable = new DynamicTable
                            {
                                TableName = actualTableName,
                                FileName = model.ExcelFile.FileName,
                                UploadedBy = userName,
                                Description = model.Description ?? string.Empty,
                                RowCount = insertedRows,
                                ColumnCount = headers.Count,
                                UploadDate = DateTime.UtcNow,
                                IsProcessed = true,
                                ProcessedDate = DateTime.UtcNow
                            };
                            
                            // Create table columns for tracking only (not for database)
                            for (int i = 0; i < headers.Count; i++)
                            {
                                var column = new TableColumn
                                {
                                    ColumnName = _dynamicTableService.SanitizeColumnName(headers[i]),
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
                    }
                    action = "data_inserted_into_existing";
                }
                else
                {
                    // Create new table structure
                    dynamicTable = await _dynamicTableService.CreateTableStructureAsync(
                        model.ExcelFile, 
                        userName, 
                        databaseConnectionId, 
                        model.Description ?? string.Empty);
                    action = "structure_created";
                }

                if (dynamicTable == null)
                {
                    return BadRequest(new { error = "Excel dosyasından tablo yapısı oluşturulamadı" });
                }

                return Ok(new { 
                    message = exactTableExists ? $"Veriler mevcut tabloya başarıyla eklendi: {tableName}" : $"Tablo yapısı başarıyla oluşturuldu: {tableName}", 
                    tableName = dynamicTable.TableName,
                    tableId = dynamicTable.Id,
                    columnCount = dynamicTable.ColumnCount,
                    rowCount = exactTableExists ? dynamicTable.RowCount : 0,
                    stage = action,
                    tableExists = exactTableExists,
                    dataInserted = exactTableExists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table structure: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { 
                    error = "Tablo yapısı oluşturulurken hata oluştu", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Test database connection endpoint
        [HttpGet]
        [Route("test-connection")]
        [Authorize]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new { error = "Default connection string not found" });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get server and database info
                var server = connection.DataSource;
                var database = connection.Database;

                return Ok(new { 
                    server = server, 
                    database = database, 
                    status = "Connected successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return StatusCode(500, new { error = $"Database connection failed: {ex.Message}" });
            }
        }

        // Get table columns endpoint
        [HttpGet]
        [Route("get-table-columns")]
        [Authorize]
        public async Task<IActionResult> GetTableColumns([FromQuery] string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return BadRequest(new { error = "Table name is required" });
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new { error = "Default connection string not found" });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        COLUMN_NAME as name,
                        DATA_TYPE as type,
                        IS_NULLABLE as isNullable,
                        CHARACTER_MAXIMUM_LENGTH as maxLength
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName 
                    ORDER BY ORDINAL_POSITION";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);

                var columns = new List<object>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(new
                    {
                        name = reader.GetString(0), // COLUMN_NAME
                        type = reader.GetString(1), // DATA_TYPE
                        isNullable = reader.GetString(2), // IS_NULLABLE
                        maxLength = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3) // CHARACTER_MAXIMUM_LENGTH
                    });
                }

                return Ok(new { columns = columns });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table columns for table: {TableName}", tableName);
                return StatusCode(500, new { error = $"Error getting table columns: {ex.Message}" });
            }
        }

        // Helper method to generate table name from file name
        private string GenerateTableNameFromFileName(string fileName)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var tableName = nameWithoutExtension.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace("ç", "c")
                .Replace("ğ", "g")
                .Replace("ı", "i")
                .Replace("ö", "o")
                .Replace("ş", "s")
                .Replace("ü", "u")
                .Replace("Ç", "C")
                .Replace("Ğ", "G")
                .Replace("İ", "I")
                .Replace("Ö", "O")
                .Replace("Ş", "S")
                .Replace("Ü", "U");
            
            // Remove timestamp patterns from the table name
            tableName = Regex.Replace(tableName, @"_\d{8}_\d{6}$", "");
            tableName = Regex.Replace(tableName, @"_\d{8}_\d{4}$", "");
            tableName = Regex.Replace(tableName, @"_\d{8}$", "");
            
            return tableName;
        }



        // New endpoint for two-stage process: Stage 2 - Insert data into existing table
        [HttpPost]
        [Route("insert-data")]
        [Authorize]
        public async Task<IActionResult> InsertData([FromForm] InsertDataViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() })
                    .ToList();
                
                return BadRequest(new { 
                    error = "Validation failed", 
                    errors = errors,
                    modelState = ModelState
                });
            }

            try
            {
                if (model.ExcelFile == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                if (model.TableId <= 0)
                {
                    return BadRequest(new { error = "Geçersiz tablo ID" });
                }

                // Check if table exists
                var table = await _dynamicTableService.GetTableByIdAsync(model.TableId);
                if (table == null)
                {
                    return BadRequest(new { error = $"Tablo ID {model.TableId} bulunamadı. Lütfen önce tablo yapısını oluşturun." });
                }

                // Validate file
                if (!await _excelService.ValidateExcelFileAsync(model.ExcelFile))
                {
                    return BadRequest(new { error = "Geçersiz dosya formatı veya boyut" });
                }

                // Handle DatabaseConnectionId - if it's 0, treat as null
                int? databaseConnectionId = model.DatabaseConnectionId;
                if (databaseConnectionId == 0)
                {
                    databaseConnectionId = null;
                }
                
                // Insert data into existing table
                var success = await _dynamicTableService.InsertDataIntoTableAsync(model.TableId, model.ExcelFile, databaseConnectionId);

                if (!success)
                {
                    return BadRequest(new { error = "Veriler tabloya eklenemedi" });
                }

                return Ok(new { 
                    message = $"Veriler başarıyla tabloya eklendi", 
                    tableId = model.TableId,
                    tableName = table.TableName,
                    stage = "data_inserted"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error inserting data into table");
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error inserting data into table");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into table");
                return StatusCode(500, new { error = "Veriler eklenirken hata oluştu", details = ex.Message });
            }
        }

        [HttpPost]
        [Route("data")]
        [Authorize]
        public async Task<IActionResult> Data([FromBody] DataRequest request)
        {
            try
            {
                if (request.TableId.HasValue)
                {
                    var table = await _dynamicTableService.GetTableByIdAsync(request.TableId.Value);
                    if (table == null)
                    {
                        return NotFound(new { error = "Tablo bulunamadı" });
                    }
                    return Ok(table);
                }

                var tables = await _dynamicTableService.GetAllTablesAsync();
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
                return StatusCode(500, new { error = "Veri yüklenirken hata oluştu" });
            }
        }

        [HttpGet]
        [Route("view-table")]
        [Authorize]
        public async Task<IActionResult> ViewTable(string tableName, int? databaseConnectionId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return BadRequest(new { error = "Tablo adı gereklidir" });
                }

                // Get table data using the specified database connection or default
                var tableData = await _dynamicTableService.GetTableDataAsync(tableName, 1, 10, databaseConnectionId);

                return Ok(new { 
                    tableName = tableName,
                    rowCount = tableData.Count,
                    data = tableData
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error viewing table: {TableName}", tableName);
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error viewing table: {TableName}", tableName);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing table: {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo görüntülenirken hata oluştu", details = ex.Message });
            }
        }



        [HttpGet]
        [Route("data")]
        [Authorize]
        public async Task<IActionResult> DataGet(int? tableId = null)
        {
            try
            {
                if (tableId.HasValue)
                {
                    // Get specific table data
                    var table = await _dynamicTableService.GetTableByIdAsync(tableId.Value);
                    if (table == null)
                    {
                        return NotFound(new { error = "Tablo bulunamadı" });
                    }

                    // Get actual data from the SQL table
                    var tableData = await _dynamicTableService.GetTableDataAsync(table.TableName, 1, 1000, null);
                    
                    // Return just the data array for the frontend
                    return Ok(tableData);
                }

                // Get all tables and combine their data
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var allData = new List<object>();

                foreach (var table in tables)
                {
                    try
                    {
                        // Get actual data from each SQL table
                        var tableData = await _dynamicTableService.GetTableDataAsync(table.TableName, 1, 100, null);
                        
                        // Add table info to each row
                        foreach (var row in tableData)
                        {
                            var rowDict = row as Dictionary<string, object> ?? new Dictionary<string, object>();
                            rowDict["_TableName"] = table.TableName;
                            rowDict["_FileName"] = table.FileName;
                            rowDict["_UploadDate"] = table.UploadDate;
                            rowDict["_TableId"] = table.Id;
                            allData.Add(rowDict);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error loading data for table: {TableName}", table.TableName);
                        // Continue with other tables even if one fails
                    }
                }

                return Ok(allData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
                return StatusCode(500, new { error = "Veri yüklenirken hata oluştu", details = ex.Message });
            }
        }

        [HttpGet]
        [Route("available-tables")]
        [Authorize]
        public async Task<IActionResult> GetAvailableTables()
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var tableList = tables.Select(t => new
                {
                    id = t.Id,
                    tableName = t.TableName,
                    fileName = t.FileName,
                    uploadDate = t.UploadDate,
                    rowCount = t.RowCount,
                    columnCount = t.ColumnCount,
                    isProcessed = t.IsProcessed
                }).ToList();

                return Ok(new
                {
                    totalTables = tableList.Count,
                    tables = tableList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available tables");
                return StatusCode(500, new { error = "Tablolar listelenirken hata oluştu" });
            }
        }





        [HttpGet]
        [Route("check-table-exists")]
        [Authorize]
        public async Task<IActionResult> CheckTableExists(string tableName, int? databaseConnectionId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return BadRequest(new { error = "Tablo adı gereklidir" });
                }

                var exists = await _dynamicTableService.CheckExactTableExistsAsync(tableName, databaseConnectionId);
                
                return Ok(new { 
                    exists = exists,
                    tableName = tableName,
                    message = exists ? $"'{tableName}' tablosu zaten mevcut" : $"'{tableName}' tablosu mevcut değil"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if table exists: {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo kontrolü sırasında hata oluştu" });
            }
        }

        [HttpPost]
        [Route("compare-table-structure")]
        [Authorize]
        public async Task<IActionResult> CompareTableStructure([FromForm] IFormFile file, string tableName, int? databaseConnectionId = null)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                if (string.IsNullOrEmpty(tableName))
                {
                    return BadRequest(new { error = "Tablo adı gereklidir" });
                }

                // Validate file
                if (!await _excelService.ValidateExcelFileAsync(file))
                {
                    return BadRequest(new { error = "Geçersiz dosya formatı veya boyut" });
                }

                // Read Excel structure
                var (excelHeaders, excelDataTypes, _) = await _dynamicTableService.AnalyzeExcelFileAsync(file);
                
                // Get table structure from database
                var connectionString = databaseConnectionId.HasValue 
                    ? await GetConnectionStringAsync(databaseConnectionId.Value)
                    : _configuration.GetConnectionString("DefaultConnection");
                
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        COLUMN_NAME as name,
                        DATA_TYPE as type,
                        IS_NULLABLE as isNullable,
                        CHARACTER_MAXIMUM_LENGTH as maxLength
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName 
                    ORDER BY ORDINAL_POSITION";

                using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TableName", tableName);

                var tableColumns = new List<object>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tableColumns.Add(new
                    {
                        name = reader.GetString(0),
                        type = reader.GetString(1),
                        isNullable = reader.GetString(2),
                        maxLength = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
                    });
                }

                // Compare structures
                var matchingColumns = new List<object>();
                var missingColumns = new List<object>();
                var extraColumns = new List<object>();

                // Check Excel headers against table columns
                foreach (var header in excelHeaders)
                {
                    var sanitizedHeader = _dynamicTableService.SanitizeColumnName(header);
                    var found = tableColumns.Any(tc => 
                        string.Equals(tc.GetType().GetProperty("name")?.GetValue(tc)?.ToString(), header, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(tc.GetType().GetProperty("name")?.GetValue(tc)?.ToString(), sanitizedHeader, StringComparison.OrdinalIgnoreCase));
                    
                    if (found)
                    {
                        matchingColumns.Add(new { excelHeader = header, status = "matched" });
                    }
                    else
                    {
                        missingColumns.Add(new { excelHeader = header, status = "missing", suggestedName = sanitizedHeader });
                    }
                }

                // Check table columns against Excel headers
                foreach (var tableColumn in tableColumns)
                {
                    var columnName = tableColumn.GetType().GetProperty("name")?.GetValue(tableColumn)?.ToString();
                    var found = excelHeaders.Any(h => 
                        string.Equals(h, columnName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(_dynamicTableService.SanitizeColumnName(h), columnName, StringComparison.OrdinalIgnoreCase));
                    
                    if (!found)
                    {
                        extraColumns.Add(new { tableColumn = columnName, status = "extra" });
                    }
                }

                return Ok(new
                {
                    tableName = tableName,
                    excelHeaders = excelHeaders,
                    excelDataTypes = excelDataTypes,
                    tableColumns = tableColumns,
                    comparison = new
                    {
                        matchingColumns = matchingColumns.Count,
                        missingColumns = missingColumns.Count,
                        extraColumns = extraColumns.Count,
                        total = excelHeaders.Count
                    },
                    details = new
                    {
                        matching = matchingColumns,
                        missing = missingColumns,
                        extra = extraColumns
                    },
                    action = missingColumns.Count > 0 ? "update_required" : "no_update_needed",
                    message = missingColumns.Count > 0 
                        ? $"{missingColumns.Count} sütun eksik, otomatik güncelleme yapılacak" 
                        : "Tablo yapısı uyumlu, güncelleme gerekmiyor"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing table structure: {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo yapısı karşılaştırılırken hata oluştu", details = ex.Message });
            }
        }

        private async Task<string> GetConnectionStringAsync(int databaseConnectionId)
        {
            var dbConnection = await _context.DatabaseConnections.FindAsync(databaseConnectionId);
            if (dbConnection == null)
                throw new ArgumentException($"Database connection with ID {databaseConnectionId} not found");
            
            return $"Server={dbConnection.ServerName},{dbConnection.Port};Database={dbConnection.DatabaseName};User Id={dbConnection.Username};Password={dbConnection.Password};TrustServerCertificate=true;";
        }

        [HttpGet]
        [Route("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "Healthy", 
                timestamp = DateTime.UtcNow,
                version = "9.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            });
        }



        // Delete table endpoint
        [HttpDelete]
        [Route("delete-table/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTable(int id)
        {
            try
            {
                var success = await _dynamicTableService.DeleteTableAsync(id);
                if (success)
                {
                    return Ok(new { message = "Tablo başarıyla silindi" });
                }
                else
                {
                    return BadRequest(new { error = "Tablo silinirken hata oluştu" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {Id}", id);
                return StatusCode(500, new { error = "Tablo silinirken hata oluştu", details = ex.Message });
            }
        }


    }
}
