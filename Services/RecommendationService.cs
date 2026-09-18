namespace ShelaStudioApi.Services;

/// <summary>
/// Matches clothing items against weather to produce outfit recommendations.
/// </summary>
public class RecommendationService
{
    private readonly ILogger<RecommendationService> _log;

    public RecommendationService(ILogger<RecommendationService> log)
    {
        _log = log;
    }

    public List<OutfitSuggestion> Recommend(RecommendRequest request)
    {
        var requiredWarmth = RequiredWarmth(request.Weather.TemperatureCelsius);
        _log.LogInformation(
            "Recommending. Required warmth={W}, rainy={R}, item count={C}",
            requiredWarmth, request.Weather.IsRainy, request.Wardrobe.Count);

        // Filter suitable items
        var suitable = request.Wardrobe.Where(item =>
        {
            var warmthOk = item.WarmthLevel >= requiredWarmth - 1;
            var rainOk = !request.Weather.IsRainy || item.IsWaterResistant;
            var seasonOk = item.Season switch
            {
                "Summer" => request.Weather.TemperatureCelsius >= 20,
                "Winter" => request.Weather.TemperatureCelsius <= 15,
                _ => true
            };
            return warmthOk && rainOk && seasonOk;
        }).ToList();

        var suitableIds = suitable.Select(i => i.ItemId).ToHashSet();

        // Check saved outfits
        var savedMatches = request.SavedOutfits
            .Where(o => o.ItemIds.Count > 0 && o.ItemIds.All(id => suitableIds.Contains(id)))
            .Select(o => new OutfitSuggestion
            {
                Name = o.Name,
                Occasion = o.Occasion,
                ItemIds = o.ItemIds,
                Reason = Explain(request.Weather),
                IsFromSaved = true
            })
            .ToList();

        if (savedMatches.Count > 0) return savedMatches;

        // Generate new combinations
        var tops = suitable.Where(i => i.Category == "Tops").ToList();
        var bottoms = suitable.Where(i => i.Category == "Bottoms").ToList();
        var shoes = suitable.Where(i => i.Category == "Shoes").ToList();
        var outerwear = suitable.Where(i => i.Category == "Outerwear").ToList();

        var generated = new List<OutfitSuggestion>();
        foreach (var t in tops)
            foreach (var b in bottoms)
                foreach (var s in shoes)
                {
                    if (generated.Count >= 5) break;
                    var ids = new List<string> { t.ItemId, b.ItemId, s.ItemId };
                    if (requiredWarmth >= 3 && outerwear.Count > 0)
                        ids.Add(outerwear[0].ItemId);

                    generated.Add(new OutfitSuggestion
                    {
                        Name = $"{t.Name} + {b.Name}",
                        Occasion = "Casual",
                        ItemIds = ids,
                        Reason = Explain(request.Weather),
                        IsFromSaved = false
                    });
                }

        // Fallback: any single suitable item
        if (generated.Count == 0 && suitable.Count > 0)
        {
            generated = suitable.Take(4).Select(i => new OutfitSuggestion
            {
                Name = i.Name,
                Occasion = "Casual",
                ItemIds = new List<string> { i.ItemId },
                Reason = Explain(request.Weather),
                IsFromSaved = false
            }).ToList();
        }

        return generated;
    }

    private static int RequiredWarmth(double celsius) => celsius switch
    {
        >= 28 => 1,
        >= 20 => 2,
        >= 12 => 3,
        >= 5 => 4,
        _ => 5
    };

    private static string Explain(WeatherResultDto w) => w.IsRainy
        ? "Water-resistant pieces are ideal for today's rain."
        : w.TemperatureCelsius switch
        {
            >= 28 => "Light, breathable layers for the heat.",
            >= 20 => "Light layers are ideal for today's mild weather.",
            >= 12 => "A light jacket will keep you comfortable.",
            >= 5 => "Warm layers for the cooler weather.",
            _ => "Bundle up — it's cold outside."
        };
}

// ---- Request/response DTOs ----

public class RecommendRequest
{
    public WeatherResultDto Weather { get; set; } = new();
    public List<ClothingItemDto> Wardrobe { get; set; } = new();
    public List<OutfitDto> SavedOutfits { get; set; } = new();
}

public class WeatherResultDto
{
    public string LocationName { get; set; } = "";
    public double TemperatureCelsius { get; set; }
    public string Condition { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsRainy { get; set; }
}

public class ClothingItemDto
{
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Season { get; set; } = "All-Season";
    public int WarmthLevel { get; set; } = 3;
    public bool IsWaterResistant { get; set; }
}

public class OutfitDto
{
    public string OutfitId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Occasion { get; set; } = "Casual";
    public List<string> ItemIds { get; set; } = new();
}

public class OutfitSuggestion
{
    public string Name { get; set; } = "";
    public string Occasion { get; set; } = "";
    public List<string> ItemIds { get; set; } = new();
    public string Reason { get; set; } = "";
    public bool IsFromSaved { get; set; }
}