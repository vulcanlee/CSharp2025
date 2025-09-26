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
                // ���ҿ�J���
                if (string.IsNullOrWhiteSpace(request.Location))
                {
                    return ApiResult<IEnumerable<WeatherForecast>>.Failure("�a�I���ର��");
                }

                // �ϥνШD���ɶ��@���_�l����A�p�G�S�����ѫh�ϥη�e�ɶ�
                DateTime startDate = request.RequestTime ?? DateTime.Now;

                // ���ͤ��ѤѮ�w��
                var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(startDate.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                    Location = request.Location
                })
                .ToArray();

                _logger.LogInformation("���a�I '{Location}' �b�ɶ� '{RequestTime}' ���ͤѮ�w��",
                    request.Location, startDate);

                return ApiResult<IEnumerable<WeatherForecast>>.Success(forecasts, "���\���o�Ѯ�w��");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���o�Ѯ�w���ɵo�Ϳ��~");
                return ApiResult<IEnumerable<WeatherForecast>>.Failure("���o�Ѯ�w���ɵo�Ϳ��~");
            }
        }
    }

    // �ШD�ҫ�
    public class WeatherRequest
    {
        public string Location { get; set; } = string.Empty;
        public DateTime? RequestTime { get; set; }
    }

    // API ���G�]�����O
    public class ApiResult<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResult<T> Success(T data, string message = "�ާ@���\")
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
