using Microsoft.AspNetCore.Mvc;

namespace DeltaSocial.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Backend-ul funcționează!" });
        }
    }
}
