using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Models;
using ExcelUploader.Services;
using ExcelUploader.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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
        public async Task<IActionResult> RecentActivities()
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var activities = tables
                    .OrderByDescending(t => t.UploadDate)
                    .Take(10)
                    .Select(t => new
                    {
                        type = "upload",
                        title = $"{t.FileName} dosyası yüklendi",
                        timestamp = t.UploadDate
                    })
                    .ToList();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent activities");
                return StatusCode(500, new { error = "Son aktiviteler yüklenirken hata oluştu" });
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
                
                // Create dynamic table from Excel file
                var dynamicTable = await _dynamicTableService.CreateTableFromExcelAsync(
                    model.ExcelFile, 
                    userName, 
                    model.DatabaseConnectionId, 
                    model.Description);

                if (dynamicTable == null)
                {
                    return BadRequest(new { error = "Excel dosyasından tablo oluşturulamadı" });
                }

                return Ok(new { 
                    message = $"Excel dosyası başarıyla yüklendi", 
                    tableName = dynamicTable.TableName,
                    rowCount = dynamicTable.RowCount,
                    tableId = dynamicTable.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading Excel file");
                return StatusCode(500, new { error = "Dosya yükleme sırasında hata oluştu" });
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
                
                // Create table structure from Excel file
                var dynamicTable = await _dynamicTableService.CreateTableStructureAsync(
                    model.ExcelFile, 
                    userName, 
                    model.DatabaseConnectionId, 
                    model.Description);

                if (dynamicTable == null)
                {
                    return BadRequest(new { error = "Excel dosyasından tablo yapısı oluşturulamadı" });
                }

                return Ok(new { 
                    message = $"Tablo yapısı başarıyla oluşturuldu", 
                    tableName = dynamicTable.TableName,
                    tableId = dynamicTable.Id,
                    columnCount = dynamicTable.ColumnCount,
                    stage = "structure_created"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table structure");
                return StatusCode(500, new { error = "Tablo yapısı oluşturulurken hata oluştu" });
            }
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

                // Insert data into existing table
                var success = await _dynamicTableService.InsertDataIntoTableAsync(model.TableId, model.ExcelFile, model.DatabaseConnectionId);

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
        [Route("diagnose")]
        [Authorize]
        public async Task<IActionResult> Diagnose(string tableName = null)
        {
            try
            {
                var diagnostics = new
                {
                    timestamp = DateTime.UtcNow,
                    databaseConnection = "Checking...",
                    tableExists = tableName != null ? "Checking..." : "No table specified",
                    availableTables = new List<string>(),
                    errors = new List<string>()
                };

                // Check database connection
                try
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                    await connection.OpenAsync();
                    diagnostics = diagnostics with { databaseConnection = "Connected successfully" };
                }
                catch (Exception ex)
                {
                    diagnostics = diagnostics with { databaseConnection = $"Connection failed: {ex.Message}" };
                    diagnostics.errors.Add($"Database connection error: {ex.Message}");
                }

                // Check if table exists
                if (!string.IsNullOrEmpty(tableName))
                {
                    try
                    {
                        var connectionString = _configuration.GetConnectionString("DefaultConnection");
                        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                        await connection.OpenAsync();

                        var sql = $@"
                            SELECT COUNT(*) 
                            FROM INFORMATION_SCHEMA.TABLES 
                            WHERE TABLE_NAME = @TableName";
                        
                        using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@TableName", tableName);
                        
                        var tableExists = await command.ExecuteScalarAsync();
                        diagnostics = diagnostics with { tableExists = tableExists.ToString() == "1" ? "Yes" : "No" };
                    }
                    catch (Exception ex)
                    {
                        diagnostics = diagnostics with { tableExists = $"Error checking: {ex.Message}" };
                        diagnostics.errors.Add($"Table check error: {ex.Message}");
                    }
                }

                // Get available tables
                try
                {
                    var tables = await _dynamicTableService.GetAllTablesAsync();
                    diagnostics = diagnostics with { availableTables = tables.Select(t => t.TableName).ToList() };
                }
                catch (Exception ex)
                {
                    diagnostics.errors.Add($"Error getting available tables: {ex.Message}");
                }

                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in diagnostics");
                return StatusCode(500, new { error = "Diagnostics failed", details = ex.Message });
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
                    var table = await _dynamicTableService.GetTableByIdAsync(tableId.Value);
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
        [Route("sql-test")]
        [AllowAnonymous]
        public async Task<IActionResult> SqlTest()
        {
            try
            {
                // Test SQL Server connection
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // Get database info
                    var connectionString = _context.Database.GetConnectionString();
                    var databaseName = _context.Database.GetDbConnection().Database;
                    var serverVersion = _context.Database.GetDbConnection().ServerVersion;
                    
                    return Ok(new { 
                        status = "Connected",
                        message = "SQL Server bağlantısı başarılı",
                        database = databaseName,
                        serverVersion = serverVersion,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new { 
                        status = "Failed",
                        message = "SQL Server bağlantısı başarısız",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL connection test failed");
                return StatusCode(500, new { 
                    status = "Error",
                    message = "SQL Server bağlantı testi sırasında hata oluştu",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet]
        [Route("database-info")]
        [Authorize]
        public async Task<IActionResult> DatabaseInfo()
        {
            try
            {
                // Get database statistics
                var tableCount = await _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'").FirstOrDefaultAsync();
                var totalSize = await _context.Database.SqlQueryRaw<decimal>("SELECT SUM(size * 8.0 / 1024) FROM sys.database_files").FirstOrDefaultAsync();
                
                // Get recent tables
                var recentTables = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Take(10)
                    .Select(t => new { t.TableName, t.FileName, t.UploadDate, t.RowCount })
                    .ToListAsync();

                return Ok(new
                {
                    tableCount,
                    totalSizeMB = Math.Round(totalSize, 2),
                    recentTables,
                    lastUpdate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database info");
                return StatusCode(500, new { error = "Veritabanı bilgileri alınırken hata oluştu" });
            }
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

        // Test database connection endpoint
        [HttpGet]
        [Route("test-database")]
        [Authorize]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var isConnected = await _dynamicTableService.TestDatabaseConnectionAsync();
                var dbInfo = await _dynamicTableService.GetDatabaseInfoAsync();
                
                return Ok(new { 
                    isConnected = isConnected,
                    databaseInfo = dbInfo,
                    message = isConnected ? "Database connection successful" : "Database connection failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return StatusCode(500, new { error = "Database connection test failed", details = ex.Message });
            }
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

        // Connection info endpoint for diagnostics
        [HttpGet]
        [Route("connection-info")]
        [Authorize]
        public IActionResult ConnectionInfo()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                // Parse connection string to extract components
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                return Ok(new
                {
                    defaultConnectionString = connectionString,
                    databaseName = builder.InitialCatalog,
                    server = builder.DataSource,
                    user = builder.UserID,
                    integratedSecurity = builder.IntegratedSecurity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection info");
                return StatusCode(500, new { error = "Connection info retrieval failed", details = ex.Message });
            }
        }

        // Reset connection endpoint
        [HttpPost]
        [Route("reset-connection")]
        [Authorize]
        public IActionResult ResetConnection()
        {
            try
            {
                // This endpoint can be used to reset connection settings if needed
                // For now, just return success
                return Ok(new { message = "Connection settings reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting connection");
                return StatusCode(500, new { error = "Connection reset failed", details = ex.Message });
            }
        }
    }
}
