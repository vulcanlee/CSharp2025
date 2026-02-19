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

    [HttpGet("GetWeatherByLocationAndDate")]
    public ActionResult<IEnumerable<WeatherForecast>> 
        GetWeatherByLocationAndDate([FromQuery] string location, [FromQuery] DateOnly date)
    {
        // 這裡可以根據地點與日期來產生不同的天氣預報
        return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = date.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            Location = location
        }).ToArray());
    }

    [HttpGet("GetWeatherByLocationAndDate/{location}/{date}")]
    public ActionResult<IEnumerable<WeatherForecast>> 
        GetWeatherByLocationAndDateRoute([FromRoute] string location, [FromRoute] DateOnly date)
    {
        // 這裡可以根據地點與日期來產生不同的天氣預報
        return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = date.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            Location = location
        }).ToArray());
    }
}
