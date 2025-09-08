using Microsoft.AspNetCore.Mvc;

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

        // �q Header ���o�a�I�P�}�l�ɶ��A�åB�N�Ѯ�w�����G�z�L Response Header �^�Ǧ^�h
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

            // �N�Ѯ�w�����G�ǦC�Ƭ� JSON �r��A�åB��� Response Header ��
            var forecastsJson = System.Text.Json.JsonSerializer.Serialize(forecasts);
            Response.Headers.Add("X-Weather-Forecasts", forecastsJson);
            return "OK";
        }
    }
}
