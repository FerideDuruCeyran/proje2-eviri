using Microsoft.AspNetCore.Mvc;
using ExcelUploader.Models;
using ExcelUploader.Services;
using Microsoft.AspNetCore.Authorization;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DatabaseConnectionController : ControllerBase
    {
        private readonly IPortService _portService;
        private readonly ILogger<DatabaseConnectionController> _logger;

        public DatabaseConnectionController(IPortService portService, ILogger<DatabaseConnectionController> logger)
        {
            _portService = portService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<DatabaseConnection>>> GetAllConnections()
        {
            try
            {
                var connections = await _portService.GetAllConnectionsAsync();
                return Ok(connections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantıları alınırken hata oluştu");
                return StatusCode(500, "Veritabanı bağlantıları alınırken hata oluştu");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DatabaseConnection>> GetConnection(int id)
        {
            try
            {
                var connection = await _portService.GetConnectionByIdAsync(id);
                return Ok(connection);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı alınırken hata oluştu: {Id}", id);
                return StatusCode(500, "Veritabanı bağlantısı alınırken hata oluştu");
            }
        }

        [HttpPost]
        public async Task<ActionResult<DatabaseConnection>> CreateConnection([FromBody] DatabaseConnection connection)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdConnection = await _portService.CreateConnectionAsync(connection);
                return CreatedAtAction(nameof(GetConnection), new { id = createdConnection.Id }, createdConnection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı oluşturulurken hata oluştu");
                return StatusCode(500, "Veritabanı bağlantısı oluşturulurken hata oluştu");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateConnection(int id, [FromBody] DatabaseConnection connection)
        {
            try
            {
                if (id != connection.Id)
                    return BadRequest("ID uyumsuzluğu");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var success = await _portService.UpdateConnectionAsync(connection);
                if (success)
                    return NoContent();
                else
                    return NotFound();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı güncellenirken hata oluştu: {Id}", id);
                return StatusCode(500, "Veritabanı bağlantısı güncellenirken hata oluştu");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteConnection(int id)
        {
            try
            {
                var success = await _portService.DeleteConnectionAsync(id);
                if (success)
                    return NoContent();
                else
                    return NotFound();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı silinirken hata oluştu: {Id}", id);
                return StatusCode(500, "Veritabanı bağlantısı silinirken hata oluştu");
            }
        }

        [HttpPost("{id}/test")]
        public async Task<ActionResult> TestConnection(int id)
        {
            try
            {
                var success = await _portService.TestConnectionByIdAsync(id);
                return Ok(new { success, message = success ? "Bağlantı başarılı" : "Bağlantı başarısız" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı test edilirken hata oluştu: {Id}", id);
                return StatusCode(500, "Veritabanı bağlantısı test edilirken hata oluştu");
            }
        }

        [HttpPost("test")]
        public async Task<ActionResult> TestConnectionDirect([FromBody] DatabaseConnection connection)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var success = await _portService.TestConnectionAsync(connection);
                return Ok(new { success, message = success ? "Bağlantı başarılı" : "Bağlantı başarısız" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanı bağlantısı test edilirken hata oluştu");
                return StatusCode(500, "Veritabanı bağlantısı test edilirken hata oluştu");
            }
        }

        [HttpGet("{id}/databases")]
        public async Task<ActionResult<List<string>>> GetDatabases(int id)
        {
            try
            {
                var connection = await _portService.GetConnectionByIdAsync(id);
                var databases = await _portService.GetDatabasesAsync(connection);
                return Ok(databases);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veritabanları alınırken hata oluştu: {Id}", id);
                return StatusCode(500, "Veritabanları alınırken hata oluştu");
            }
        }

        [HttpGet("{id}/tables")]
        public async Task<ActionResult<List<string>>> GetTables(int id, [FromQuery] string databaseName)
        {
            try
            {
                if (string.IsNullOrEmpty(databaseName))
                    return BadRequest("Veritabanı adı gerekli");

                var connection = await _portService.GetConnectionByIdAsync(id);
                var tables = await _portService.GetTablesAsync(connection, databaseName);
                return Ok(tables);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tablolar alınırken hata oluştu: {Id}, Database: {Database}", id, databaseName);
                return StatusCode(500, "Tablolar alınırken hata oluştu");
            }
        }

        [HttpPost("{id}/execute")]
        public async Task<ActionResult> ExecuteQuery(int id, [FromQuery] string databaseName, [FromBody] ExecuteQueryRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(databaseName))
                    return BadRequest("Veritabanı adı gerekli");

                if (string.IsNullOrEmpty(request.Query))
                    return BadRequest("SQL sorgusu gerekli");

                var connection = await _portService.GetConnectionByIdAsync(id);
                var success = await _portService.ExecuteQueryAsync(connection, databaseName, request.Query);
                
                if (success)
                    return Ok(new { success = true, message = "Sorgu başarıyla çalıştırıldı" });
                else
                    return BadRequest("Sorgu çalıştırılamadı");
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sorgu çalıştırılırken hata oluştu: {Id}, Database: {Database}", id, databaseName);
                return StatusCode(500, "Sorgu çalıştırılırken hata oluştu");
            }
        }

        [HttpPost("{id}/execute-with-result")]
        public async Task<ActionResult> ExecuteQueryWithResult(int id, [FromQuery] string databaseName, [FromBody] ExecuteQueryRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(databaseName))
                    return BadRequest("Veritabanı adı gerekli");

                if (string.IsNullOrEmpty(request.Query))
                    return BadRequest("SQL sorgusu gerekli");

                var connection = await _portService.GetConnectionByIdAsync(id);
                var result = await _portService.ExecuteQueryWithResultAsync(connection, databaseName, request.Query);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sorgu sonuçları alınırken hata oluştu: {Id}, Database: {Database}", id, databaseName);
                return StatusCode(500, "Sorgu sonuçları alınırken hata oluştu");
            }
        }
    }

    public class ExecuteQueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }
}
