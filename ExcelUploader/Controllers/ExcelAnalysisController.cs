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
    [Authorize]
    public class ExcelAnalysisController : ControllerBase
    {
        private readonly IExcelAnalyzerService _excelAnalyzerService;
        private readonly IDynamicTableService _dynamicTableService;
        private readonly IExcelService _excelService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExcelAnalysisController> _logger;

        public ExcelAnalysisController(
            IExcelAnalyzerService excelAnalyzerService,
            IDynamicTableService dynamicTableService,
            IExcelService excelService,
            ApplicationDbContext context,
            ILogger<ExcelAnalysisController> logger)
        {
            _excelAnalyzerService = excelAnalyzerService;
            _dynamicTableService = dynamicTableService;
            _excelService = excelService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeExcelFile(IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                _logger.LogInformation("Analyzing Excel file: {FileName}", file.FileName);

                // Validate file
                var isValid = await _excelService.ValidateExcelFileAsync(file);
                if (!isValid)
                {
                    return BadRequest(new { error = "Geçersiz Excel dosyası" });
                }

                // Get sheet names
                var sheetNames = await _excelAnalyzerService.GetSheetNamesAsync(file);
                if (!sheetNames.Any())
                {
                    return BadRequest(new { error = "Excel dosyasında çalışma sayfası bulunamadı" });
                }

                // Analyze first sheet
                var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(file, 0);
                if (!analysisResult.IsSuccess)
                {
                    return BadRequest(new { error = "Excel dosyası analiz edilemedi", details = analysisResult.ErrorMessage });
                }

                return Ok(new
                {
                    fileName = file.FileName,
                    fileSize = file.Length,
                    sheetNames = sheetNames,
                    headers = analysisResult.Headers,
                    rowCount = analysisResult.Rows.Count,
                    columnCount = analysisResult.Headers.Count,
                    columnTypes = analysisResult.ColumnDataTypes.Select(c => new
                    {
                        column = c.ColumnName,
                        type = c.DetectedDataType,
                        confidence = c.Confidence,
                        totalValues = c.TotalValues,
                        nonNullValues = c.NonNullValues,
                        nullValues = c.NullValues
                    }).ToList(),
                    sampleData = analysisResult.Rows.Take(10).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing Excel file: {FileName}", file?.FileName);
                return StatusCode(500, new { error = "Excel dosyası analiz edilirken hata oluştu" });
            }
        }

        [HttpPost("create-table")]
        public async Task<IActionResult> CreateTableFromAnalysis([FromForm] CreateTableRequest request)
        {
            try
            {
                if (request.File == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                _logger.LogInformation("Creating table from analysis: {FileName}", request.File.FileName);

                // Analyze Excel file
                var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(request.File, 0);
                if (!analysisResult.IsSuccess)
                {
                    return BadRequest(new { error = "Excel dosyası analiz edilemedi", details = analysisResult.ErrorMessage });
                }

                // Generate table name from file name (without making it unique)
                var tableName = GenerateTableNameFromFileName(request.File.FileName);

                // Create table
                var createResult = await _dynamicTableService.CreateTableFromExcelAsync(
                    tableName,
                    analysisResult.Headers,
                    analysisResult.Rows,
                    analysisResult.ColumnDataTypes);

                if (!createResult.IsSuccess)
                {
                    return StatusCode(500, new { error = "Tablo oluşturulamadı", details = createResult.ErrorMessage });
                }

                // Save or update table metadata
                var existingTable = await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == tableName);
                
                if (existingTable != null)
                {
                    // Update existing table metadata
                    existingTable.RowCount += analysisResult.Rows.Count; // Add new rows to existing count
                    existingTable.ProcessedDate = DateTime.UtcNow;
                    existingTable.IsProcessed = true;
                }
                else
                {
                    // Create new table metadata
                    var dynamicTable = new DynamicTable
                    {
                        TableName = tableName,
                        FileName = request.File.FileName,
                        Description = request.Description ?? "",
                        UploadDate = DateTime.UtcNow,
                        RowCount = analysisResult.Rows.Count,
                        ColumnCount = analysisResult.Headers.Count,
                        IsProcessed = true,
                        ProcessedDate = DateTime.UtcNow
                    };

                    _context.DynamicTables.Add(dynamicTable);
                }

                await _context.SaveChangesAsync();

                // Get the final table info for response
                var finalTable = existingTable ?? await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == tableName);
                var isNewTable = existingTable == null;

                return Ok(new
                {
                    message = isNewTable 
                        ? $"Tablo başarıyla oluşturuldu: {tableName}"
                        : $"Veri başarıyla mevcut tabloya eklendi: {tableName}",
                    tableName = tableName,
                    tableId = finalTable?.Id,
                    rowCount = analysisResult.Rows.Count,
                    totalRowCount = finalTable?.RowCount ?? analysisResult.Rows.Count,
                    columnCount = analysisResult.Headers.Count,
                    isNewTable = isNewTable
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table from analysis: {FileName}", request.File?.FileName);
                return StatusCode(500, new { error = "Tablo oluşturulurken hata oluştu" });
            }
        }

        [HttpPost("insert-data")]
        public async Task<IActionResult> InsertDataIntoTable([FromForm] InsertDataRequest request)
        {
            try
            {
                if (request.File == null)
                {
                    return BadRequest(new { error = "Lütfen bir Excel dosyası seçin" });
                }

                if (string.IsNullOrEmpty(request.TableName))
                {
                    return BadRequest(new { error = "Tablo adı belirtilmelidir" });
                }

                _logger.LogInformation("Inserting data into table: {TableName}", request.TableName);

                // Analyze Excel file
                var analysisResult = await _excelAnalyzerService.AnalyzeExcelFileAsync(request.File, 0);
                if (!analysisResult.IsSuccess)
                {
                    return BadRequest(new { error = "Excel dosyası analiz edilemedi", details = analysisResult.ErrorMessage });
                }

                // Create table if it doesn't exist
                var createResult = await _dynamicTableService.CreateTableFromExcelAsync(
                    request.TableName,
                    analysisResult.Headers,
                    analysisResult.Rows,
                    analysisResult.ColumnDataTypes);

                if (!createResult.IsSuccess)
                {
                    return StatusCode(500, new { error = "Veri eklenemedi", details = createResult.ErrorMessage });
                }

                // Update tracking table
                var existingTable = await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == request.TableName);
                if (existingTable != null)
                {
                    existingTable.RowCount = analysisResult.Rows.Count;
                    existingTable.IsProcessed = true;
                    existingTable.ProcessedDate = DateTime.UtcNow;
                }
                else
                {
                    var newTable = new DynamicTable
                    {
                        TableName = request.TableName,
                        FileName = request.File.FileName,
                        Description = request.Description ?? "",
                        UploadDate = DateTime.UtcNow,
                        RowCount = analysisResult.Rows.Count,
                        ColumnCount = analysisResult.Headers.Count,
                        IsProcessed = true,
                        ProcessedDate = DateTime.UtcNow
                    };
                    _context.DynamicTables.Add(newTable);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Veri başarıyla eklendi: {request.TableName}",
                    tableName = request.TableName,
                    rowCount = analysisResult.Rows.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting data into table: {TableName}", request.TableName);
                return StatusCode(500, new { error = "Veri eklenirken hata oluştu" });
            }
        }

        [HttpGet("table/{id}/analysis")]
        public async Task<IActionResult> GetTableAnalysis(int id)
        {
            try
            {
                var table = await _context.DynamicTables
                    .Include(t => t.Columns)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (table == null)
                {
                    return NotFound(new { error = "Tablo bulunamadı" });
                }

                return Ok(new
                {
                    table.Id,
                    table.TableName,
                    table.FileName,
                    table.Description,
                    table.UploadDate,
                    table.RowCount,
                    table.ColumnCount,
                    table.IsProcessed,
                    table.ProcessedDate,
                    columns = table.Columns.Select(c => new
                    {
                        c.ColumnName,
                        c.DisplayName,
                        c.DataType,
                        c.ColumnOrder,
                        c.MaxLength,
                        c.IsRequired,
                        c.IsUnique
                    }).OrderBy(c => c.ColumnOrder).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table analysis for ID: {Id}", id);
                return StatusCode(500, new { error = "Tablo analizi alınırken hata oluştu" });
            }
        }

        private string GenerateTableNameFromFileName(string fileName)
        {
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
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"[^a-zA-Z0-9_]", "_");
            
            // Remove consecutive underscores
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"_+", "_");
            
            // Remove leading and trailing underscores
            cleanName = cleanName.Trim('_');
            
            if (cleanName.Length > 0 && !char.IsLetter(cleanName[0]))
            {
                cleanName = "Table_" + cleanName;
            }
            
            // If empty after cleaning, provide a default name
            if (string.IsNullOrEmpty(cleanName))
            {
                cleanName = "Table_1";
            }
            
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
    }

    public class CreateTableRequest
    {
        public IFormFile File { get; set; }
        public string Description { get; set; }
    }

    public class InsertDataRequest
    {
        public IFormFile File { get; set; }
        public string TableName { get; set; }
        public string Description { get; set; }
    }
}
