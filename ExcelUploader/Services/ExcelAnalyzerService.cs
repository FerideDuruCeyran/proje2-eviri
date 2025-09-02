using ExcelUploader.Models;
using OfficeOpenXml;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Services
{
    public class ExcelAnalyzerService : IExcelAnalyzerService
    {
        private readonly ILogger<ExcelAnalyzerService> _logger;

        public ExcelAnalyzerService(ILogger<ExcelAnalyzerService> logger)
        {
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ExcelAnalysisResult> AnalyzeExcelFileAsync(IFormFile file)
        {
            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    return await AnalyzeXlsxFileAsync(file);
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    return await AnalyzeXlsFileAsync(file);
                }
                else
                {
                    throw new ArgumentException("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excel dosyası analiz edilirken hata oluştu: {file.FileName}");
                throw;
            }
        }

        public async Task<ExcelAnalysisResult> AnalyzeExcelFileAsync(IFormFile file, int sheetIndex = 0)
        {
            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    return await AnalyzeXlsxFileAsync(file, sheetIndex);
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    return await AnalyzeXlsFileAsync(file, sheetIndex);
                }
                else
                {
                    throw new ArgumentException("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excel dosyası analiz edilirken hata oluştu: {file.FileName}");
                throw;
            }
        }

        public async Task<List<string>> GetSheetNamesAsync(IFormFile file)
        {
            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    using var stream = file.OpenReadStream();
                    using var package = new ExcelPackage(stream);
                    return package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
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
                    return sheetNames;
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

        private async Task<ExcelAnalysisResult> AnalyzeXlsxFileAsync(IFormFile file, int sheetIndex = 0)
        {
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            
            if (package.Workbook.Worksheets.Count == 0)
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

            if (sheetIndex >= package.Workbook.Worksheets.Count)
                throw new ArgumentException($"Sayfa indeksi geçersiz. Dosyada {package.Workbook.Worksheets.Count} sayfa var.");

            var worksheet = package.Workbook.Worksheets[sheetIndex];

            if (worksheet == null)
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            if (rowCount == 0 || colCount == 0)
            {
                // Return empty result for empty sheets
                return new ExcelAnalysisResult
                {
                    FileName = file.FileName,
                    TotalRows = 0,
                    TotalColumns = 0,
                    Headers = new List<string>(),
                    ExcelColumnHeaders = new List<string>(),
                    DataTypes = new List<string>(),
                    DataTypeAnalysis = new List<ColumnDataTypeAnalysis>(),
                    SampleData = new List<Dictionary<string, object>>(),
                    AnalysisDate = DateTime.UtcNow,
                    SheetName = worksheet.Name,
                    SheetIndex = sheetIndex
                };
            }

            // Excel sütun başlıklarını oluştur (A, B, C, D, ...)
            var columnHeaders = GenerateExcelColumnHeaders(colCount);
            
            // İlk satırı oku (gerçek başlıklar varsa)
            var actualHeaders = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = GetCellValue(worksheet, 1, col);
                actualHeaders.Add(string.IsNullOrEmpty(headerValue) ? columnHeaders[col - 1] : headerValue);
            }

            // İlk 10 satırı oku ve veri tiplerini analiz et
            var sampleData = new List<Dictionary<string, object>>();
            var dataTypes = new List<string>();
            var dataTypeAnalysis = new List<ColumnDataTypeAnalysis>();

            // Start from row 2 (after header) and read up to 10 rows
            for (int row = 2; row <= Math.Min(11, rowCount); row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = GetCellValueWithType(worksheet, row, col);
                    rowData[actualHeaders[col - 1]] = cellValue;
                }
                sampleData.Add(rowData);
            }

            // Her sütun için veri tipi analizi yap
            for (int col = 0; col < actualHeaders.Count; col++)
            {
                var columnValues = sampleData.Select(r => r.Values.ElementAt(col)).ToList();
                var analysis = AnalyzeColumnDataType(actualHeaders[col], columnValues);
                dataTypes.Add(analysis.DetectedDataType);
                dataTypeAnalysis.Add(analysis);
            }

            var result = new ExcelAnalysisResult
            {
                FileName = file.FileName,
                TotalRows = rowCount - 1, // Header satırını çıkar
                TotalColumns = colCount,
                Headers = actualHeaders,
                ExcelColumnHeaders = columnHeaders,
                DataTypes = dataTypes,
                DataTypeAnalysis = dataTypeAnalysis,
                SampleData = sampleData,
                AnalysisDate = DateTime.UtcNow,
                SheetName = worksheet.Name,
                SheetIndex = sheetIndex
            };

            _logger.LogInformation($"Excel dosyası analiz edildi: {file.FileName}, Sayfa: {worksheet.Name}, {result.TotalRows} satır, {result.TotalColumns} sütun");
            return result;
        }

        private async Task<ExcelAnalysisResult> AnalyzeXlsFileAsync(IFormFile file, int sheetIndex = 0)
        {
            using var stream = file.OpenReadStream();
            using var workbook = new HSSFWorkbook(stream);
            
            if (workbook.NumberOfSheets == 0)
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

            if (sheetIndex >= workbook.NumberOfSheets)
                throw new ArgumentException($"Sayfa indeksi geçersiz. Dosyada {workbook.NumberOfSheets} sayfa var.");

            var sheet = workbook.GetSheetAt(sheetIndex);

            if (sheet == null)
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

            var rowCount = sheet.LastRowNum + 1; // LastRowNum is 0-based
            var headerRow = sheet.GetRow(0);
            var colCount = headerRow?.LastCellNum ?? 0;

            if (rowCount == 0 || colCount == 0)
            {
                // Return empty result for empty sheets
                return new ExcelAnalysisResult
                {
                    FileName = file.FileName,
                    TotalRows = 0,
                    TotalColumns = 0,
                    Headers = new List<string>(),
                    ExcelColumnHeaders = new List<string>(),
                    DataTypes = new List<string>(),
                    DataTypeAnalysis = new List<ColumnDataTypeAnalysis>(),
                    SampleData = new List<Dictionary<string, object>>(),
                    AnalysisDate = DateTime.UtcNow,
                    SheetName = sheet.SheetName,
                    SheetIndex = sheetIndex
                };
            }

            // Excel sütun başlıklarını oluştur (A, B, C, D, ...)
            var columnHeaders = GenerateExcelColumnHeaders(colCount);
            
            // İlk satırı oku (gerçek başlıklar varsa)
            var actualHeaders = new List<string>();
            if (headerRow != null)
            {
                for (int col = 0; col < colCount; col++)
                {
                    var cell = headerRow.GetCell(col);
                    var headerValue = GetNpoiCellValue(cell);
                    actualHeaders.Add(string.IsNullOrEmpty(headerValue) ? columnHeaders[col] : headerValue);
                }
            }

            // İlk 10 satırı oku ve veri tiplerini analiz et
            var sampleData = new List<Dictionary<string, object>>();
            var dataTypes = new List<string>();
            var dataTypeAnalysis = new List<ColumnDataTypeAnalysis>();

            // Start from row 1 (after header) and read up to 10 rows
            for (int row = 1; row <= Math.Min(10, rowCount - 1); row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow == null) continue;

                var rowData = new Dictionary<string, object>();
                for (int col = 0; col < actualHeaders.Count; col++)
                {
                    var cell = sheetRow.GetCell(col);
                    var cellValue = GetNpoiCellValueWithType(cell);
                    rowData[actualHeaders[col]] = cellValue;
                }
                sampleData.Add(rowData);
            }

            // Her sütun için veri tipi analizi yap
            for (int col = 0; col < actualHeaders.Count; col++)
            {
                var columnValues = sampleData.Select(r => r.Values.ElementAt(col)).ToList();
                var analysis = AnalyzeColumnDataType(actualHeaders[col], columnValues);
                dataTypes.Add(analysis.DetectedDataType);
                dataTypeAnalysis.Add(analysis);
            }

            var result = new ExcelAnalysisResult
            {
                FileName = file.FileName,
                TotalRows = rowCount - 1, // Exclude header
                TotalColumns = colCount,
                Headers = actualHeaders,
                ExcelColumnHeaders = columnHeaders,
                DataTypes = dataTypes,
                DataTypeAnalysis = dataTypeAnalysis,
                SampleData = sampleData,
                AnalysisDate = DateTime.UtcNow,
                SheetName = sheet.SheetName,
                SheetIndex = sheetIndex
            };

            _logger.LogInformation($"Excel dosyası analiz edildi: {file.FileName}, Sayfa: {sheet.SheetName}, {result.TotalRows} satır, {result.TotalColumns} sütun");
            return result;
        }

        private List<string> GenerateExcelColumnHeaders(int columnCount)
        {
            var headers = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                var columnLetter = GetExcelColumnLetter(i);
                headers.Add(columnLetter); // A, B, C, D, E, F, ...
            }
            return headers;
        }

        private string GetExcelColumnLetter(int columnIndex)
        {
            var result = "";
            while (columnIndex >= 0)
            {
                result = (char)('A' + (columnIndex % 26)) + result;
                columnIndex = columnIndex / 26 - 1;
            }
            return result;
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

            // Veri tipi sayacı
            int intCount = 0, decimalCount = 0, dateCount = 0, boolCount = 0, stringCount = 0;
            int totalCount = nonNullValues.Count;

            foreach (var value in nonNullValues)
            {
                if (value is DateTime)
                    dateCount++;
                else if (value is bool)
                    boolCount++;
                else if (value is int || value is long)
                    intCount++;
                else if (value is decimal || value is double || value is float)
                    decimalCount++;
                else if (value is string strValue)
                {
                    // String değerleri parse etmeye çalış
                    if (DateTime.TryParse(strValue, out _))
                        dateCount++;
                    else if (int.TryParse(strValue, out _))
                        intCount++;
                    else if (decimal.TryParse(strValue, out _))
                        decimalCount++;
                    else if (bool.TryParse(strValue, out _))
                        boolCount++;
                    else
                        stringCount++;
                }
                else
                    stringCount++;
            }

            // En yaygın veri tipini belirle
            var maxCount = Math.Max(Math.Max(Math.Max(Math.Max(intCount, decimalCount), dateCount), boolCount), stringCount);
            
            if (dateCount == maxCount && dateCount > totalCount * 0.3)
            {
                analysis.DetectedDataType = "datetime2";
                analysis.Confidence = (double)dateCount / totalCount;
            }
            else if (boolCount == maxCount && boolCount > totalCount * 0.3)
            {
                analysis.DetectedDataType = "bit";
                analysis.Confidence = (double)boolCount / totalCount;
            }
            else if (intCount == maxCount && intCount > totalCount * 0.3)
            {
                analysis.DetectedDataType = "int";
                analysis.Confidence = (double)intCount / totalCount;
            }
            else if (decimalCount == maxCount && decimalCount > totalCount * 0.3)
            {
                analysis.DetectedDataType = "decimal(18,2)";
                analysis.Confidence = (double)decimalCount / totalCount;
            }
            else if (stringCount == maxCount)
            {
                // String uzunluğunu kontrol et
                var maxLength = nonNullValues.OfType<string>().Max(s => s?.Length ?? 0);
                if (maxLength > 255)
                {
                    analysis.DetectedDataType = "nvarchar(max)";
                }
                else
                {
                    analysis.DetectedDataType = $"nvarchar({Math.Max(255, maxLength)})";
                }
                analysis.Confidence = (double)stringCount / totalCount;
            }
            else
            {
                // Belirgin bir çoğunluk yoksa, sütun adına göre tahmin et
                analysis.DetectedDataType = DetermineDataTypeByColumnName(columnName, values);
                analysis.Confidence = 0.5;
            }

            // İstatistikleri hesapla
            analysis.IntCount = intCount;
            analysis.DecimalCount = decimalCount;
            analysis.DateCount = dateCount;
            analysis.BoolCount = boolCount;
            analysis.StringCount = stringCount;

            return analysis;
        }

        private string DetermineDataTypeByColumnName(string columnName, List<object> values)
        {
            if (string.IsNullOrEmpty(columnName)) return "nvarchar(255)";

            var lowerColumnName = columnName.ToLowerInvariant();
            
            // Tarih ile ilgili sütun adları
            if (lowerColumnName.Contains("tarih") || lowerColumnName.Contains("date") || 
                lowerColumnName.Contains("zaman") || lowerColumnName.Contains("time") ||
                lowerColumnName.Contains("başlangıç") || lowerColumnName.Contains("bitiş") ||
                lowerColumnName.Contains("start") || lowerColumnName.Contains("end") ||
                lowerColumnName.Contains("doğum") || lowerColumnName.Contains("birth") ||
                lowerColumnName.Contains("ödeme") && lowerColumnName.Contains("tarih"))
            {
                return "datetime2";
            }

            // Para/tutar ile ilgili sütun adları
            if (lowerColumnName.Contains("tutar") || lowerColumnName.Contains("amount") ||
                lowerColumnName.Contains("fiyat") || lowerColumnName.Contains("price") ||
                lowerColumnName.Contains("ücret") || lowerColumnName.Contains("fee") ||
                lowerColumnName.Contains("maliyet") || lowerColumnName.Contains("cost") ||
                lowerColumnName.Contains("öde") || lowerColumnName.Contains("pay") ||
                lowerColumnName.Contains("para") || lowerColumnName.Contains("money") ||
                lowerColumnName.Contains("oran") || lowerColumnName.Contains("rate") ||
                lowerColumnName.Contains("yüzde") || lowerColumnName.Contains("percent") ||
                lowerColumnName.Contains("odenecek") || lowerColumnName.Contains("odendiginde"))
            {
                return "decimal(18,2)";
            }

            // Sayı ile ilgili sütun adları
            if (lowerColumnName.Contains("numara") || lowerColumnName.Contains("no") ||
                lowerColumnName.Contains("id") || lowerColumnName.Contains("kod") ||
                lowerColumnName.Contains("code") || lowerColumnName.Contains("sıra") ||
                lowerColumnName.Contains("order") || lowerColumnName.Contains("index") ||
                lowerColumnName.Contains("yıl") || lowerColumnName.Contains("year"))
            {
                return "int";
            }

            // Boolean ile ilgili sütun adları
            if (lowerColumnName.Contains("aktif") || lowerColumnName.Contains("active") ||
                lowerColumnName.Contains("pasif") || lowerColumnName.Contains("passive") ||
                lowerColumnName.Contains("evet") || lowerColumnName.Contains("hayır") ||
                lowerColumnName.Contains("yes") || lowerColumnName.Contains("no") ||
                lowerColumnName.Contains("var") || lowerColumnName.Contains("yok") ||
                lowerColumnName.Contains("true") || lowerColumnName.Contains("false"))
            {
                return "bit";
            }

            // Uzun metin sütun adları
            if (lowerColumnName.Contains("açıklama") || lowerColumnName.Contains("description") ||
                lowerColumnName.Contains("detay") || lowerColumnName.Contains("detail") ||
                lowerColumnName.Contains("not") || lowerColumnName.Contains("comment") ||
                lowerColumnName.Contains("yorum") || lowerColumnName.Contains("adres") ||
                lowerColumnName.Contains("address") || lowerColumnName.Contains("içerik") ||
                lowerColumnName.Contains("content"))
            {
                return "nvarchar(max)";
            }

            return "nvarchar(255)";
        }

        private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                return cell?.Value?.ToString()?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private object GetCellValueWithType(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                if (cell?.Value == null) return string.Empty;
                return cell.Value;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetNpoiCellValue(ICell? cell)
        {
            if (cell == null) return string.Empty;
            
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue ?? string.Empty;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString();
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    return cell.StringCellValue ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private object GetNpoiCellValueWithType(ICell? cell)
        {
            if (cell == null) return string.Empty;
            
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue ?? string.Empty;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue;
                    return cell.NumericCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Formula:
                    try
                    {
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                                return cell.StringCellValue ?? string.Empty;
                            case CellType.Numeric:
                                if (DateUtil.IsCellDateFormatted(cell))
                                    return cell.DateCellValue;
                                return cell.NumericCellValue;
                            case CellType.Boolean:
                                return cell.BooleanCellValue;
                            default:
                                return cell.StringCellValue ?? string.Empty;
                        }
                    }
                    catch
                    {
                        return cell.StringCellValue ?? string.Empty;
                    }
                default:
                    return string.Empty;
            }
        }
    }

    public class ExcelAnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public List<string> Headers { get; set; } = new List<string>();
        public List<string> ExcelColumnHeaders { get; set; } = new List<string>();
        public List<string> DataTypes { get; set; } = new List<string>();
        public List<ColumnDataTypeAnalysis> DataTypeAnalysis { get; set; } = new List<ColumnDataTypeAnalysis>();
        public List<Dictionary<string, object>> SampleData { get; set; } = new List<Dictionary<string, object>>();
        public DateTime AnalysisDate { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public int SheetIndex { get; set; }
    }

    public class ColumnDataTypeAnalysis
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DetectedDataType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int TotalValues { get; set; }
        public int NonNullValues { get; set; }
        public int NullValues { get; set; }
        public int IntCount { get; set; }
        public int DecimalCount { get; set; }
        public int DateCount { get; set; }
        public int BoolCount { get; set; }
        public int StringCount { get; set; }
    }
}
