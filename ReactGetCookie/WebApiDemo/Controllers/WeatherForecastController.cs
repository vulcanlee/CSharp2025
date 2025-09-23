using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace WebApiDemo.Controllers
{
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

        // Ū�� Cookie: forecast_date (yyyy-MM-dd), forecast_location
        // �^�Ǥ覡�G�H Cookie �g�X forecast_data (JSON)�A�P�ɦ^�g����P�a�I Cookie
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
                HttpOnly = false,             // �e�� JS �ݭnŪ��
                Secure = true,                // SameSite=None �ݷf�t Secure�A�B API ������ HTTPS
                SameSite = SameSiteMode.None, // ���\�󯸡]React �P API ���P���ɡ^
                Expires = DateTimeOffset.UtcNow.AddMinutes(10),
                Path = "/"
            };

            // �^�g����P�a�I�A�T�O�e�ݥiŪ��@�P��
            Response.Cookies.Append("forecast_date", startDate.ToString("yyyy-MM-dd"), cookieOptions);
            Response.Cookies.Append("forecast_location", location, cookieOptions);

            // �g�X�w����ơ]JSON�^
            Response.Cookies.Append("forecast_data", json, cookieOptions);

            // ���e�w�b Cookie ���A���夣�ݦA�^��
            return NoContent();
        }
    }
}
