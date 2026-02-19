using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

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

    // 讀取 Cookie: forecast_date (yyyy-MM-dd), forecast_location
    // 回傳方式：以 Cookie 寫出 forecast_data (JSON)，同時回寫日期與地點 Cookie
    [HttpGet("CookieForecast")]
    public IActionResult GetForecastFromCookies()
    {
        Request.Cookies.TryGetValue("forecast_date", out var dateStr);
        Request.Cookies.TryGetValue("forecast_location", out var location);

        if (!DateOnly.TryParse(dateStr, out var startDate))
        {
            startDate = DateOnly.FromDateTime(DateTime.Now);
        }

        location ??= "Taipei";

        var forecasts = Enumerable.Range(0, 5).Select(i => new WeatherForecast
        {
            Date = startDate.AddDays(i),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();

        var json = JsonSerializer.Serialize(forecasts);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,             // 前端 JS 需要讀取
            Secure = true,                // SameSite=None 需搭配 Secure，且 API 必須走 HTTPS
            SameSite = SameSiteMode.None, // 允許跨站（React 與 API 不同源時）
            Expires = DateTimeOffset.UtcNow.AddMinutes(10),
            Path = "/"
        };

        // 回寫日期與地點，確保前端可讀到一致值
        Response.Cookies.Append("forecast_date", startDate.ToString("yyyy-MM-dd"), cookieOptions);
        Response.Cookies.Append("forecast_location", location, cookieOptions);

        // 寫出預報資料（JSON）
        Response.Cookies.Append("forecast_data", json, cookieOptions);

        // 內容已在 Cookie 中，本文不需再回傳
        return NoContent();
    }
}
