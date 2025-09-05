using ExcelUploader.Models;
using OfficeOpenXml;
using ClosedXML.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace ExcelUploader.Services
{
    public class ExcelService : IExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<object> GetExcelPreviewAsync(IFormFile file)
        {
            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    return await GetXlsxPreviewAsync(file);
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    return await GetXlsPreviewAsync(file);
                }
                else
                {
                    throw new ArgumentException("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excel dosyası önizlenirken hata oluştu: {file.FileName}");
                throw;
            }
        }

        public async Task<object> GetExcelPreviewAsync(IFormFile file, int sheetIndex = 0)
        {
            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    return await GetXlsxPreviewAsync(file, sheetIndex);
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    return await GetXlsPreviewAsync(file, sheetIndex);
                }
                else
                {
                    throw new ArgumentException("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excel dosyası önizlenirken hata oluştu: {file.FileName}");
                throw;
            }
        }

        public Task<List<string>> GetSheetNamesAsync(IFormFile file)
        {
            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    using var stream = file.OpenReadStream();
                    using var package = new ExcelPackage(stream);
                    return Task.FromResult(package.Workbook.Worksheets.Select(ws => ws.Name).ToList());
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
                    return Task.FromResult(sheetNames);
                }
                else
                {
                    throw new ArgumentException("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excel dosyası sayfa adları alınırken hata oluştu: {file.FileName}");
                throw;
            }
        }

        private Task<object> GetXlsxPreviewAsync(IFormFile file, int sheetIndex = 0)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                
                _logger.LogInformation($"Processing XLSX file: {file.FileName}, Sheets: {package.Workbook.Worksheets.Count}");
                
                if (package.Workbook.Worksheets.Count == 0)
                    throw new ArgumentException("Excel dosyası boş veya geçersiz.");

                // Validate sheet index
                if (sheetIndex < 0 || sheetIndex >= package.Workbook.Worksheets.Count)
                {
                    throw new ArgumentException($"Geçersiz sayfa indeksi: {sheetIndex}. Dosyada {package.Workbook.Worksheets.Count} sayfa bulunmaktadır.");
                }

                var worksheet = package.Workbook.Worksheets[sheetIndex];
                var dimension = worksheet.Dimension;

                _logger.LogInformation($"Worksheet: {worksheet.Name}, Dimension: {dimension?.Address}");

                if (dimension == null)
                {
                    _logger.LogWarning($"Worksheet {worksheet.Name} has no dimension (empty sheet)");
                    return Task.FromResult<object>(new 
                    { 
                        fileName = file.FileName,
                        sheetName = worksheet.Name,
                        totalRows = 0,
                        totalColumns = 0,
                        headers = new List<string>(), 
                        data = new List<List<object>>() 
                    });
                }

                var headers = new List<string>();
                var data = new List<List<object>>();

                // Read headers (first row)
                for (int col = 1; col <= dimension.End.Column; col++)
                {
                    var cellValue = worksheet.Cells[1, col].Value;
                    headers.Add(cellValue?.ToString() ?? $"Column{col}");
                }

                _logger.LogInformation($"Found {headers.Count} headers: {string.Join(", ", headers)}");

                // Read data (first 10 rows)
                var maxRows = Math.Min(10, dimension.End.Row - 1);
                for (int row = 2; row <= maxRows + 1; row++)
                {
                    var rowData = new List<object>();
                    for (int col = 1; col <= dimension.End.Column; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value;
                        rowData.Add(cellValue ?? "");
                    }
                    data.Add(rowData);
                }

                _logger.LogInformation($"Read {data.Count} data rows");

                return Task.FromResult<object>(new
                {
                    fileName = file.FileName,
                    sheetName = worksheet.Name,
                    totalRows = dimension.End.Row - 1,
                    totalColumns = dimension.End.Column,
                    headers = headers,
                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing XLSX file {file.FileName}");
                throw;
            }
        }

        private Task<object> GetXlsPreviewAsync(IFormFile file, int sheetIndex = 0)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new HSSFWorkbook(stream);
                
                _logger.LogInformation($"Processing XLS file: {file.FileName}, Sheets: {workbook.NumberOfSheets}");
                
                if (workbook.NumberOfSheets == 0)
                    throw new ArgumentException("Excel dosyası boş veya geçersiz.");

                // Validate sheet index
                if (sheetIndex < 0 || sheetIndex >= workbook.NumberOfSheets)
                {
                    throw new ArgumentException($"Geçersiz sayfa indeksi: {sheetIndex}. Dosyada {workbook.NumberOfSheets} sayfa bulunmaktadır.");
                }

                var sheet = workbook.GetSheetAt(sheetIndex);
                var lastRowNum = sheet.LastRowNum;

                _logger.LogInformation($"Sheet: {sheet.SheetName}, LastRowNum: {lastRowNum}");

                // Handle empty sheets more gracefully
                if (lastRowNum < 0)
                {
                    _logger.LogWarning($"Sheet {sheet.SheetName} is empty (lastRowNum < 0)");
                    return Task.FromResult<object>(new 
                    { 
                        fileName = file.FileName,
                        sheetName = sheet.SheetName,
                        totalRows = 0,
                        totalColumns = 0,
                        headers = new List<string>(), 
                        data = new List<List<object>>() 
                    });
                }

                var headers = new List<string>();
                var data = new List<List<object>>();

                // Read headers (first row)
                var headerRow = sheet.GetRow(0);
                if (headerRow != null)
                {
                    for (int col = 0; col < headerRow.LastCellNum; col++)
                    {
                        var cell = headerRow.GetCell(col);
                        headers.Add(cell?.StringCellValue ?? $"Column{col + 1}");
                    }
                }

                _logger.LogInformation($"Found {headers.Count} headers: {string.Join(", ", headers)}");

                // Read data (first 10 rows)
                var maxRows = Math.Min(10, lastRowNum);
                for (int row = 1; row <= maxRows; row++)
                {
                    var sheetRow = sheet.GetRow(row);
                    if (sheetRow != null)
                    {
                        var rowData = new List<object>();
                        for (int col = 0; col < sheetRow.LastCellNum; col++)
                        {
                            var cell = sheetRow.GetCell(col);
                            rowData.Add(GetCellValue(cell));
                        }
                        data.Add(rowData);
                    }
                }

                _logger.LogInformation($"Read {data.Count} data rows");

                return Task.FromResult<object>(new
                {
                    fileName = file.FileName,
                    sheetName = sheet.SheetName,
                    totalRows = lastRowNum,
                    totalColumns = headers.Count,
                    headers = headers,
                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing XLS file {file.FileName}");
                throw;
            }
        }

        private object GetCellValue(ICell cell)
        {
            if (cell == null) return "";

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString("yyyy-MM-dd");
                    return cell.NumericCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Formula:
                    return cell.CellFormula;
                default:
                    return "";
            }
        }
    }
}
