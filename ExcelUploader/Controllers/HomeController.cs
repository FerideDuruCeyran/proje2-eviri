using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Models;
using ExcelUploader.Services;
using System.Security.Claims;

namespace ExcelUploader.Controllers
{
    public class HomeController : Controller
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

        [Authorize]
        public async Task<IActionResult> Index()
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var dashboardData = new DashboardViewModel
                {
                    TotalRecords = tables.Sum(t => t.RowCount),
                    ProcessedRecords = tables.Where(t => t.IsProcessed).Sum(t => t.RowCount),
                    PendingRecords = tables.Where(t => !t.IsProcessed).Sum(t => t.RowCount),
                    TotalGrantAmount = 0, // Will be calculated from dynamic table data if needed
                    TotalPaidAmount = 0,  // Will be calculated from dynamic table data if needed
                    RecentUploads = new List<ExcelData>(), // Keep for backward compatibility
                    MonthlyData = new List<ChartData>(),   // Keep for backward compatibility
                    DynamicTables = tables.Take(5).ToList() // Show recent dynamic tables
                };
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View(new DashboardViewModel());
            }
        }

        [Authorize]
        public IActionResult Upload()
        {
            return View(new UploadViewModel());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Upload(UploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.ExcelFile == null)
                {
                    ModelState.AddModelError("ExcelFile", "Lütfen bir Excel dosyası seçin");
                    return View(model);
                }

                // Validate file
                if (!await _excelService.ValidateExcelFileAsync(model.ExcelFile))
                {
                    ModelState.AddModelError("ExcelFile", "Geçersiz dosya formatı veya boyut");
                    return View(model);
                }

                // Process Excel file and create dynamic table
                var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown";
                
                // Create dynamic table from Excel file
                var dynamicTable = await _dynamicTableService.CreateTableFromExcelAsync(model.ExcelFile, userName, model.Description);

                if (dynamicTable == null)
                {
                    ModelState.AddModelError("", "Excel dosyasından tablo oluşturulamadı");
                    return View(model);
                }

                TempData["SuccessMessage"] = $"Excel dosyası başarıyla yüklendi. '{dynamicTable.TableName}' tablosu oluşturuldu ve {dynamicTable.RowCount} kayıt işlendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading Excel file");
                ModelState.AddModelError("", "Dosya yüklenirken hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Data(int page = 1, string? searchTerm = null, string? sortBy = null, string? sortOrder = null)
        {
            try
            {
                var pageSize = 20;
                var data = await _dataImportService.GetPaginatedDataAsync(page, pageSize, searchTerm, sortBy, sortOrder);
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
                return View(new DataListViewModel());
            }
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var data = await _dataImportService.GetDataByIdAsync(id);
                if (data == null)
                {
                    TempData["ErrorMessage"] = "Kayıt bulunamadı";
                    return RedirectToAction(nameof(Data));
                }

                var editModel = new EditDataViewModel
                {
                    Id = data.Id,
                    BasvuruYili = data.BasvuruYili,
                    HareketlilikTipi = data.HareketlilikTipi,
                    BasvuruTipi = data.BasvuruTipi,
                    Ad = data.Ad,
                    Soyad = data.Soyad,
                    OdemeTipi = data.OdemeTipi,
                    Taksit = data.Taksit,
                    Odenecek = data.Odenecek,
                    Odendiginde = data.Odendiginde,
                    OdemeTarihi = data.OdemeTarihi,
                    Aciklama = data.Aciklama,
                    OdemeOrani = data.OdemeOrani,
                    FileName = data.FileName,
                    UploadDate = data.UploadDate,
                    UploadedBy = data.UploadedBy,
                    RowNumber = data.RowNumber
                };

                return View(editModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading data for edit with ID {id}");
                TempData["ErrorMessage"] = "Kayıt yüklenirken hata oluştu";
                return RedirectToAction(nameof(Data));
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(EditDataViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var data = await _dataImportService.GetDataByIdAsync(model.Id);
                if (data == null)
                {
                    TempData["ErrorMessage"] = "Kayıt bulunamadı";
                    return RedirectToAction(nameof(Data));
                }

                // Update data properties
                data.BasvuruYili = model.BasvuruYili;
                data.HareketlilikTipi = model.HareketlilikTipi;
                data.BasvuruTipi = model.BasvuruTipi;
                data.Ad = model.Ad;
                data.Soyad = model.Soyad;
                data.OdemeTipi = model.OdemeTipi;
                data.Taksit = model.Taksit;
                data.Odenecek = model.Odenecek;
                data.Odendiginde = model.Odendiginde;
                data.OdemeTarihi = model.OdemeTarihi;
                data.Aciklama = model.Aciklama;
                data.OdemeOrani = model.OdemeOrani;

                var updateResult = await _dataImportService.UpdateDataAsync(data);

                if (updateResult)
                {
                    TempData["SuccessMessage"] = "Kayıt başarıyla güncellendi";
                    return RedirectToAction(nameof(Data));
                }
                else
                {
                    ModelState.AddModelError("", "Kayıt güncellenirken hata oluştu");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating data with ID {model.Id}");
                ModelState.AddModelError("", "Kayıt güncellenirken hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleteResult = await _dataImportService.DeleteDataAsync(id);

                if (deleteResult)
                {
                    TempData["SuccessMessage"] = "Kayıt başarıyla silindi";
                }
                else
                {
                    TempData["ErrorMessage"] = "Kayıt silinirken hata oluştu";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting data with ID {id}");
                TempData["ErrorMessage"] = "Kayıt silinirken hata oluştu";
            }

            return RedirectToAction(nameof(Data));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Export(string? searchTerm = null, string? filterBy = null, string? filterValue = null)
        {
            try
            {
                var data = await _dataImportService.SearchDataAsync(searchTerm ?? "", filterBy, filterValue);
                
                if (!data.Any())
                {
                    TempData["ErrorMessage"] = "Dışa aktarılacak veri bulunamadı";
                    return RedirectToAction(nameof(Data));
                }

                var excelBytes = await _dataImportService.ExportDataToExcelAsync(data);
                var fileName = $"ExcelData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                TempData["ErrorMessage"] = "Veri dışa aktarılırken hata oluştu";
                return RedirectToAction(nameof(Data));
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
