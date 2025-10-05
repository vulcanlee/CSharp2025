
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using WebApiDemo.Services;

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

            #region ���U�A��
            builder.Services.AddSingleton<Services.ConfigurationService>();
            #endregion

            #region �K�[ CORS �A��
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:49158") // React ���ιB��b���ݤf
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            #endregion

            #region �{�һP���v�A�ȵ��U
            var configService = builder.Services.BuildServiceProvider().GetRequiredService<ConfigurationService>();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "bff_auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax; // �Y�����^���ݭn�A�� None �å��{ HTTPS
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    // ���n�G���~���n�J�y�{�i�O�s tokens (�i��)
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = ctx =>
                        {
                            // �� API �ШD��^ 401 �Ӥ��O 302
                            if (ctx.Request.Path.StartsWithSegments("/api"))
                            {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return Task.CompletedTask;
                            }
                            ctx.Response.Redirect(ctx.RedirectUri);
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = configService.ClientId;
                    options.ClientSecret = configService.ClientSecret;
                    options.CallbackPath = "/signin-google"; // �n�M Google ����x�@�P
                    options.SaveTokens = true;               // �p�ݫ���ե� Google API �i�O�d
                });

            builder.Services.AddAuthorization();
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

            app.UseAuthentication();
            app.UseAuthorization();

            #region �s���A�Ⱥ��I
            app.MapGet("/login", async ctx =>
            {
                // �n�J������^��e�ݭ���
                var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/"
                };
                await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
            });

            app.MapPost("/logout", async ctx =>
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            });

            // ���}���I�G�ˬd�ثe�n�J���A
            app.MapGet("/api/me", (HttpContext ctx) =>
            {
                if (ctx.User?.Identity?.IsAuthenticated == true)
                {
                    var name = ctx.User.Identity?.Name ?? "";
                    var email = ctx.User.Claims.FirstOrDefault(c => c.Type.Contains("email"))?.Value ?? "";
                    return Results.Ok(new { isAuthenticated = true, name, email });
                }
                return Results.Ok(new { isAuthenticated = false });
            });

            // ���O�@���I�G�����n�J
            app.MapGet("/api/secure/data", [Authorize] () =>
            {
                return Results.Ok(new
                {
                    secret = "�u���n�J���H�ݱo�쪺���",
                    at = DateTimeOffset.UtcNow
                });
            });
            #endregion

            app.MapControllers();

            app.Run();
        }
    }
}
