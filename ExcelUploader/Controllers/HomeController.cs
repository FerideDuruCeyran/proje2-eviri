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
                Console.WriteLine($"Dashboard data error: {ex.Message}");
                return StatusCode(500, new { error = "Dashboard data error: " + ex.Message });
            }
        }

        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var totalTables = await _context.DynamicTables.CountAsync();
                var totalRecords = await _context.DynamicTables.SumAsync(t => t.RowCount);
                var totalFiles = await _context.DynamicTables.CountAsync();

                return Ok(new
                {
                    totalTables,
                    totalRecords,
                    totalFiles,
                    lastUpdate = DateTime.Now
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
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                var tableCount = await _context.DynamicTables.CountAsync();
                
                return Ok(new
                {
                    isConnected = canConnect,
                    databaseInfo = new
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
                }
            }
            catch (Exception ex)
            {
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
                });
            }
            catch (Exception ex)
            {
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
        }
    }
}
