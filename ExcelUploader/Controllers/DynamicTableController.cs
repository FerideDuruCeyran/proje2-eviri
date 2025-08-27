using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExcelUploader.Services;
using ExcelUploader.Models;
using ExcelUploader.Data;

namespace ExcelUploader.Controllers
{
    [Authorize]
    public class DynamicTableController : Controller
    {
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ILogger<DynamicTableController> _logger;

        public DynamicTableController(IDynamicTableService dynamicTableService, ILogger<DynamicTableController> logger)
        {
            _dynamicTableService = dynamicTableService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var tables = await _dynamicTableService.GetAllTablesAsync();
                var viewModel = new DynamicTableListViewModel
                {
                    Tables = tables,
                    TotalTables = tables.Count,
                    ProcessedTables = tables.Count(t => t.IsProcessed),
                    PendingTables = tables.Count(t => !t.IsProcessed)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dynamic tables");
                TempData["Error"] = "Tablolar yüklenirken hata oluştu.";
                return View(new DynamicTableListViewModel());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var table = await _dynamicTableService.GetTableByIdAsync(id);
                if (table == null)
                {
                    TempData["Error"] = "Tablo bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                var data = await _dynamicTableService.GetTableDataAsync(table.TableName, 1, 50);
                var totalCount = await _dynamicTableService.GetTableDataCountAsync(table.TableName);

                var viewModel = new DynamicTableDetailsViewModel
                {
                    Table = table,
                    Data = data,
                    TotalRows = totalCount,
                    CurrentPage = 1,
                    PageSize = 50
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table details: {Id}", id);
                TempData["Error"] = "Tablo detayları yüklenirken hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Data(string tableName, int page = 1, int pageSize = 50)
        {
            try
            {
                var table = await _dynamicTableService.GetTableByNameAsync(tableName);
                if (table == null)
                {
                    TempData["Error"] = "Tablo bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                var data = await _dynamicTableService.GetTableDataAsync(tableName, page, pageSize);
                var totalCount = await _dynamicTableService.GetTableDataCountAsync(tableName);

                var viewModel = new DynamicTableDataViewModel
                {
                    Table = table,
                    Data = data,
                    TotalRows = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data: {TableName}", tableName);
                TempData["Error"] = "Tablo verisi yüklenirken hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateData(string tableName, int rowId, [FromBody] Dictionary<string, object> data)
        {
            try
            {
                var success = await _dynamicTableService.UpdateTableDataAsync(tableName, rowId, data);
                if (success)
                {
                    return Json(new { success = true, message = "Veri başarıyla güncellendi." });
                }
                else
                {
                    return Json(new { success = false, message = "Veri güncellenirken hata oluştu." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating table data: {TableName}, RowId: {RowId}", tableName, rowId);
                return Json(new { success = false, message = "Veri güncellenirken hata oluştu." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteData(string tableName, int rowId)
        {
            try
            {
                var success = await _dynamicTableService.DeleteTableDataAsync(tableName, rowId);
                if (success)
                {
                    TempData["Success"] = "Veri başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Veri silinirken hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table data: {TableName}, RowId: {RowId}", tableName, rowId);
                TempData["Error"] = "Veri silinirken hata oluştu.";
            }

            return RedirectToAction(nameof(Data), new { tableName });
        }

        public async Task<IActionResult> Export(string tableName, string format = "xlsx")
        {
            try
            {
                var data = await _dynamicTableService.ExportTableDataAsync(tableName, format);
                if (data.Length == 0)
                {
                    TempData["Error"] = "Dışa aktarılacak veri bulunamadı.";
                    return RedirectToAction(nameof(Data), new { tableName });
                }

                var table = await _dynamicTableService.GetTableByNameAsync(tableName);
                var fileName = $"{table?.FileName ?? tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";

                return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting table data: {TableName}", tableName);
                TempData["Error"] = "Veri dışa aktarılırken hata oluştu.";
                return RedirectToAction(nameof(Data), new { tableName });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _dynamicTableService.DeleteTableAsync(id);
                if (success)
                {
                    TempData["Success"] = "Tablo başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Tablo silinirken hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {Id}", id);
                TempData["Error"] = "Tablo silinirken hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> GetTableData([FromBody] GetTableDataRequest request)
        {
            try
            {
                var data = await _dynamicTableService.GetTableDataAsync(request.TableName, request.Page, request.PageSize);
                var totalCount = await _dynamicTableService.GetTableDataCountAsync(request.TableName);

                return Json(new
                {
                    success = true,
                    data = data,
                    totalCount = totalCount,
                    currentPage = request.Page,
                    pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data for AJAX: {TableName}", request.TableName);
                return Json(new { success = false, message = "Veri yüklenirken hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTableDataGet(string tableName, int page = 1, int pageSize = 50)
        {
            try
            {
                var data = await _dynamicTableService.GetTableDataAsync(tableName, page, pageSize);
                var totalCount = await _dynamicTableService.GetTableDataCountAsync(tableName);

                return Json(new
                {
                    success = true,
                    data = data,
                    totalCount = totalCount,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data for AJAX: {TableName}", tableName);
                return Json(new { success = false, message = "Veri yüklenirken hata oluştu." });
            }
        }
    }
}
