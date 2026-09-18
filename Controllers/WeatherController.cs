using Microsoft.AspNetCore.Mvc;
using ShelaStudioApi.Services;

namespace ShelaStudioApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly WeatherService _weather;
    private readonly ILogger<WeatherController> _log;

    public WeatherController(WeatherService weather, ILogger<WeatherController> log)
    {
        _weather = weather;
        _log = log;
    }

    /// <summary>
    /// GET /api/weather?city=Johannesburg
    /// Returns current weather for the given city.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string city = "Johannesburg")
    {
        try
        {
            var result = await _weather.GetCurrentAsync(city);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Weather fetch failed for {City}", city);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Health check — useful for verifying deployment.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok", timestamp = DateTime.UtcNow });
}