
namespace WebApiDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            #region 添加 CORS 服務
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:49158") // React 應用運行在此端口
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()
                          ;
                });
            });
            #endregion

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            // 放在較前位置以攔截完整流程
            app.UseRawHttpLogging();

            #region 使用 CORS 中介軟體 - 必須放在管道的早期位置
            app.UseCors("AllowReactApp");
            #endregion

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
