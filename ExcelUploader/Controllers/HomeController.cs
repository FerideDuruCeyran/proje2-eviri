using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ExcelUploader.Models;
using ExcelUploader.Services;
using ExcelUploader.Data;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IExcelService _excelService;
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ApplicationDbContext _context;

        public HomeController(
            IExcelService excelService,
            IDynamicTableService dynamicTableService,
            ApplicationDbContext context)
        {
            _excelService = excelService;
            _dynamicTableService = dynamicTableService;
            _context = context;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
        }

        [HttpGet("dashboard-data")]
        [Authorize]
        public async Task<IActionResult> DashboardData()
        {
            try
            {
                var totalTables = await _context.DynamicTables.CountAsync();
                var totalRecords = await _context.DynamicTables.SumAsync(t => t.RowCount);
                var totalFiles = await _context.DynamicTables.CountAsync();
                
                // Get last login from login logs - handle case where table might not exist or is empty
                DateTime? lastLogin = null;
                try
                {
                    var lastLoginLog = await _context.LoginLogs
                        .OrderByDescending(l => l.LoginTime)
                        .FirstOrDefaultAsync();
                    lastLogin = lastLoginLog?.LoginTime;
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the request
                    Console.WriteLine($"Error accessing LoginLogs: {ex.Message}");
                    lastLogin = DateTime.Now;
                }

                // If no login logs exist, use current time
                if (lastLogin == null)
                {
                    lastLogin = DateTime.Now;
                }

                var result = new
                {
                    stats = new
                    {
                        totalTables,
                        totalRecords,
                        totalFiles,
                        lastUpdate = DateTime.Now
                    },
                    lastLogin = lastLogin,
                    activities = new List<object>() // Empty activities for now
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                Console.WriteLine($"Dashboard data error: {ex.Message}");
                return StatusCode(500, new { error = "Dashboard data error: " + ex.Message });
=======
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
            }
        }

        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> Stats()
        {
            try
            {
<<<<<<< HEAD
                var totalTables = await _context.DynamicTables.CountAsync();
                var totalRecords = await _context.DynamicTables.SumAsync(t => t.RowCount);
                var totalFiles = await _context.DynamicTables.CountAsync();

                return Ok(new
                {
                    totalTables,
                    totalRecords,
                    totalFiles,
                    lastUpdate = DateTime.Now
=======
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Stats error: " + ex.Message });
            }
        }

        [HttpGet("recent-activities")]
        [Authorize]
        public async Task<IActionResult> RecentActivities()
        {
            try
            {
                var recentTables = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Take(10)
                    .Select(t => new
                    {
                        id = t.Id,
                        tableName = t.TableName,
                        fileName = t.FileName ?? "Bilinmeyen dosya",
                        uploadDate = t.UploadDate,
                        rowCount = t.RowCount,
                        description = t.Description ?? ""
                    })
                    .ToListAsync();

                return Ok(recentTables);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Recent activities error: " + ex.Message });
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
<<<<<<< HEAD
                var tableCount = await _context.DynamicTables.CountAsync();
                
                return Ok(new
                {
                    isConnected = canConnect,
                    databaseInfo = new
=======
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
                    {
                        Database = "ExcelUploader",
                        Server = "localhost",
                        TableCount = tableCount
                    },
                    message = canConnect ? "Veritabanı bağlantısı başarılı" : "Veritabanı bağlantısı başarısız",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Database test error: " + ex.Message });
            }
        }

        [HttpGet("test-connection-simple")]
        public async Task<IActionResult> TestConnectionSimple()
        {
            try
            {
                // Simple database connection test
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new
                {
                    success = canConnect,
                    message = canConnect ? "Bağlantı başarılı" : "Bağlantı başarısız",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Connection test error: " + ex.Message });
            }
        }

        [HttpGet("data")]
        [Authorize]
        public async Task<IActionResult> Data([FromQuery] int? tableId = null)
        {
            try
            {
                if (tableId.HasValue)
                {
                    // Return specific table data
                    var table = await _context.DynamicTables
                        .FirstOrDefaultAsync(t => t.Id == tableId.Value);
                    
                    if (table == null)
                    {
                        return NotFound(new { error = "Tablo bulunamadı" });
                    }

                    return Ok(new List<object> { table });
                }
                else
                {
                    // Return all tables
                    var tables = await _context.DynamicTables
                        .Select(t => new
                        {
                            id = t.Id,
                            tableName = t.TableName,
                            fileName = t.FileName ?? "Bilinmeyen dosya",
                            uploadDate = t.UploadDate,
                            rowCount = t.RowCount,
                            description = t.Description
                        })
                        .ToListAsync();

                    return Ok(tables);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Data error: " + ex.Message });
            }
        }

        [HttpGet("view-table")]
        [Authorize]
        public async Task<IActionResult> ViewTable([FromQuery] string tableName, [FromQuery] string? databaseConnectionId = null)
        {
            try
            {
                var table = await _context.DynamicTables
                    .FirstOrDefaultAsync(t => t.TableName == tableName);
                
                if (table == null)
                {
                    return NotFound(new { error = "Tablo bulunamadı" });
                }

                return Ok(new
                {
                    id = table.Id,
                    tableName = table.TableName,
                    fileName = table.FileName,
                    uploadDate = table.UploadDate,
                    rowCount = table.RowCount,
                    description = table.Description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "View table error: " + ex.Message });
            }
        }

<<<<<<< HEAD
        [HttpDelete("delete-table/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTable(int id)
        {
            try
            {
                var table = await _context.DynamicTables.FindAsync(id);
                if (table == null)
                {
                    return NotFound(new { error = "Tablo bulunamadı" });
                }

                _context.DynamicTables.Remove(table);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Tablo başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Delete table error: " + ex.Message });
            }
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] UploadViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Geçersiz veri formatı" });
                }

                if (model.ExcelFile == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                // Generate table name
                var tableName = GenerateTableNameFromFileName(model.ExcelFile.FileName);
                tableName = await GenerateUniqueTableNameAsync(tableName);

                // Create dynamic table
                var result = await _dynamicTableService.CreateTableFromExcelAsync(
                    tableName, 
                    model.ExcelFile, 
                    model.Description ?? "");

                if (result.IsSuccess)
                {
                    var tableResult = result as TableCreationResult;
                    return Ok(new { 
                        success = true, 
                        message = "Dosya başarıyla yüklendi",
                        tableName = tableName,
                        rowCount = tableResult?.RowCount ?? 0,
                        columnCount = tableResult?.ColumnCount ?? 0
                    });
                }
                else
                {
                    return BadRequest(new { error = result.ErrorMessage });
=======
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
                }
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                return StatusCode(500, new { error = "Sunucu hatası: " + ex.Message });
            }
        }

        [HttpPost("preview")]
        [Authorize]
        public async Task<IActionResult> Preview([FromForm] IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new { error = "Dosya bulunamadı" });
                }

                // Validate file type
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest(new { error = "Sadece Excel dosyaları (.xlsx, .xls) desteklenir" });
                }

                // Validate file size (50MB limit)
                if (file.Length > 50 * 1024 * 1024)
                {
                    return BadRequest(new { error = "Dosya boyutu 50MB'dan büyük olamaz" });
                }

                var preview = await _excelService.GetExcelPreviewAsync(file);
                return Ok(new { success = true, data = preview });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Önizleme hatası: " + ex.Message });
            }
        }

        [HttpGet("check-table-exists")]
        public async Task<IActionResult> CheckTableExists([FromQuery] string tableName, [FromQuery] string? databaseConnectionId = null)
        {
            try
            {
                var exists = await _context.DynamicTables.AnyAsync(t => t.TableName == tableName);
                return Ok(new { exists = exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Tablo kontrol hatası: " + ex.Message });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var totalTables = await _context.DynamicTables.CountAsync();
                var processedTables = await _context.DynamicTables.CountAsync(t => t.IsProcessed);
                var totalRows = await _context.DynamicTables.SumAsync(t => t.RowCount);
                var recentTables = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Take(5)
                    .Select(t => new { t.TableName, t.FileName, t.UploadDate, t.RowCount })
                    .ToListAsync();

                return Ok(new
                {
                    totalTables,
                    processedTables,
                    pendingTables = totalTables - processedTables,
                    totalRows,
                    recentTables
=======
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
                });
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                return StatusCode(500, new { error = "Dashboard hatası: " + ex.Message });
            }
        }

        private string GenerateTableNameFromFileName(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            // Remove special characters and replace with underscore
            name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
            return name.Length > 50 ? name.Substring(0, 50) : name;
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
=======
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
>>>>>>> fbf1790ad5d630d065f59967cc2c0538e5e773c2
        }
    }
}
