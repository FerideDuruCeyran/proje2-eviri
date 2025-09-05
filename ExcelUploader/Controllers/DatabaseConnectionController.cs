using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseConnectionController : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public IActionResult GetConnections()
        {
            // Return empty list for now - can be extended later
            return Ok(new List<object>());
        }
    }
}
