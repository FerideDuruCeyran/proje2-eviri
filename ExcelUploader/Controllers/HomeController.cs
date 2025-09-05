using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ExcelUploader.Models;
using ExcelUploader.Services;
using ExcelUploader.Data;
using System.Text.RegularExpressions;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IExcelService _excelService;
        private readonly IDynamicTableService _dynamicTableService;
        private readonly IExcelAnalyzerService _excelAnalyzerService;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            IExcelService excelService,
            IDynamicTableService dynamicTableService,
            IExcelAnalyzerService excelAnalyzerService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _excelService = excelService;
            _dynamicTableService = dynamicTableService;
            _excelAnalyzerService = excelAnalyzerService;
            _context = context;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
        }

        [HttpPost]
        [Route("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] UploadViewModel model)
        {
            try
            {
                _logger.LogInformation("Upload endpoint called");
                
                // Model validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() })
                        .ToList();
                    
                    var errorDetails = string.Join("; ", errors.SelectMany(e => e.Errors));
                    _logger.LogWarning("Model validation failed: {Errors}", errorDetails);
                    
                    return BadRequest(new { 
                        error = "Geçersiz veri formatı", 
                        details = errorDetails 
                    });
                }

                if (model.ExcelFile == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                _logger.LogInformation("Excel file received: {FileName}, Size: {Size}", 
                    model.ExcelFile.FileName, model.ExcelFile.Length);

                // Validate file
                var validationResult = await _excelService.ValidateExcelFileAsync(model.ExcelFile);
                if (!validationResult)
                {
                    return BadRequest(new { error = "Geçersiz Excel dosyası" });
                }

                // Generate table name from file name (without making it unique)
                var tableName = GenerateTableNameFromFileName(model.ExcelFile.FileName);
                
                _logger.LogInformation("Generated table name: {TableName}", tableName);

                // Analyze Excel structure and data types
                var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(model.ExcelFile);
                if (!analysisResult.IsSuccess)
                {
                    return BadRequest(new { error = "Excel dosyası analiz edilemedi", details = analysisResult.ErrorMessage });
                }

                _logger.LogInformation("Excel analysis completed. Headers: {HeaderCount}, Rows: {RowCount}", 
                    analysisResult.Headers.Count, analysisResult.Rows.Count);

                // Create dynamic table
                var tableCreationResult = await _dynamicTableService.CreateTableFromExcelAsync(
                    tableName, 
                    analysisResult.Headers, 
                    analysisResult.Rows, 
                    analysisResult.ColumnDataTypes);

                if (!tableCreationResult.IsSuccess)
                {
                    return StatusCode(500, new { 
                        error = "Tablo oluşturulamadı", 
                        details = tableCreationResult.ErrorMessage 
                    });
                }

                // Save or update table metadata to database
                var existingTable = await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == tableName);
                
                if (existingTable != null)
                {
                    // Update existing table metadata
                    existingTable.RowCount += analysisResult.Rows.Count; // Add new rows to existing count
                    existingTable.ProcessedDate = DateTime.UtcNow;
                    existingTable.IsProcessed = true;
                    
                    _logger.LogInformation("Updated existing table metadata: {TableName}, ID: {TableId}, New row count: {RowCount}", 
                        tableName, existingTable.Id, existingTable.RowCount);
                }
                else
                {
                    // Create new table metadata
                    var dynamicTable = new DynamicTable
                    {
                        TableName = tableName,
                        FileName = model.ExcelFile.FileName,
                        Description = model.Description ?? "",
                        UploadDate = DateTime.UtcNow,
                        RowCount = analysisResult.Rows.Count,
                        ColumnCount = analysisResult.Headers.Count,
                        IsProcessed = true,
                        ProcessedDate = DateTime.UtcNow
                    };

                    _context.DynamicTables.Add(dynamicTable);
                    
                    _logger.LogInformation("Created new table metadata: {TableName}, ID: {TableId}", 
                        tableName, dynamicTable.Id);
                }

                await _context.SaveChangesAsync();

                // Get the final table info for response
                var finalTable = existingTable ?? await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == tableName);
                var isNewTable = existingTable == null;

                return Ok(new { 
                    message = isNewTable 
                        ? $"Excel dosyası başarıyla yüklendi ve yeni tablo oluşturuldu: {tableName}" 
                        : $"Excel dosyası başarıyla yüklendi ve mevcut tabloya eklendi: {tableName}",
                    tableName = tableName,
                    tableId = finalTable?.Id,
                    rowCount = analysisResult.Rows.Count,
                    totalRowCount = finalTable?.RowCount ?? analysisResult.Rows.Count,
                    columnCount = analysisResult.Headers.Count,
                    action = isNewTable ? "table_created" : "data_inserted",
                    isNewTable = isNewTable,
                    fileName = model.ExcelFile.FileName,
                    fileSize = model.ExcelFile.Length,
                    columnTypes = analysisResult.ColumnDataTypes.Select(c => new 
                    { 
                        column = c.ColumnName, 
                        type = c.DetectedDataType, 
                        confidence = c.Confidence 
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in upload. File: {FileName}, Error: {ErrorMessage}", 
                    model.ExcelFile?.FileName ?? "null", ex.Message);
                
                return StatusCode(500, new { 
                    error = "Dosya yükleme sırasında hata oluştu", 
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        private string GenerateTableNameFromFileName(string fileName)
        {
            // Remove extension and special characters
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            // Convert Turkish characters to English equivalents
            var turkishToEnglish = new Dictionary<char, char>
            {
                {'ç', 'c'}, {'Ç', 'C'},
                {'ğ', 'g'}, {'Ğ', 'G'},
                {'ı', 'i'}, {'I', 'I'},
                {'ö', 'o'}, {'Ö', 'O'},
                {'ş', 's'}, {'Ş', 'S'},
                {'ü', 'u'}, {'Ü', 'U'},
                {'İ', 'I'}
            };

            var cleanName = nameWithoutExt;
            
            // Replace Turkish characters
            foreach (var kvp in turkishToEnglish)
            {
                cleanName = cleanName.Replace(kvp.Key, kvp.Value);
            }

            // Replace whitespace with underscore
            cleanName = cleanName.Replace(' ', '_');
            
            // Remove or replace other special characters that are not valid in SQL identifiers
            cleanName = Regex.Replace(cleanName, @"[^a-zA-Z0-9_]", "_");
            
            // Remove consecutive underscores
            cleanName = Regex.Replace(cleanName, @"_+", "_");
            
            // Remove leading and trailing underscores
            cleanName = cleanName.Trim('_');
            
            // Ensure it starts with a letter
            if (cleanName.Length > 0 && !char.IsLetter(cleanName[0]))
            {
                cleanName = "Table_" + cleanName;
            }
            
            // If empty after cleaning, provide a default name
            if (string.IsNullOrEmpty(cleanName))
            {
                cleanName = "Table_1";
            }
            
            // Limit length
            if (cleanName.Length > 50)
            {
                cleanName = cleanName.Substring(0, 50);
            }
            
            return cleanName;
        }

        private async Task<string> GenerateUniqueTableNameAsync(string baseName)
        {
            var tableName = baseName;
            var counter = 1;
            
            while (await _context.DynamicTables.AnyAsync(t => t.TableName == tableName))
            {
                tableName = $"{baseName}_{counter}";
                counter++;
            }
            
            return tableName;
        }

        [HttpGet("tables")]
        [Authorize]
        public async Task<IActionResult> GetTables()
        {
            try
            {
                var tables = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Select(t => new
                    {
                        t.Id,
                        t.TableName,
                        t.FileName,
                        t.Description,
                        t.UploadDate,
                        t.RowCount,
                        t.ColumnCount
                    })
                    .ToListAsync();

                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables");
                return StatusCode(500, new { error = "Tablolar alınırken hata oluştu" });
            }
        }

        [HttpGet("table/{id}")]
        [Authorize]
        public async Task<IActionResult> GetTableData(int id)
        {
            try
            {
                var table = await _context.DynamicTables.FindAsync(id);
                if (table == null)
                {
                    return NotFound(new { error = "Tablo bulunamadı" });
                }

                var data = await _dynamicTableService.GetTableDataAsync(table.TableName);
                if (!data.IsSuccess)
                {
                    return StatusCode(500, new { error = "Tablo verisi alınamadı", details = data.ErrorMessage });
                }

                return Ok(new
                {
                    table = new
                    {
                        table.Id,
                        table.TableName,
                        table.FileName,
                        table.Description,
                        table.UploadDate,
                        table.RowCount,
                        table.ColumnCount
                    },
                    data = data.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data for ID: {Id}", id);
                return StatusCode(500, new { error = "Tablo verisi alınırken hata oluştu" });
            }
        }

        [HttpPost("preview")]
        [Authorize]
        public async Task<IActionResult> PreviewExcelFile(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                _logger.LogInformation("Preview requested for file: {FileName}", file.FileName);

                // Validate file
                var isValid = await _excelService.ValidateExcelFileAsync(file);
                if (!isValid)
                {
                    return BadRequest(new { error = "Geçersiz Excel dosyası" });
                }

                // Analyze Excel file
                var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(file);
                if (!analysisResult.IsSuccess)
                {
                    return BadRequest(new { error = "Excel dosyası analiz edilemedi", details = analysisResult.ErrorMessage });
                }

                // Get sheet names
                var sheetNames = await _excelAnalyzerService.GetSheetNamesAsync(file);

                // Return preview data (first 10 rows)
                var previewData = analysisResult.Rows.Take(10).ToList();

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    sheetNames = sheetNames,
                    headers = analysisResult.Headers,
                    totalRows = analysisResult.Rows.Count,
                    totalColumns = analysisResult.Headers.Count,
                    previewRows = previewData.Count,
                    columnTypes = analysisResult.ColumnDataTypes.Select(c => new
                    {
                        column = c.ColumnName,
                        type = c.DetectedDataType,
                        confidence = c.Confidence,
                        totalValues = c.TotalValues,
                        nonNullValues = c.NonNullValues,
                        nullValues = c.NullValues
                    }).ToList(),
                    previewData = previewData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing Excel file: {FileName}", file?.FileName);
                return StatusCode(500, new { error = "Excel dosyası önizlenirken hata oluştu" });
            }
        }

        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalFiles = await _context.DynamicTables.CountAsync();
                var totalRecords = await _context.DynamicTables.SumAsync(t => t.RowCount);
                var totalTables = totalFiles; // For backward compatibility
                var processedTables = await _context.DynamicTables.CountAsync(t => t.IsProcessed);
                var recentUploads = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Take(5)
                    .Select(t => new
                    {
                        t.TableName,
                        t.FileName,
                        t.UploadDate,
                        t.RowCount,
                        t.ColumnCount
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalFiles = totalFiles,
                    totalRecords = totalRecords,
                    totalTables = totalTables,
                    totalRows = totalRecords, // For backward compatibility
                    processedTables = processedTables,
                    pendingTables = totalTables - processedTables,
                    recentUploads = recentUploads,
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return StatusCode(500, new { error = "İstatistikler alınırken hata oluştu" });
            }
        }

        [HttpGet("data")]
        [Authorize]
        public async Task<IActionResult> GetData([FromQuery] int? tableId = null)
        {
            try
            {
                if (tableId.HasValue)
                {
                    // Get specific table data
                    var table = await _context.DynamicTables.FindAsync(tableId.Value);
                    if (table == null)
                    {
                        return NotFound(new { error = "Tablo bulunamadı" });
                    }

                    var data = await _dynamicTableService.GetTableDataAsync(table.TableName);
                    if (!data.IsSuccess)
                    {
                        return StatusCode(500, new { error = "Tablo verisi alınamadı", details = data.ErrorMessage });
                    }

                    return Ok(data.Data);
                }
                else
                {
                    // Get all tables list for dropdown
                    var tables = await _context.DynamicTables
                        .OrderByDescending(t => t.UploadDate)
                        .Select(t => new
                        {
                            id = t.Id,
                            tableName = t.TableName,
                            fileName = t.FileName,
                            description = t.Description,
                            uploadDate = t.UploadDate,
                            rowCount = t.RowCount,
                            columnCount = t.ColumnCount
                        })
                        .ToListAsync();

                    return Ok(tables);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data");
                return StatusCode(500, new { error = "Veriler alınırken hata oluştu" });
            }
        }

        [HttpGet("test-database")]
        [Authorize]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection test result: {CanConnect}", canConnect);
                
                if (!canConnect)
                {
                    _logger.LogWarning("Database connection failed");
                    return Ok(new { 
                        isConnected = false,
                        databaseInfo = new { Database = "Unknown", Server = "Unknown" }
                    });
                }

                // Test basic query
                var tableCount = await _context.DynamicTables.CountAsync();
                _logger.LogInformation("Successfully queried DynamicTables, count: {TableCount}", tableCount);

                // Get database information
                var connection = _context.Database.GetDbConnection();
                var databaseName = connection.Database ?? "Unknown";
                var serverName = connection.DataSource ?? "Unknown";
                
                _logger.LogInformation("Database info - Name: {DatabaseName}, Server: {ServerName}", databaseName, serverName);

                return Ok(new
                {
                    isConnected = true,
                    databaseInfo = new
                    {
                        Database = databaseName,
                        Server = serverName
                    },
                    tableCount = tableCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed with exception");
                return Ok(new { 
                    isConnected = false,
                    databaseInfo = new { Database = "Error", Server = "Error" },
                    error = ex.Message
                });
            }
        }

        [HttpGet("recent-activities")]
        [Authorize]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var recentTables = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Take(10)
                    .Select(t => new
                    {
                        id = t.Id,
                        type = "table_upload",
                        title = $"Tablo yüklendi: {t.TableName}",
                        description = $"{t.FileName} dosyasından {t.RowCount} satır, {t.ColumnCount} sütun",
                        timestamp = t.UploadDate,
                        status = t.IsProcessed ? "completed" : "pending",
                        data = new
                        {
                            tableName = t.TableName,
                            fileName = t.FileName,
                            rowCount = t.RowCount,
                            columnCount = t.ColumnCount
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    activities = recentTables,
                    totalCount = recentTables.Count,
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, new { error = "Son aktiviteler alınırken hata oluştu" });
            }
        }

        [HttpGet("check-table-exists")]
        [Authorize]
        public async Task<IActionResult> CheckTableExists([FromQuery] string tableName, [FromQuery] string databaseConnectionId = "")
        {
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return BadRequest(new { error = "Tablo adı gereklidir" });
                }

                // Check if table exists in our tracking system
                var existingTable = await _context.DynamicTables
                    .FirstOrDefaultAsync(t => t.TableName == tableName);

                if (existingTable != null)
                {
                    return Ok(new { exists = true, tableName = tableName });
                }

                // Also check in the actual database
                try
                {
                    var connectionString = _context.Database.GetConnectionString();
                    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                    await connection.OpenAsync();

                    var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
                    using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@TableName", tableName);

                    var count = (int)await command.ExecuteScalarAsync();
                    return Ok(new { exists = count > 0, tableName = tableName });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not check actual database for table {TableName}", tableName);
                    // Return false if we can't check the actual database
                    return Ok(new { exists = false, tableName = tableName });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking table exists for {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo kontrolü sırasında hata oluştu" });
            }
        }

        [HttpGet("view-table")]
        [Authorize]
        public async Task<IActionResult> ViewTable([FromQuery] string tableName, [FromQuery] string databaseConnectionId = "")
        {
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return BadRequest(new { error = "Tablo adı gereklidir" });
                }

                var tableData = await _dynamicTableService.GetTableDataAsync(tableName);
                if (!tableData.IsSuccess)
                {
                    return BadRequest(new { error = tableData.ErrorMessage });
                }

                return Ok(new
                {
                    tableName = tableName,
                    data = tableData.Data,
                    rowCount = tableData.Data?.Count ?? 0,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing table {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo görüntülenirken hata oluştu" });
            }
        }

        [HttpGet("last-login")]
        [Authorize]
        public async Task<IActionResult> GetLastLogin()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "Kullanıcı kimliği bulunamadı" });
                }

                // Get the last successful login for the current user
                var lastLogin = await _context.LoginLogs
                    .Where(l => l.UserId == userId && l.IsSuccess)
                    .OrderByDescending(l => l.LoginTime)
                    .FirstOrDefaultAsync();

                if (lastLogin != null)
                {
                    return Ok(new { lastLogin = lastLogin.LoginTime });
                }
                else
                {
                    return Ok(new { lastLogin = (DateTime?)null });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last login");
                return StatusCode(500, new { error = "Son giriş bilgisi alınırken hata oluştu" });
            }
        }

        [HttpGet("test-connection")]
        [AllowAnonymous]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("Testing database connection without authentication...");
                
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection test result: {CanConnect}", canConnect);
                
                if (!canConnect)
                {
                    _logger.LogWarning("Database connection failed");
                    return Ok(new { 
                        isConnected = false,
                        message = "Database connection failed",
                        timestamp = DateTime.UtcNow
                    });
                }

                // Get database information
                var connection = _context.Database.GetDbConnection();
                var databaseName = connection.Database ?? "Unknown";
                var serverName = connection.DataSource ?? "Unknown";
                
                _logger.LogInformation("Database info - Name: {DatabaseName}, Server: {ServerName}", databaseName, serverName);

                return Ok(new
                {
                    isConnected = true,
                    message = "Database connection successful",
                    databaseInfo = new
                    {
                        Database = databaseName,
                        Server = serverName
                    },
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed with exception");
                return Ok(new { 
                    isConnected = false,
                    message = $"Database connection test failed: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("test-database-creation")]
        [AllowAnonymous]
        public async Task<IActionResult> TestDatabaseCreation()
        {
            try
            {
                _logger.LogInformation("Testing database creation...");
                
                // Try to create the database if it doesn't exist
                var created = await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database creation result: {Created}", created);
                
                // Test connection after creation
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection after creation: {CanConnect}", canConnect);
                
                if (!canConnect)
                {
                    return Ok(new { 
                        success = false,
                        message = "Database creation failed or connection still not working",
                        timestamp = DateTime.UtcNow
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Database created/connected successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database creation test failed with exception");
                return Ok(new { 
                    success = false,
                    message = $"Database creation test failed: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
