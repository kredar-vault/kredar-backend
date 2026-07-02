using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Common;

[ApiController]
[Tags("Health")]
public class HealthController : ControllerBase
{
    [HttpGet("/api/health")]
    public IActionResult Get() => Ok(new { status = "Healthy" });
}
