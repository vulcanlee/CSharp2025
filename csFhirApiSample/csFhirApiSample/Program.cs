
namespace csFhirApiSample
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
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<Services.PatientService>();
            builder.Services.AddScoped<Services.ObservationHeightWeightService>();
            builder.Services.AddScoped<Services.EncounterService>();
            builder.Services.AddScoped<Services.ConditionService>();

			// Configure CORS to allow any origin/method/header (suitable for dev/demo)
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll",
					policy => policy
						.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader());
			});

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            // ✅ CORS 應該放在這裡 - 在 UseRouting 之後、UseAuthorization 之前
            app.UseCors("AllowAll");
            
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
