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
    public class DynamicTableController : ControllerBase
    {
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DynamicTableController> _logger;

        public DynamicTableController(
            IDynamicTableService dynamicTableService,
            ApplicationDbContext context,
            ILogger<DynamicTableController> logger)
        {
            _dynamicTableService = dynamicTableService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            try
            {
                var tables = await _context.DynamicTables
                    .OrderByDescending(t => t.UploadDate)
                    .Select(t => new
                    {
                        t.Id,
                        t.TableName,
                        t.FileName,
                        t.Description,
                        t.UploadDate,
                        t.RowCount,
                        t.ColumnCount,
                        t.IsProcessed,
                        t.ProcessedDate
                    })
                    .ToListAsync();

                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tables");
                return StatusCode(500, new { error = "Tablolar alınırken hata oluştu" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTable(int id)
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
                    Columns = table.Columns.Select(c => new
                    {
                        c.Id,
                        c.ColumnName,
                        c.DisplayName,
                        c.DataType,
                        c.ColumnOrder,
                        c.MaxLength,
                        c.IsRequired,
                        c.IsUnique
                    }).OrderBy(c => c.ColumnOrder)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table with ID: {Id}", id);
                return StatusCode(500, new { error = "Tablo alınırken hata oluştu" });
            }
        }

        [HttpGet("{tableName}/data")]
        public async Task<IActionResult> GetTableData(string tableName, int page = 1, int pageSize = 50)
        {
            try
            {
                var result = await _dynamicTableService.GetTableDataAsync(tableName);
                if (!result.IsSuccess)
                {
                    return StatusCode(500, new { error = "Tablo verisi alınamadı", details = result.ErrorMessage });
                }

                var data = result.Data;
                var totalCount = data.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var pagedData = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return Ok(new
                {
                    data = pagedData,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data for: {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo verisi alınırken hata oluştu" });
            }
        }

        [HttpDelete("{tableName}")]
        public async Task<IActionResult> DeleteTable(string tableName)
        {
            try
            {
                var result = await _dynamicTableService.DeleteTableAsync(tableName);
                if (!result.IsSuccess)
                {
                    return StatusCode(500, new { error = "Tablo silinemedi", details = result.ErrorMessage });
                }

                // Also delete from tracking table
                var trackingTable = await _context.DynamicTables.FirstOrDefaultAsync(t => t.TableName == tableName);
                if (trackingTable != null)
                {
                    _context.DynamicTables.Remove(trackingTable);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = $"Tablo {tableName} başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting table: {TableName}", tableName);
                return StatusCode(500, new { error = "Tablo silinirken hata oluştu" });
            }
        }
    }
}
