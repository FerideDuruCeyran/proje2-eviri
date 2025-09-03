using Microsoft.AspNetCore.Mvc;
using ExcelUploader.Services;
using ExcelUploader.Models;
using OfficeOpenXml;
using NPOI.HSSF.UserModel;

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
        public Task<IActionResult> GetSheetNames(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Task.FromResult<IActionResult>(BadRequest("Dosya seçilmedi."));
                }

                // Dosya formatını kontrol et
                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return Task.FromResult<IActionResult>(BadRequest("Sadece .xlsx ve .xls dosyaları desteklenir."));
                }

                // Excel dosyasının sayfa adlarını al
                var sheetNames = _excelAnalyzerService.GetSheetNamesAsync(file).Result;

                return Task.FromResult<IActionResult>(Ok(new
                {
                    success = true,
                    data = sheetNames,
                    message = "Sayfa adları başarıyla alındı."
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sayfa adları alınırken hata oluştu: {FileName}", file?.FileName);
                return Task.FromResult<IActionResult>(StatusCode(500, new
                {
                    success = false,
                    message = $"Sayfa adları alınırken hata oluştu: {ex.Message}"
                }));
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

        [HttpPost("debug")]
        public async Task<IActionResult> DebugExcelFile(IFormFile file, [FromQuery] int? sheetIndex = null)
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

                var debugInfo = new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    contentType = file.ContentType,
                    sheetIndex = sheetIndex ?? 0
                };

                if (file.FileName.EndsWith(".xlsx"))
                {
                    using var stream = file.OpenReadStream();
                    using var package = new ExcelPackage(stream);
                    
                    var sheetNames = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
                    var selectedSheetIndex = sheetIndex ?? 0;
                    
                    if (selectedSheetIndex >= package.Workbook.Worksheets.Count)
                    {
                        return BadRequest($"Sayfa indeksi geçersiz. Dosyada {package.Workbook.Worksheets.Count} sayfa var.");
                    }

                    var worksheet = package.Workbook.Worksheets[selectedSheetIndex];
                    var dimension = worksheet.Dimension;
                    
                    var sheetInfo = new
                    {
                        sheetName = worksheet.Name,
                        sheetIndex = selectedSheetIndex,
                        dimension = dimension != null ? new
                        {
                            startRow = dimension.Start.Row,
                            startColumn = dimension.Start.Column,
                            endRow = dimension.End.Row,
                            endColumn = dimension.End.Column,
                            rows = dimension.Rows,
                            columns = dimension.Columns
                        } : null,
                        hasData = dimension != null && dimension.Rows > 0 && dimension.Columns > 0
                    };

                    // Try to read first few cells
                    var sampleCells = new List<object>();
                    if (dimension != null)
                    {
                        for (int row = 1; row <= Math.Min(5, dimension.Rows); row++)
                        {
                            for (int col = 1; col <= Math.Min(5, dimension.Columns); col++)
                            {
                                try
                                {
                                    var cell = worksheet.Cells[row, col];
                                    sampleCells.Add(new
                                    {
                                        row = row,
                                        col = col,
                                        address = cell.Address,
                                        value = cell.Value,
                                        valueType = cell.Value?.GetType().Name,
                                        hasValue = cell.Value != null
                                    });
                                }
                                catch (Exception ex)
                                {
                                    sampleCells.Add(new
                                    {
                                        row = row,
                                        col = col,
                                        error = ex.Message
                                    });
                                }
                            }
                        }
                    }

                    return Ok(new
                    {
                        success = true,
                        debugInfo = debugInfo,
                        sheetNames = sheetNames,
                        selectedSheet = sheetInfo,
                        sampleCells = sampleCells,
                        message = "Debug bilgileri başarıyla alındı."
                    });
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    using var stream = file.OpenReadStream();
                    using var workbook = new HSSFWorkbook(stream);
                    
                    var sheetNames = new List<string>();
                    for (int i = 0; i < workbook.NumberOfSheets; i++)
                    {
                        sheetNames.Add(workbook.GetSheetName(i));
                    }
                    
                    var selectedSheetIndex = sheetIndex ?? 0;
                    if (selectedSheetIndex >= workbook.NumberOfSheets)
                    {
                        return BadRequest($"Sayfa indeksi geçersiz. Dosyada {workbook.NumberOfSheets} sayfa var.");
                    }

                    var sheet = workbook.GetSheetAt(selectedSheetIndex);
                    var lastRowNum = sheet.LastRowNum;
                    var headerRow = sheet.GetRow(0);
                    var lastCellNum = headerRow?.LastCellNum ?? 0;
                    
                    var sheetInfo = new
                    {
                        sheetName = sheet.SheetName,
                        sheetIndex = selectedSheetIndex,
                        lastRowNum = lastRowNum,
                        lastCellNum = lastCellNum,
                        hasData = lastRowNum > 0 && lastCellNum > 0
                    };

                    // Try to read first few cells
                    var sampleCells = new List<object>();
                    for (int row = 0; row <= Math.Min(4, (int)lastRowNum); row++)
                    {
                        var sheetRow = sheet.GetRow(row);
                        if (sheetRow != null)
                        {
                            for (int col = 0; col < Math.Min(5, (int)lastCellNum); col++)
                            {
                                try
                                {
                                    var cell = sheetRow.GetCell(col);
                                    sampleCells.Add(new
                                    {
                                        row = row,
                                        col = col,
                                        value = cell?.ToString(),
                                        cellType = cell?.CellType.ToString(),
                                        hasValue = cell != null
                                    });
                                }
                                catch (Exception ex)
                                {
                                    sampleCells.Add(new
                                    {
                                        row = row,
                                        col = col,
                                        error = ex.Message
                                    });
                                }
                            }
                        }
                    }

                    return Ok(new
                    {
                        success = true,
                        debugInfo = debugInfo,
                        sheetNames = sheetNames,
                        selectedSheet = sheetInfo,
                        sampleCells = sampleCells,
                        message = "Debug bilgileri başarıyla alındı."
                    });
                }

                return BadRequest("Desteklenmeyen dosya formatı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Debug Excel dosyası sırasında hata oluştu: {FileName}", file?.FileName);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Debug sırasında hata oluştu: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
