using Microsoft.AspNetCore.Mvc;
using ExcelUploader.Services;
using ExcelUploader.Models;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExcelAnalysisController : ControllerBase
    {
        private readonly IExcelAnalyzerService _excelAnalyzerService;
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ILogger<ExcelAnalysisController> _logger;

        public ExcelAnalysisController(
            IExcelAnalyzerService excelAnalyzerService,
            IDynamicTableService dynamicTableService,
            ILogger<ExcelAnalysisController> logger)
        {
            _excelAnalyzerService = excelAnalyzerService;
            _dynamicTableService = dynamicTableService;
            _logger = logger;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeExcelFile(IFormFile file, [FromQuery] int? sheetIndex = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Dosya seçilmedi.");
                }

                // Dosya formatını kontrol et
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest("Sadece .xlsx ve .xls dosyaları desteklenir.");
                }

                // Excel dosyasını analiz et
                var analysisResult = sheetIndex.HasValue 
                    ? await _excelAnalyzerService.AnalyzeExcelFileAsync(file, sheetIndex.Value)
                    : await _excelAnalyzerService.AnalyzeExcelFileAsync(file);

                return Ok(new
                {
                    success = true,
                    data = analysisResult,
                    message = "Excel dosyası başarıyla analiz edildi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel dosyası analiz edilirken hata oluştu: {FileName}", file?.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Excel dosyası analiz edilirken hata oluştu: {ex.Message}"
                });
            }
        }

        [HttpPost("get-sheet-names")]
        public async Task<IActionResult> GetSheetNames(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Dosya seçilmedi.");
                }

                // Dosya formatını kontrol et
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest("Sadece .xlsx ve .xls dosyaları desteklenir.");
                }

                // Excel dosyasının sayfa adlarını al
                var sheetNames = await _excelAnalyzerService.GetSheetNamesAsync(file);

                return Ok(new
                {
                    success = true,
                    data = sheetNames,
                    message = "Sayfa adları başarıyla alındı."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sayfa adları alınırken hata oluştu: {FileName}", file?.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Sayfa adları alınırken hata oluştu: {ex.Message}"
                });
            }
        }

        [HttpPost("create-table")]
        public async Task<IActionResult> CreateTableFromExcel(IFormFile file, [FromQuery] int? databaseConnectionId = null, [FromQuery] string? description = null, [FromQuery] int? sheetIndex = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Dosya seçilmedi.");
                }

                // Dosya formatını kontrol et
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest("Sadece .xlsx ve .xls dosyaları desteklenir.");
                }

                // Önce Excel dosyasını analiz et
                var analysisResult = sheetIndex.HasValue 
                    ? await _excelAnalyzerService.AnalyzeExcelFileAsync(file, sheetIndex.Value)
                    : await _excelAnalyzerService.AnalyzeExcelFileAsync(file);

                // Tablo oluştur
                var dynamicTable = await _dynamicTableService.CreateTableFromExcelAsync(file, "System", databaseConnectionId, description);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        table = dynamicTable,
                        analysis = analysisResult
                    },
                    message = "Tablo başarıyla oluşturuldu ve veriler eklendi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo oluşturulurken hata oluştu: {FileName}", file?.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Tablo oluşturulurken hata oluştu: {ex.Message}"
                });
            }
        }

        [HttpPost("create-table-structure")]
        public async Task<IActionResult> CreateTableStructure(IFormFile file, [FromQuery] int? databaseConnectionId = null, [FromQuery] string? description = null, [FromQuery] int? sheetIndex = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Dosya seçilmedi.");
                }

                // Dosya formatını kontrol et
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest("Sadece .xlsx ve .xls dosyaları desteklenir.");
                }

                // Önce Excel dosyasını analiz et
                var analysisResult = sheetIndex.HasValue 
                    ? await _excelAnalyzerService.AnalyzeExcelFileAsync(file, sheetIndex.Value)
                    : await _excelAnalyzerService.AnalyzeExcelFileAsync(file);

                // Sadece tablo yapısını oluştur
                var dynamicTable = await _dynamicTableService.CreateTableStructureAsync(file, "System", databaseConnectionId, description);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        table = dynamicTable,
                        analysis = analysisResult
                    },
                    message = "Tablo yapısı başarıyla oluşturuldu."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo yapısı oluşturulurken hata oluştu: {FileName}", file?.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Tablo yapısı oluşturulurken hata oluştu: {ex.Message}"
                });
            }
        }

        [HttpPost("insert-data/{tableId}")]
        public async Task<IActionResult> InsertDataIntoTable(int tableId, IFormFile file, [FromQuery] int? databaseConnectionId = null, [FromQuery] int? sheetIndex = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Dosya seçilmedi.");
                }

                // Dosya formatını kontrol et
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest("Sadece .xlsx ve .xls dosyaları desteklenir.");
                }

                // Verileri tabloya ekle
                var success = await _dynamicTableService.InsertDataIntoTableAsync(tableId, file, databaseConnectionId);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Veriler başarıyla tabloya eklendi."
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Veriler tabloya eklenirken hata oluştu."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veriler tabloya eklenirken hata oluştu: TableId={TableId}, FileName={FileName}", tableId, file?.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Veriler tabloya eklenirken hata oluştu: {ex.Message}"
                });
            }
        }

        [HttpGet("preview/{tableId}")]
        public async Task<IActionResult> GetTablePreview(int tableId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var table = await _dynamicTableService.GetTableByIdAsync(tableId);
                if (table == null)
                {
                    return NotFound("Tablo bulunamadı.");
                }

                var data = await _dynamicTableService.GetTableDataAsync(table.TableName, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        table = table,
                        data = data,
                        page = page,
                        pageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablo önizlemesi alınırken hata oluştu: TableId={TableId}", tableId);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Tablo önizlemesi alınırken hata oluştu: {ex.Message}"
                });
            }
        }
    }
}
