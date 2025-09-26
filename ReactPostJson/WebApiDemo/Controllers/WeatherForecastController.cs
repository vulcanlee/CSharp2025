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

        [HttpPost(Name = "PostWeatherForecast")]
        public ApiResult<IEnumerable<WeatherForecast>> Post([FromBody] WeatherRequest request)
        {
            try
            {
                // 驗證輸入資料
                if (string.IsNullOrWhiteSpace(request.Location))
                {
                    return ApiResult<IEnumerable<WeatherForecast>>.Failure("地點不能為空");
                }

                // 使用請求的時間作為起始日期，如果沒有提供則使用當前時間
                DateTime startDate = request.RequestTime ?? DateTime.Now;

                // 產生五天天氣預報
                var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(startDate.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                    Location = request.Location
                })
                .ToArray();

                _logger.LogInformation("為地點 '{Location}' 在時間 '{RequestTime}' 產生天氣預報",
                    request.Location, startDate);

                return ApiResult<IEnumerable<WeatherForecast>>.Success(forecasts, "成功取得天氣預報");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得天氣預報時發生錯誤");
                return ApiResult<IEnumerable<WeatherForecast>>.Failure("取得天氣預報時發生錯誤");
            }
        }
    }

    // 請求模型
    public class WeatherRequest
    {
        public string Location { get; set; } = string.Empty;
        public DateTime? RequestTime { get; set; }
    }

    // API 結果包裝類別
    public class ApiResult<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResult<T> Success(T data, string message = "操作成功")
        {
            return new ApiResult<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResult<T> Failure(string message)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                Message = message,
                Data = default(T)
            };
        }
    }
}
