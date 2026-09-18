using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShelaStudioApi.Services;

/// <summary>
/// Fetches current weather from OpenWeather and shapes it for the app.
/// </summary>
public class WeatherService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<WeatherService> _log;

    public WeatherService(HttpClient http, IConfiguration config, ILogger<WeatherService> log)
    {
        _http = http;
        _config = config;
        _log = log;
    }

    public async Task<WeatherResult> GetCurrentAsync(string city, string units = "metric")
    {
        var apiKey = _config["OpenWeather:ApiKey"]
            ?? throw new InvalidOperationException("OpenWeather:ApiKey is not set");
        var baseUrl = _config["OpenWeather:BaseUrl"]
            ?? "https://api.openweathermap.org/data/2.5";

        var url = $"{baseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={apiKey}&units={units}";
        _log.LogInformation("Fetching weather for {City}", city);

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _log.LogWarning("OpenWeather failed: {Status} {Body}", response.StatusCode, body);
            throw new HttpRequestException($"OpenWeather returned {response.StatusCode}");
        }

        var raw = await response.Content.ReadAsStringAsync();
        var open = JsonSerializer.Deserialize<OpenWeatherResponse>(raw, JsonOpts())
            ?? throw new HttpRequestException("Could not parse OpenWeather response");

        var condition = open.Weather?.FirstOrDefault()?.Main ?? "Clear";
        var description = open.Weather?.FirstOrDefault()?.Description ?? "";
        var isRainy = condition.Equals("Rain", StringComparison.OrdinalIgnoreCase)
                   || condition.Equals("Drizzle", StringComparison.OrdinalIgnoreCase)
                   || condition.Equals("Thunderstorm", StringComparison.OrdinalIgnoreCase);

        return new WeatherResult
        {
            LocationName = open.Name ?? city,
            TemperatureCelsius = open.Main?.Temp ?? 0,
            Condition = condition,
            Description = Capitalize(description),
            IsRainy = isRainy,
            Humidity = open.Main?.Humidity ?? 0,
            WindSpeed = open.Wind?.Speed ?? 0,
            FetchedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    private static JsonSerializerOptions JsonOpts() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ---- Private DTOs matching OpenWeather's JSON ----
    private class OpenWeatherResponse
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("weather")] public List<OpenWeatherWeather>? Weather { get; set; }
        [JsonPropertyName("main")] public OpenWeatherMain? Main { get; set; }
        [JsonPropertyName("wind")] public OpenWeatherWind? Wind { get; set; }
    }

    private class OpenWeatherWeather
    {
        [JsonPropertyName("main")] public string? Main { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    private class OpenWeatherMain
    {
        [JsonPropertyName("temp")] public double Temp { get; set; }
        [JsonPropertyName("humidity")] public int Humidity { get; set; }
    }

    private class OpenWeatherWind
    {
        [JsonPropertyName("speed")] public double Speed { get; set; }
    }
}

/// <summary>
/// Public weather shape returned to the mobile app.
/// Must match the Android WeatherDto fields.
/// </summary>
public class WeatherResult
{
    public string LocationName { get; set; } = "";
    public double TemperatureCelsius { get; set; }
    public string Condition { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsRainy { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public long FetchedAt { get; set; }
}