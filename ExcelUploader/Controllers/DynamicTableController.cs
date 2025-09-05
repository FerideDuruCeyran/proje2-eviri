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
    public class DynamicTableController : ControllerBase
    {
        private readonly IDynamicTableService _dynamicTableService;
        private readonly ApplicationDbContext _context;

        public DynamicTableController(IDynamicTableService dynamicTableService, ApplicationDbContext context)
        {
            _dynamicTableService = dynamicTableService;
            _context = context;
        }

        [HttpGet]
        [Authorize]
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
                        t.IsProcessed
                    })
                    .ToListAsync();

                return Ok(tables);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Tablolar alınırken hata oluştu: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetTable(int id)
        {
            try
            {
                var table = await _context.DynamicTables.FindAsync(id);
                if (table == null)
                {
                    return NotFound(new { error = "Tablo bulunamadı" });
                }

                var data = await _dynamicTableService.GetTableDataAsync(table.TableName);
                if (!data.IsSuccess)
                {
                    return StatusCode(500, new { error = "Tablo verisi alınamadı: " + data.ErrorMessage });
                }

                return Ok(new
                {
                    table = new
                    {
                        table.Id,
                        table.TableName,
                        table.FileName,
                        table.Description,
                        table.UploadDate,
                        table.RowCount,
                        table.ColumnCount,
                        table.IsProcessed
                    },
                    data = data.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Tablo verisi alınırken hata oluştu: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTable(int id)
        {
            try
            {
                var table = await _context.DynamicTables.FindAsync(id);
                if (table == null)
                {
                    return NotFound(new { error = "Tablo bulunamadı" });
                }

                var result = await _dynamicTableService.DeleteTableAsync(table.TableName);
                if (!result.IsSuccess)
                {
                    return StatusCode(500, new { error = "Tablo silinirken hata oluştu: " + result.ErrorMessage });
                }

                return Ok(new { message = "Tablo başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Tablo silinirken hata oluştu: " + ex.Message });
            }
        }
    }
}
