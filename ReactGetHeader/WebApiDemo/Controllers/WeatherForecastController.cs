using Microsoft.AspNetCore.Mvc;

namespace WebApiDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    // 從 Header 取得地點與開始時間，並且將天氣預報結果透過 Response Header 回傳回去
    [HttpGet("GetWeatherForecastWithHeaders", Name = "GetWeatherForecastWithHeaders")]
    public string GetWeatherForecastWithHeaders(
        [FromHeader(Name = "Location")] string location,
        [FromHeader(Name = "StartDate")] DateOnly startDate)
    {
        _logger.LogInformation("Location: {Location}, StartDate: {StartDate}", location, startDate);
        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        //return forecasts;

        // 將天氣預報結果序列化為 JSON 字串，並且放到 Response Header 中
        var forecastsJson = System.Text.Json.JsonSerializer.Serialize(forecasts);
        Response.Headers.Add("X-Weather-Forecasts", forecastsJson);
        return "OK";
    }
}
