using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Models;
using ExcelUploader.Services;
using System.Security.Claims;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IDataImportService _dataImportService;
        private readonly IExcelService _excelService;
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDataImportService dataImportService, IExcelService excelService, IDynamicTableService dynamicTableService, ILogger<HomeController> logger)
        {
            _dataImportService = dataImportService;
            _excelService = excelService;
            _dynamicTableService = dynamicTableService;
            _logger = logger;
        }

        [HttpGet]
        [Route("")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            return Ok(new { message = "Excel Uploader API", version = "9.0", status = "Running" });
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
                var dynamicTable = await _dynamicTableService.CreateTableFromExcelAsync(model.ExcelFile, userName, model.Description);

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

        [HttpGet]
        [Route("data")]
        [Authorize]
        public async Task<IActionResult> Data(int? tableId = null)
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
    }
}
