using ExcelUploader.Models;
using OfficeOpenXml;
using ClosedXML.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Globalization;
using System.ComponentModel.DataAnnotations;

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



        private Task<object> GetXlsPreviewAsync(IFormFile file, int sheetIndex = 0)
        {
            try
            {
                using var stream = file.OpenReadStream();
                
                // For .xls files, use HSSFWorkbook (NPOI)
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
                int colCount = headerRow?.LastCellNum ?? 0;

                _logger.LogInformation($"Excel preview (.xls): {file.FileName}, Sheet: {sheet.SheetName}, Initial Rows: {rowCount}, Columns: {colCount}");

                // If no data detected, try to scan for actual data
                if (rowCount == 0 || colCount == 0)
                {
                    var actualData = ScanForDataXls(sheet);
                    rowCount = actualData.rowCount;
                    colCount = actualData.colCount;
                    
                    _logger.LogInformation($"After scanning (.xls): Rows: {rowCount}, Columns: {colCount}");
                    
                    if (rowCount == 0 || colCount == 0)
                    {
                        // Return empty result for empty sheets
                        var emptyResult = new
                        {
                            headers = new List<string>(),
                            rows = new List<List<string>>(),
                            dataTypes = new List<string>(),
                            totalRows = 0,
                            totalColumns = 0,
                            sheetName = sheet.SheetName,
                            sheetIndex = sheetIndex
                        };
                        return Task.FromResult<object>(emptyResult);
                    }
                }

                // Get headers (first row)
                var headers = new List<string>();
                if (headerRow != null)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        var cell = headerRow.GetCell(col);
                        var headerValue = GetNpoiCellValue(cell);
                        headers.Add(string.IsNullOrEmpty(headerValue) ? $"Sütun_{col + 1}" : headerValue);
                    }
                }

                _logger.LogInformation($"Headers found (.xls): {string.Join(", ", headers)}");

                // Get first 10 rows of data (excluding header)
                var rows = new List<List<string>>();
                var dataTypes = new List<string>();
                var sampleData = new List<Dictionary<string, object>>();

                for (int row = 1; row <= Math.Min(10, rowCount - 1); row++)
                {
                    var sheetRow = sheet.GetRow(row);
                    if (sheetRow == null) continue;

                    var rowData = new List<string>();
                    var rowDict = new Dictionary<string, object>();
                    
                    for (int col = 0; col < headers.Count; col++)
                    {
                        var cell = sheetRow.GetCell(col);
                        var cellValue = GetNpoiCellValue(cell);
                        var displayValue = cellValue ?? "";
                        rowData.Add(displayValue);
                        
                        // Store the actual cell value with proper type for analysis
                        var actualValue = GetNpoiCellValueWithType(cell);
                        rowDict[headers[col]] = actualValue;
                    }
                    rows.Add(rowData);
                    sampleData.Add(rowDict);
                }

                _logger.LogInformation($"Data rows collected (.xls): {rows.Count}");

                // Determine data types based on sample data
                for (int col = 0; col < headers.Count; col++)
                {
                    var columnValues = sampleData.Select(r => r.Values.ElementAt(col)).ToList();
                    var dataType = DetermineDataTypeByColumnName(headers[col], columnValues);
                    dataTypes.Add(dataType);
                }

                var result = new
                {
                    headers = headers,
                    rows = rows,
                    dataTypes = dataTypes,
                    totalRows = rowCount - 1, // Total rows excluding header
                    totalColumns = headers.Count,
                    sheetName = sheet.SheetName,
                    sheetIndex = sheetIndex
                };

                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetXlsPreviewAsync for file: {FileName}", file.FileName);
                
                // Return a more user-friendly error
                var errorResult = new
                {
                    error = true,
                    message = $"Excel dosyası okunamadı: {ex.Message}",
                    headers = new List<string>(),
                    rows = new List<List<string>>(),
                    dataTypes = new List<string>(),
                    totalRows = 0,
                    totalColumns = 0,
                    sheetName = "",
                    sheetIndex = sheetIndex
                };
                
                return Task.FromResult<object>(errorResult);
            }
        }

        private (int rowCount, int colCount) ScanForDataXls(ISheet sheet)
        {
            int lastRow = 0;
            int lastCol = 0;
            
            // Scan up to 1000 rows and 100 columns
            for (int row = 0; row <= 1000; row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow == null) continue;
                
                bool rowHasData = false;
                for (int col = 0; col < 100; col++)
                {
                    var cell = sheetRow.GetCell(col);
                    if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
                    {
                        rowHasData = true;
                        lastCol = Math.Max(lastCol, (int)(col + 1));
                    }
                }
                if (rowHasData)
                {
                    lastRow = row + 1; // Convert to 1-based
                }
                else if (row > 10 && lastRow > 0)
                {
                    // If we've found data and then hit empty rows, we can stop
                    break;
                }
            }
            
            return (lastRow, lastCol);
        }

        private Task<object> GetXlsxPreviewAsync(IFormFile file, int sheetIndex = 0)
        {
            try
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

                _logger.LogInformation($"Excel preview: {file.FileName}, Sheet: {worksheet.Name}, Initial Rows: {rowCount}, Columns: {colCount}");

                // If Dimension is null or shows 0, try to find data by scanning
                if (rowCount == 0 || colCount == 0)
                {
                    // Scan for actual data
                    var actualData = ScanForData(worksheet);
                    rowCount = actualData.rowCount;
                    colCount = actualData.colCount;
                    
                    _logger.LogInformation($"After scanning: Rows: {rowCount}, Columns: {colCount}");
                    
                    if (rowCount == 0 || colCount == 0)
                    {
                        // Return empty result for empty sheets
                        var emptyResult = new
                        {
                            headers = new List<string>(),
                            rows = new List<List<string>>(),
                            dataTypes = new List<string>(),
                            totalRows = 0,
                            totalColumns = 0,
                            sheetName = worksheet.Name,
                            sheetIndex = sheetIndex
                        };
                        return Task.FromResult<object>(emptyResult);
                    }
                }

                // Get headers (first row)
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = GetCellValue(worksheet, 1, col);
                    headers.Add(string.IsNullOrEmpty(headerValue) ? $"Sütun_{col}" : headerValue);
                }

                _logger.LogInformation($"Headers found: {string.Join(", ", headers)}");

                // Get first 10 rows of data (excluding header)
                var rows = new List<List<string>>();
                var dataTypes = new List<string>();
                var sampleData = new List<Dictionary<string, object>>();

                for (int row = 2; row <= Math.Min(11, rowCount); row++)
                {
                    var rowData = new List<string>();
                    var rowDict = new Dictionary<string, object>();
                    
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = GetCellValue(worksheet, row, col);
                        var displayValue = cellValue ?? "";
                        rowData.Add(displayValue);
                        
                        // Store the actual cell value with proper type for analysis
                        var actualValue = GetCellValueWithType(worksheet, row, col);
                        rowDict[headers[col - 1]] = actualValue;
                    }
                    rows.Add(rowData);
                    sampleData.Add(rowDict);
                }

                _logger.LogInformation($"Data rows collected: {rows.Count}");

                // Determine data types based on sample data
                for (int col = 0; col < headers.Count; col++)
                {
                    var columnValues = sampleData.Select(r => r.Values.ElementAt(col)).ToList();
                    var dataType = DetermineDataTypeByColumnName(headers[col], columnValues);
                    dataTypes.Add(dataType);
                }

                var result = new
                {
                    headers = headers,
                    rows = rows,
                    dataTypes = dataTypes,
                    totalRows = rowCount - 1, // Exclude header
                    totalColumns = colCount,
                    sheetName = worksheet.Name,
                    sheetIndex = sheetIndex
                };

                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetXlsxPreviewAsync");
                throw;
            }
        }

        private (int rowCount, int colCount) ScanForData(ExcelWorksheet worksheet)
        {
            // Scan from bottom up to find the last row with data
            int lastRow = 0;
            int lastCol = 0;
            
            // Scan up to 1000 rows and 100 columns
            for (int row = 1; row <= 1000; row++)
            {
                bool rowHasData = false;
                for (int col = 1; col <= 100; col++)
                {
                    try
                    {
                        var cell = worksheet.Cells[row, col];
                        if (cell?.Value != null && !string.IsNullOrWhiteSpace(cell.Value.ToString()))
                        {
                            rowHasData = true;
                            lastCol = Math.Max(lastCol, col);
                        }
                    }
                    catch
                    {
                        // Cell doesn't exist, continue
                    }
                }
                if (rowHasData)
                {
                    lastRow = row;
                }
                else if (row > 10 && lastRow > 0)
                {
                    // If we've found data and then hit empty rows, we can stop
                    break;
                }
            }
            
            return (lastRow, lastCol);
        }

        private string DetermineDataTypeImproved(List<object> values)
        {
            var nonNullValues = values.Where(v => v != null && !string.IsNullOrEmpty(v.ToString())).ToList();
            if (!nonNullValues.Any()) return "nvarchar(255)";

            // Count different types in the column
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
                    // Try to parse string values
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

            // Determine the most common type
            if (dateCount > totalCount * 0.5) return "datetime2";
            if (boolCount > totalCount * 0.5) return "bit";
            if (intCount > totalCount * 0.5) return "int";
            if (decimalCount > totalCount * 0.5) return "decimal(18,2)";
            if (stringCount > totalCount * 0.5)
            {
                // Check if any string is very long
                var maxLength = nonNullValues.OfType<string>().Max(s => s?.Length ?? 0);
                if (maxLength > 255) return "nvarchar(max)";
                return "nvarchar(255)";
            }

            // If no clear majority, use the most specific type found
            if (dateCount > 0) return "datetime2";
            if (boolCount > 0) return "bit";
            if (intCount > 0) return "int";
            if (decimalCount > 0) return "decimal(18,2)";
            
            return "nvarchar(255)";
        }

        private string DetermineDataTypeByColumnName(string columnName, List<object> values)
        {
            if (string.IsNullOrEmpty(columnName)) return DetermineDataTypeImproved(values);

            var lowerColumnName = columnName.ToLowerInvariant();
            
            // Date-related column names - HIGH PRIORITY
            if (lowerColumnName.Contains("tarih") || lowerColumnName.Contains("date") || 
                lowerColumnName.Contains("zaman") || lowerColumnName.Contains("time") ||
                lowerColumnName.Contains("başlangıç") || lowerColumnName.Contains("bitiş") ||
                lowerColumnName.Contains("start") || lowerColumnName.Contains("end") ||
                lowerColumnName.Contains("doğum") || lowerColumnName.Contains("birth") ||
                lowerColumnName.Contains("ödeme") && lowerColumnName.Contains("tarih"))
            {
                return "datetime2";
            }

            // Money/amount-related column names - HIGH PRIORITY
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

            // Number-related column names
            if (lowerColumnName.Contains("numara") || lowerColumnName.Contains("no") ||
                lowerColumnName.Contains("id") || lowerColumnName.Contains("kod") ||
                lowerColumnName.Contains("code") || lowerColumnName.Contains("sıra") ||
                lowerColumnName.Contains("order") || lowerColumnName.Contains("index") ||
                lowerColumnName.Contains("yıl") || lowerColumnName.Contains("year"))
            {
                return "int";
            }

            // Boolean-related column names
            if (lowerColumnName.Contains("aktif") || lowerColumnName.Contains("active") ||
                lowerColumnName.Contains("pasif") || lowerColumnName.Contains("passive") ||
                lowerColumnName.Contains("evet") || lowerColumnName.Contains("hayır") ||
                lowerColumnName.Contains("yes") || lowerColumnName.Contains("no") ||
                lowerColumnName.Contains("var") || lowerColumnName.Contains("yok") ||
                lowerColumnName.Contains("true") || lowerColumnName.Contains("false"))
            {
                return "bit";
            }

            // Long text column names
            if (lowerColumnName.Contains("açıklama") || lowerColumnName.Contains("description") ||
                lowerColumnName.Contains("detay") || lowerColumnName.Contains("detail") ||
                lowerColumnName.Contains("not") || lowerColumnName.Contains("comment") ||
                lowerColumnName.Contains("yorum") || lowerColumnName.Contains("adres") ||
                lowerColumnName.Contains("address") || lowerColumnName.Contains("içerik") ||
                lowerColumnName.Contains("content"))
            {
                return "nvarchar(max)";
            }

            // Try content-based detection as fallback
            var contentBasedType = DetermineDataTypeImproved(values);
            
            // If content-based detection suggests varchar, try to be more specific
            if (contentBasedType == "nvarchar(255)")
            {
                // Check if any values look like numbers
                var hasNumbers = values.Any(v => 
                    v != null && 
                    decimal.TryParse(v.ToString(), out _) && 
                    !DateTime.TryParse(v.ToString(), out _));
                
                if (hasNumbers)
                {
                    return "decimal(18,2)";
                }
            }
            
            return contentBasedType;
        }

        public async Task<List<ExcelData>> ProcessExcelFileAsync(IFormFile file, string uploadedBy)
        {
            try
            {
                var data = new List<ExcelData>();
                
                if (file.FileName.EndsWith(".xlsx"))
                {
                    data = await ProcessXlsxFileAsync(file, uploadedBy);
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    data = await ProcessXlsFileAsync(file, uploadedBy);
                }
                else
                {
                    throw new ArgumentException("Desteklenmeyen dosya formatı. Sadece .xlsx ve .xls dosyaları desteklenir.");
                }

                _logger.LogInformation($"Excel dosyası başarıyla işlendi: {file.FileName}, {data.Count} satır");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excel dosyası işlenirken hata oluştu: {file.FileName}");
                throw;
            }
        }

        private Task<List<ExcelData>> ProcessXlsxFileAsync(IFormFile file, string uploadedBy)
        {
            var data = new List<ExcelData>();
            
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets[0];

            if (worksheet == null)
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            _logger.LogInformation($"Processing XLSX file: {file.FileName}, Total rows: {rowCount}, Total columns: {colCount}");

            // Basit yaklaşım: İlk satırdan başla ve tüm verileri oku
            for (int row = 1; row <= rowCount; row++)
            {
                var excelData = new ExcelData
                {
                    FileName = file.FileName,
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = uploadedBy,
                    RowNumber = row,
                    IsProcessed = false
                };

                // Her satırdan tüm hücreleri oku
                var rowValues = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = GetCellValue(worksheet, row, col);
                    rowValues.Add(cellValue ?? "");
                }

                _logger.LogInformation($"Row {row} raw values: {string.Join(" | ", rowValues)}");

                // Pozisyon bazlı mapping - kesin çalışır
                if (rowValues.Count > 0) excelData.Ad = rowValues[0];
                if (rowValues.Count > 1) excelData.Soyad = rowValues[1];
                if (rowValues.Count > 2) excelData.TCKimlikNo = rowValues[2];
                if (rowValues.Count > 3) excelData.OgrenciNo = rowValues[3];
                if (rowValues.Count > 4) excelData.DogumTarihi = GetDateValue(worksheet, row, 5);
                if (rowValues.Count > 5) excelData.DogumYeri = rowValues[5];
                if (rowValues.Count > 6) excelData.Cinsiyet = rowValues[6];
                if (rowValues.Count > 7) excelData.Odenecek = GetDecimalValue(worksheet, row, 8);
                if (rowValues.Count > 8) excelData.OdemeTarihi = GetDateValue(worksheet, row, 9);
                if (rowValues.Count > 9) excelData.Aciklama = rowValues[9];

                // Herhangi bir veri varsa ekle
                var hasAnyData = !string.IsNullOrWhiteSpace(excelData.Ad) || !string.IsNullOrWhiteSpace(excelData.Soyad) || 
                    !string.IsNullOrWhiteSpace(excelData.TCKimlikNo) || !string.IsNullOrWhiteSpace(excelData.OgrenciNo) ||
                    !string.IsNullOrWhiteSpace(excelData.DogumTarihi?.ToString()) || !string.IsNullOrWhiteSpace(excelData.DogumYeri) ||
                    !string.IsNullOrWhiteSpace(excelData.Cinsiyet) || !string.IsNullOrWhiteSpace(excelData.Aciklama);
                
                if (hasAnyData)
                {
                    data.Add(excelData);
                    _logger.LogInformation($"Row {row} added with data: Ad='{excelData.Ad}', Soyad='{excelData.Soyad}', TC='{excelData.TCKimlikNo}', OgrenciNo='{excelData.OgrenciNo}'");
                }
                else
                {
                    _logger.LogWarning($"Row {row} skipped - no data found in any field");
                }
            }

            _logger.LogInformation($"Total rows processed: {data.Count}");
            return Task.FromResult(data);
        }

        private Task<List<ExcelData>> ProcessXlsFileAsync(IFormFile file, string uploadedBy)
        {
            var data = new List<ExcelData>();
            
            using var stream = file.OpenReadStream();
            IWorkbook workbook;
            
            if (file.FileName.EndsWith(".xlsx"))
                workbook = new XSSFWorkbook(stream);
            else
                workbook = new HSSFWorkbook(stream);

            var sheet = workbook.GetSheetAt(0);
            var rowCount = sheet.LastRowNum;

            _logger.LogInformation($"Processing XLS file: {file.FileName}, Total rows: {rowCount}");

            // Basit yaklaşım: İlk satırdan başla ve tüm verileri oku
            for (int row = 0; row <= rowCount; row++)
            {
                var sheetRow = sheet.GetRow(row);
                if (sheetRow == null) continue;

                var excelData = new ExcelData
                {
                    FileName = file.FileName,
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = uploadedBy,
                    RowNumber = row,
                    IsProcessed = false
                };

                // Her satırdan tüm hücreleri oku
                var rowValues = new List<string>();
                for (int col = 0; col < sheetRow.LastCellNum; col++)
                {
                    var cellValue = GetNpoiCellValue(sheetRow.GetCell(col));
                    rowValues.Add(cellValue);
                }

                _logger.LogInformation($"Row {row} raw values: {string.Join(" | ", rowValues)}");

                // Pozisyon bazlı mapping - kesin çalışır
                if (rowValues.Count > 0) excelData.Ad = rowValues[0];
                if (rowValues.Count > 1) excelData.Soyad = rowValues[1];
                if (rowValues.Count > 2) excelData.TCKimlikNo = rowValues[2];
                if (rowValues.Count > 3) excelData.OgrenciNo = rowValues[3];
                if (rowValues.Count > 4) excelData.DogumTarihi = GetNpoiDateValue(sheetRow, 4);
                if (rowValues.Count > 5) excelData.DogumYeri = rowValues[5];
                if (rowValues.Count > 6) excelData.Cinsiyet = rowValues[6];
                if (rowValues.Count > 7) excelData.Odenecek = GetNpoiDecimalValue(sheetRow, 7);
                if (rowValues.Count > 8) excelData.OdemeTarihi = GetNpoiDateValue(sheetRow, 8);
                if (rowValues.Count > 9) excelData.Aciklama = rowValues[9];

                // Herhangi bir veri varsa ekle
                var hasAnyData = !string.IsNullOrWhiteSpace(excelData.Ad) || !string.IsNullOrWhiteSpace(excelData.Soyad) || 
                    !string.IsNullOrWhiteSpace(excelData.TCKimlikNo) || !string.IsNullOrWhiteSpace(excelData.OgrenciNo) ||
                    !string.IsNullOrWhiteSpace(excelData.DogumTarihi?.ToString()) || !string.IsNullOrWhiteSpace(excelData.DogumYeri) ||
                    !string.IsNullOrWhiteSpace(excelData.Cinsiyet) || !string.IsNullOrWhiteSpace(excelData.Aciklama);
                
                if (hasAnyData)
                {
                    data.Add(excelData);
                    _logger.LogInformation($"Row {row} added with data: Ad='{excelData.Ad}', Soyad='{excelData.Soyad}', TC='{excelData.TCKimlikNo}', OgrenciNo='{excelData.OgrenciNo}'");
                }
                else
                {
                    _logger.LogWarning($"Row {row} skipped - no data found in any field");
                }
            }

            _logger.LogInformation($"Total rows processed: {data.Count}");
            return Task.FromResult(data);
        }

        private string? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                if (cell?.Value == null) return null;
                
                var value = cell.Value.ToString();
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error reading cell at row {row}, col {col}");
                return null;
            }
        }

        private decimal? GetDecimalValue(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                if (cell?.Value == null) return null;
                
                if (decimal.TryParse(cell.Value.ToString(), out var result))
                    return result;
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private DateTime? GetDateValue(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                if (cell?.Value == null) return null;
                
                if (cell.Value is DateTime dateTime)
                    return dateTime;
                
                if (DateTime.TryParse(cell.Value.ToString(), out var result))
                    return result;
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string? GetNpoiCellValue(IRow row, int col)
        {
            try
            {
                var cell = row.GetCell(col);
                return cell?.ToString()?.Trim();
            }
            catch
            {
                return null;
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
                    // For formulas, try to get the calculated value
                    try
                    {
                        switch (cell.CachedFormulaResultType)
                        {
                            case CellType.String:
                    return cell.StringCellValue ?? string.Empty;
                            case CellType.Numeric:
                                if (DateUtil.IsCellDateFormatted(cell))
                                    return cell.DateCellValue.ToString();
                                return cell.NumericCellValue.ToString();
                            case CellType.Boolean:
                                return cell.BooleanCellValue.ToString();
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

        private decimal? GetNpoiDecimalValue(IRow row, int col)
        {
            try
            {
                var cell = row.GetCell(col);
                if (cell == null) return null;
                
                if (cell.CellType == CellType.Numeric)
                    return Convert.ToDecimal(cell.NumericCellValue);
                
                if (decimal.TryParse(cell.ToString(), out var result))
                    return result;
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private DateTime? GetNpoiDateValue(IRow row, int col)
        {
            try
            {
                var cell = row.GetCell(col);
                if (cell == null) return null;
                
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                    return cell.DateCellValue;
                
                if (DateTime.TryParse(cell.ToString(), out var result))
                    return result;
                
                return null;
            }
            catch
            {
                return null;
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
                    // For formulas, try to get the calculated value
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

        private object GetCellValueWithType(ExcelWorksheet worksheet, int row, int col)
        {
            try
            {
                var cell = worksheet.Cells[row, col];
                if (cell?.Value == null) return string.Empty;
                
                // Return the actual value with its type
                return cell.Value;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(List<ExcelData> data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Excel Data");

            // Add headers
            var properties = typeof(ExcelData).GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "FileName" && p.Name != "UploadDate" && 
                           p.Name != "UploadedBy" && p.Name != "RowNumber" && p.Name != "IsProcessed" && 
                           p.Name != "ProcessedDate")
                .ToList();

            for (int i = 0; i < properties.Count; i++)
            {
                var displayAttr = properties[i].GetCustomAttributes(typeof(DisplayAttribute), false)
                    .FirstOrDefault() as DisplayAttribute;
                worksheet.Cells[1, i + 1].Value = displayAttr?.Name ?? properties[i].Name;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Add data
            for (int row = 0; row < data.Count; row++)
            {
                for (int col = 0; col < properties.Count; col++)
                {
                    var value = properties[col].GetValue(data[row]);
                    worksheet.Cells[row + 2, col + 1].Value = value;
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        public async Task<List<ExcelData>> GetDataByFileNameAsync(string fileName)
        {
            // This would typically query the database
            // For now, return empty list
            return await Task.FromResult(new List<ExcelData>());
        }

        public Task<bool> ValidateExcelFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Task.FromResult(false);

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
                return Task.FromResult(false);

            if (file.Length > 50 * 1024 * 1024) // 50MB limit
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        public async Task<string> GetFileSummaryAsync(IFormFile file)
        {
            try
            {
                var data = await ProcessExcelFileAsync(file, "System");
                return $"Dosya: {file.FileName}, Toplam Satır: {data.Count}, Boyut: {file.Length / 1024} KB";
            }
            catch
            {
                return $"Dosya işlenemedi: {file.FileName}";
            }
        }
    }
}
