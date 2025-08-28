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

        private Task<object> GetXlsxPreviewAsync(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets[0];

                if (worksheet == null)
                    throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                // Get headers (first row)
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = GetCellValue(worksheet, 1, col);
                    headers.Add(string.IsNullOrEmpty(headerValue) ? $"Sütun_{col}" : headerValue);
                }

                // Get first 10 rows of data (excluding header)
                var rows = new List<List<string>>();
                for (int row = 2; row <= Math.Min(11, rowCount); row++)
                {
                    var rowData = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = GetCellValue(worksheet, row, col);
                        rowData.Add(cellValue ?? "");
                    }
                    rows.Add(rowData);
                }

                var result = new
                {
                    headers = headers,
                    rows = rows,
                    totalRows = rowCount - 1, // Exclude header
                    totalColumns = colCount
                };

                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetXlsxPreviewAsync");
                throw;
            }
        }

        private Task<object> GetXlsPreviewAsync(IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.First();

                if (worksheet == null)
                    throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

                var rowCount = worksheet.RowCount();
                var colCount = worksheet.ColumnCount();

                // Get headers (first row)
                var headers = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = worksheet.Cell(1, col).Value.ToString() ?? "";
                    headers.Add(string.IsNullOrEmpty(headerValue) ? $"Sütun_{col}" : headerValue);
                }

                // Get first 10 rows of data (excluding header)
                var rows = new List<List<string>>();
                for (int row = 2; row <= Math.Min(11, rowCount); row++)
                {
                    var rowData = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var cellValue = worksheet.Cell(row, col).Value.ToString() ?? "";
                        rowData.Add(cellValue);
                    }
                    rows.Add(rowData);
                }

                var result = new
                {
                    headers = headers,
                    rows = rows,
                    totalRows = rowCount - 1, // Exclude header
                    totalColumns = colCount
                };

                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetXlsPreviewAsync");
                throw;
            }
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

        private async Task<List<ExcelData>> ProcessXlsxFileAsync(IFormFile file, string uploadedBy)
        {
            var data = new List<ExcelData>();
            
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault() ?? package.Workbook.Worksheets[0];

            if (worksheet == null)
                throw new InvalidOperationException("Excel dosyasında çalışma sayfası bulunamadı.");

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            // Skip header row (row 1)
            for (int row = 2; row <= rowCount; row++)
            {
                var excelData = new ExcelData
                {
                    FileName = file.FileName,
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = uploadedBy,
                    RowNumber = row,
                    IsProcessed = false
                };

                // Map columns based on the Excel structure from the images
                excelData.BasvuruYili = GetCellValue(worksheet, row, 1); // Column A
                excelData.HareketlilikTipi = GetCellValue(worksheet, row, 2); // Column B
                excelData.BasvuruTipi = GetCellValue(worksheet, row, 3); // Column C
                excelData.Ad = GetCellValue(worksheet, row, 4); // Column D
                excelData.Soyad = GetCellValue(worksheet, row, 5); // Column E
                excelData.OdemeTipi = GetCellValue(worksheet, row, 6); // Column F
                excelData.Taksit = GetCellValue(worksheet, row, 7); // Column G
                excelData.Odenecek = GetDecimalValue(worksheet, row, 8); // Column H
                excelData.Odendiginde = GetDecimalValue(worksheet, row, 9); // Column I
                excelData.OdemeTarihi = GetDateValue(worksheet, row, 10); // Column J
                excelData.Aciklama = GetCellValue(worksheet, row, 11); // Column K
                excelData.OdemeOrani = GetDecimalValue(worksheet, row, 12); // Column L

                // Additional columns for detailed student information
                if (colCount >= 13) excelData.KullaniciAdi = GetCellValue(worksheet, row, 13);
                if (colCount >= 14) excelData.TCKimlikNo = GetCellValue(worksheet, row, 14);
                if (colCount >= 15) excelData.PasaportNo = GetCellValue(worksheet, row, 15);
                if (colCount >= 16) excelData.DogumTarihi = GetDateValue(worksheet, row, 16);
                if (colCount >= 17) excelData.DogumYeri = GetCellValue(worksheet, row, 17);
                if (colCount >= 18) excelData.Cinsiyet = GetCellValue(worksheet, row, 18);

                // Address and bank information
                if (colCount >= 19) excelData.AdresIl = GetCellValue(worksheet, row, 19);
                if (colCount >= 20) excelData.AdresUlke = GetCellValue(worksheet, row, 20);
                if (colCount >= 21) excelData.BankaHesapSahibi = GetCellValue(worksheet, row, 21);
                if (colCount >= 22) excelData.BankaAdi = GetCellValue(worksheet, row, 22);
                if (colCount >= 23) excelData.BankaSubeKodu = GetCellValue(worksheet, row, 23);
                if (colCount >= 24) excelData.BankaSubeAdi = GetCellValue(worksheet, row, 24);
                if (colCount >= 25) excelData.BankaHesapNumarasi = GetCellValue(worksheet, row, 25);
                if (colCount >= 26) excelData.BankaIBANNo = GetCellValue(worksheet, row, 26);

                // Student and academic information
                if (colCount >= 27) excelData.OgrenciNo = GetCellValue(worksheet, row, 27);
                if (colCount >= 28) excelData.FakulteAdi = GetCellValue(worksheet, row, 28);
                if (colCount >= 29) excelData.BirimAdi = GetCellValue(worksheet, row, 29);
                if (colCount >= 30) excelData.DiplomaDerecesi = GetCellValue(worksheet, row, 30);
                if (colCount >= 31) excelData.Sinif = GetCellValue(worksheet, row, 31);

                // Only add if at least one field has data
                if (!string.IsNullOrWhiteSpace(excelData.Ad) || !string.IsNullOrWhiteSpace(excelData.Soyad) || 
                    !string.IsNullOrWhiteSpace(excelData.TCKimlikNo) || !string.IsNullOrWhiteSpace(excelData.OgrenciNo))
                {
                    data.Add(excelData);
                }
            }

            return data;
        }

        private async Task<List<ExcelData>> ProcessXlsFileAsync(IFormFile file, string uploadedBy)
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

            // Skip header row (row 0)
            for (int row = 1; row <= rowCount; row++)
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

                // Map columns based on the Excel structure from the images
                excelData.BasvuruYili = GetNpoiCellValue(sheetRow, 0); // Column A
                excelData.HareketlilikTipi = GetNpoiCellValue(sheetRow, 1); // Column B
                excelData.BasvuruTipi = GetNpoiCellValue(sheetRow, 2); // Column C
                excelData.Ad = GetNpoiCellValue(sheetRow, 3); // Column D
                excelData.Soyad = GetNpoiCellValue(sheetRow, 4); // Column E
                excelData.OdemeTipi = GetNpoiCellValue(sheetRow, 5); // Column F
                excelData.Taksit = GetNpoiCellValue(sheetRow, 6); // Column G
                excelData.Odenecek = GetNpoiDecimalValue(sheetRow, 7); // Column H
                excelData.Odendiginde = GetNpoiDecimalValue(sheetRow, 8); // Column I
                excelData.OdemeTarihi = GetNpoiDateValue(sheetRow, 9); // Column J
                excelData.Aciklama = GetNpoiCellValue(sheetRow, 10); // Column K
                excelData.OdemeOrani = GetNpoiDecimalValue(sheetRow, 11); // Column L

                // Additional columns for detailed student information
                if (sheetRow.LastCellNum >= 13) excelData.KullaniciAdi = GetNpoiCellValue(sheetRow, 12);
                if (sheetRow.LastCellNum >= 14) excelData.TCKimlikNo = GetNpoiCellValue(sheetRow, 13);
                if (sheetRow.LastCellNum >= 15) excelData.PasaportNo = GetNpoiCellValue(sheetRow, 14);
                if (sheetRow.LastCellNum >= 16) excelData.DogumTarihi = GetNpoiDateValue(sheetRow, 15);
                if (sheetRow.LastCellNum >= 17) excelData.DogumYeri = GetNpoiCellValue(sheetRow, 16);
                if (sheetRow.LastCellNum >= 18) excelData.Cinsiyet = GetNpoiCellValue(sheetRow, 17);

                // Address and bank information
                if (sheetRow.LastCellNum >= 19) excelData.AdresIl = GetNpoiCellValue(sheetRow, 18);
                if (sheetRow.LastCellNum >= 20) excelData.AdresUlke = GetNpoiCellValue(sheetRow, 19);
                if (sheetRow.LastCellNum >= 21) excelData.BankaHesapSahibi = GetNpoiCellValue(sheetRow, 20);
                if (sheetRow.LastCellNum >= 22) excelData.BankaAdi = GetNpoiCellValue(sheetRow, 21);
                if (sheetRow.LastCellNum >= 23) excelData.BankaSubeKodu = GetNpoiCellValue(sheetRow, 22);
                if (sheetRow.LastCellNum >= 24) excelData.BankaSubeAdi = GetNpoiCellValue(sheetRow, 23);
                if (sheetRow.LastCellNum >= 25) excelData.BankaHesapNumarasi = GetNpoiCellValue(sheetRow, 24);
                if (sheetRow.LastCellNum >= 26) excelData.BankaIBANNo = GetNpoiCellValue(sheetRow, 25);

                // Student and academic information
                if (sheetRow.LastCellNum >= 27) excelData.OgrenciNo = GetNpoiCellValue(sheetRow, 26);
                if (sheetRow.LastCellNum >= 28) excelData.FakulteAdi = GetNpoiCellValue(sheetRow, 27);
                if (sheetRow.LastCellNum >= 29) excelData.BirimAdi = GetNpoiCellValue(sheetRow, 28);
                if (sheetRow.LastCellNum >= 30) excelData.DiplomaDerecesi = GetNpoiCellValue(sheetRow, 29);
                if (sheetRow.LastCellNum >= 31) excelData.Sinif = GetNpoiCellValue(sheetRow, 30);

                // Only add if at least one field has data
                if (!string.IsNullOrWhiteSpace(excelData.Ad) || !string.IsNullOrWhiteSpace(excelData.Soyad) || 
                    !string.IsNullOrWhiteSpace(excelData.TCKimlikNo) || !string.IsNullOrWhiteSpace(excelData.OgrenciNo))
                {
                    data.Add(excelData);
                }
            }

            return data;
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
