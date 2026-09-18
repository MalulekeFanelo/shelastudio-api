using Microsoft.AspNetCore.Mvc;
using ShelaStudioApi.Services;

namespace ShelaStudioApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendController : ControllerBase
{
    private readonly RecommendationService _service;
    private readonly ILogger<RecommendController> _log;

    public RecommendController(RecommendationService service, ILogger<RecommendController> log)
    {
        _service = service;
        _log = log;
    }

    /// <summary>
    /// POST /api/recommend
    /// Body: { weather, wardrobe, savedOutfits }
    /// Returns ranked outfit suggestions.
    /// </summary>
    [HttpPost]
    public IActionResult Post([FromBody] RecommendRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Empty body" });

        _log.LogInformation(
            "Recommend request: {Items} items, {Outfits} outfits",
            request.Wardrobe.Count, request.SavedOutfits.Count);

        var suggestions = _service.Recommend(request);
        return Ok(new { suggestions });
    }
}