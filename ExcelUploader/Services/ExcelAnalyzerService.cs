using OfficeOpenXml;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using ExcelUploader.Models;
using Microsoft.AspNetCore.Http;

namespace ExcelUploader.Services
{
    public class ExcelAnalyzerService : IExcelAnalyzerService
    {
        private readonly ILogger<ExcelAnalyzerService> _logger;

        public ExcelAnalyzerService(ILogger<ExcelAnalyzerService> logger)
        {
            _logger = logger;
        }

        public async Task<ExcelAnalysisResult> AnalyzeExcelFileAsync(IFormFile file)
        {
            return await AnalyzeExcelFileAsync(file, 0);
        }

        public async Task<ExcelAnalysisResult> AnalyzeExcelFileAsync(IFormFile file, int sheetIndex)
        {
            try
            {
                _logger.LogInformation("Starting Excel analysis for file: {FileName}, sheet: {SheetIndex}", file.FileName, sheetIndex);

                using var stream = file.OpenReadStream();
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (fileExtension == ".xlsx")
                {
                    return await AnalyzeXlsxFileAsync(stream, sheetIndex);
                }
                else if (fileExtension == ".xls")
                {
                    return await AnalyzeXlsFileAsync(stream, sheetIndex);
                }
                else
                {
                    return ExcelAnalysisResult.Failure("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing Excel file: {FileName}", file.FileName);
                return ExcelAnalysisResult.Failure($"Excel dosyası analiz edilirken hata oluştu: {ex.Message}");
            }
        }

        public async Task<List<string>> GetSheetNamesAsync(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var sheetNames = new List<string>();

                if (fileExtension == ".xlsx")
                {
                    using var package = new ExcelPackage(stream);
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        sheetNames.Add(worksheet.Name);
                    }
                }
                else if (fileExtension == ".xls")
                {
                    IWorkbook workbook;
                    if (stream.CanSeek)
                    {
                        stream.Position = 0;
                        workbook = new HSSFWorkbook(stream);
                    }
                    else
                    {
                        using var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        workbook = new HSSFWorkbook(memoryStream);
                    }

                    for (int i = 0; i < workbook.NumberOfSheets; i++)
                    {
                        sheetNames.Add(workbook.GetSheetName(i));
                    }
                }

                return sheetNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sheet names for file: {FileName}", file.FileName);
                return new List<string>();
            }
        }

        private async Task<ExcelAnalysisResult> AnalyzeXlsxFileAsync(Stream stream, int sheetIndex)
        {
            try
            {
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[sheetIndex];
                
                if (worksheet == null)
                {
                    return ExcelAnalysisResult.Failure("Excel dosyasında belirtilen çalışma sayfası bulunamadı.");
                }

                var headers = new List<string>();
                var rows = new List<List<object>>();
                var columnDataTypes = new List<ColumnDataTypeAnalysis>();

                // Read headers (first row)
                var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column];
                foreach (var cell in headerRow)
                {
                    headers.Add(cell.Text?.Trim() ?? $"Column{cell.Start.Column}");
                }

                _logger.LogInformation("Found {HeaderCount} headers", headers.Count);

                // Read data rows
                var dataRows = worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];
                var rowData = new List<List<object>>();

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var rowValues = new List<object>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var cell = worksheet.Cells[row, col];
                        rowValues.Add(GetCellValue(cell));
                    }
                    rowData.Add(rowValues);
                }

                _logger.LogInformation("Found {RowCount} data rows", rowData.Count);

                // Analyze data types for each column
                for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                {
                    var columnValues = rowData.Select(row => row[colIndex]).ToList();
                    var analysis = AnalyzeColumnDataType(headers[colIndex], columnValues);
                    columnDataTypes.Add(analysis);
                }

                return ExcelAnalysisResult.Success(headers, rowData, columnDataTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing XLSX file");
                return ExcelAnalysisResult.Failure($"XLSX dosyası analiz edilirken hata oluştu: {ex.Message}");
            }
        }

        private async Task<ExcelAnalysisResult> AnalyzeXlsFileAsync(Stream stream, int sheetIndex)
        {
            try
            {
                IWorkbook workbook;
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                    workbook = new HSSFWorkbook(stream);
                }
                else
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    workbook = new HSSFWorkbook(memoryStream);
                }

                var sheet = workbook.GetSheetAt(sheetIndex);
                if (sheet == null)
                {
                    return ExcelAnalysisResult.Failure("Excel dosyasında belirtilen çalışma sayfası bulunamadı.");
                }

                var headers = new List<string>();
                var rows = new List<List<object>>();
                var columnDataTypes = new List<ColumnDataTypeAnalysis>();

                // Read headers (first row)
                var headerRow = sheet.GetRow(0);
                if (headerRow != null)
                {
                    for (int col = 0; col < headerRow.LastCellNum; col++)
                    {
                        var cell = headerRow.GetCell(col);
                        headers.Add(GetNpoiCellValue(cell)?.ToString()?.Trim() ?? $"Column{col + 1}");
                    }
                }

                _logger.LogInformation("Found {HeaderCount} headers", headers.Count);

                // Read data rows
                var rowData = new List<List<object>>();
                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row != null)
                    {
                        var rowValues = new List<object>();
                        for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                        {
                            var cell = row.GetCell(colIndex);
                            rowValues.Add(GetNpoiCellValue(cell));
                        }
                        rowData.Add(rowValues);
                    }
                }

                _logger.LogInformation("Found {RowCount} data rows", rowData.Count);

                // Analyze data types for each column
                for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                {
                    var columnValues = rowData.Select(row => row[colIndex]).ToList();
                    var analysis = AnalyzeColumnDataType(headers[colIndex], columnValues);
                    columnDataTypes.Add(analysis);
                }

                return ExcelAnalysisResult.Success(headers, rowData, columnDataTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing XLS file");
                return ExcelAnalysisResult.Failure($"XLS dosyası analiz edilirken hata oluştu: {ex.Message}");
            }
        }

        private object GetCellValue(OfficeOpenXml.ExcelRange cell)
        {
            if (cell == null) return null;

            try
            {
                switch (cell.Value)
                {
                    case null:
                        return null;
                    case string str:
                        return str.Trim();
                    case DateTime dt:
                        return dt;
                    case double d:
                        return d;
                    case int i:
                        return i;
                    case long l:
                        return l;
                    case decimal dec:
                        return dec;
                    case bool b:
                        return b;
                    default:
                        return cell.Value?.ToString()?.Trim();
                }
            }
            catch
            {
                return cell.Text?.Trim();
            }
        }

        private object GetNpoiCellValue(ICell cell)
        {
            if (cell == null) return null;

            try
            {
                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue?.Trim();
                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            return cell.DateCellValue;
                        }
                        return cell.NumericCellValue;
                    case CellType.Boolean:
                        return cell.BooleanCellValue;
                    case CellType.Formula:
                        return GetNpoiCellValue(cell.CachedFormulaResultType);
                    case CellType.Blank:
                        return null;
                    default:
                        return cell.ToString()?.Trim();
                }
            }
            catch
            {
                return cell.ToString()?.Trim();
            }
        }

        private object GetNpoiCellValue(CellType cellType)
        {
            return cellType switch
            {
                CellType.String => "",
                CellType.Numeric => 0.0,
                CellType.Boolean => false,
                _ => null
            };
        }

        private ColumnDataTypeAnalysis AnalyzeColumnDataType(string columnName, List<object> values)
        {
            var analysis = new ColumnDataTypeAnalysis
            {
                ColumnName = columnName,
                TotalValues = values.Count,
                NonNullValues = values.Where(v => v != null && !string.IsNullOrEmpty(v.ToString())).Count(),
                NullValues = values.Where(v => v == null || string.IsNullOrEmpty(v.ToString())).Count()
            };

            var nonNullValues = values.Where(v => v != null && !string.IsNullOrEmpty(v.ToString())).ToList();
            
            if (!nonNullValues.Any())
            {
                analysis.DetectedDataType = "nvarchar(255)";
                analysis.Confidence = 0.0;
                return analysis;
            }

            // Data type counters
            int intCount = 0, decimalCount = 0, dateCount = 0, boolCount = 0, stringCount = 0;
            int maxStringLength = 0;

            foreach (var value in nonNullValues)
            {
                var stringValue = value.ToString();
                
                // Check for boolean
                if (bool.TryParse(stringValue, out _))
                {
                    boolCount++;
                    continue;
                }

                // Check for date
                if (DateTime.TryParse(stringValue, out _))
                {
                    dateCount++;
                    continue;
                }

                // Check for integer
                if (int.TryParse(stringValue, out _))
                {
                    intCount++;
                    continue;
                }

                // Check for decimal
                if (decimal.TryParse(stringValue, out _))
                {
                    decimalCount++;
                    continue;
                }

                // Must be string
                stringCount++;
                maxStringLength = Math.Max(maxStringLength, stringValue.Length);
            }

            // Determine data type based on majority
            var totalNonNull = nonNullValues.Count;
            var intRatio = (double)intCount / totalNonNull;
            var decimalRatio = (double)decimalCount / totalNonNull;
            var dateRatio = (double)dateCount / totalNonNull;
            var boolRatio = (double)boolCount / totalNonNull;
            var stringRatio = (double)stringCount / totalNonNull;

            // Set confidence and data type
            if (boolRatio > 0.8)
            {
                analysis.DetectedDataType = "bit";
                analysis.Confidence = boolRatio;
            }
            else if (dateRatio > 0.8)
            {
                analysis.DetectedDataType = "datetime2";
                analysis.Confidence = dateRatio;
            }
            else if (intRatio > 0.8)
            {
                analysis.DetectedDataType = "int";
                analysis.Confidence = intRatio;
            }
            else if (decimalRatio > 0.8)
            {
                analysis.DetectedDataType = "decimal(18,2)";
                analysis.Confidence = decimalRatio;
            }
            else
            {
                // String type - determine length
                var stringLength = maxStringLength;
                if (stringLength <= 50)
                {
                    analysis.DetectedDataType = $"nvarchar({Math.Max(stringLength, 10)})";
                }
                else if (stringLength <= 255)
                {
                    analysis.DetectedDataType = "nvarchar(255)";
                }
                else if (stringLength <= 1000)
                {
                    analysis.DetectedDataType = "nvarchar(1000)";
                }
                else
                {
                    analysis.DetectedDataType = "nvarchar(max)";
                }
                analysis.Confidence = stringRatio;
            }

            _logger.LogInformation("Column {ColumnName}: Type={DataType}, Confidence={Confidence}, Length={Length}", 
                columnName, analysis.DetectedDataType, analysis.Confidence, maxStringLength);

            return analysis;
        }
    }

    public class ExcelAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Headers { get; set; }
        public List<List<object>> Rows { get; set; }
        public List<ColumnDataTypeAnalysis> ColumnDataTypes { get; set; }

        public static ExcelAnalysisResult Success(List<string> headers, List<List<object>> rows, List<ColumnDataTypeAnalysis> columnDataTypes)
        {
            return new ExcelAnalysisResult
            {
                IsSuccess = true,
                Headers = headers,
                Rows = rows,
                ColumnDataTypes = columnDataTypes
            };
        }

        public static ExcelAnalysisResult Failure(string errorMessage)
        {
            return new ExcelAnalysisResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public class ColumnDataTypeAnalysis
    {
        public string ColumnName { get; set; }
        public string DetectedDataType { get; set; }
        public double Confidence { get; set; }
        public int TotalValues { get; set; }
        public int NonNullValues { get; set; }
        public int NullValues { get; set; }
    }
}
