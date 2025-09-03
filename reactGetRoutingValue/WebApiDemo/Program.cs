
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

            #region �K�[ CORS �A��
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:49158") // React ���ιB��b���ݤf
                          .AllowAnyHeader()
                          .AllowAnyMethod();
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

            // ��b���e��m�H�d�I����y�{
            app.UseRawHttpLogging();

            #region �ϥ� CORS �����n�� - ������b�޹D��������m
            app.UseCors("AllowReactApp");
            #endregion

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
