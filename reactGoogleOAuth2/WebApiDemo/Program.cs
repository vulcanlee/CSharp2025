
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

            #region 註冊服務
            builder.Services.AddSingleton<Services.ConfigurationService>();
            #endregion

            #region 添加 CORS 服務
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:49158") // React 應用運行在此端口
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            #endregion

            #region 認證與授權服務註冊
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
                    options.Cookie.SameSite = SameSiteMode.Lax; // 若跨網域回跳需要，改 None 並全程 HTTPS
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    // 重要：讓外部登入流程可保存 tokens (可選)
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = ctx =>
                        {
                            // 對 API 請求返回 401 而不是 302
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
                    options.CallbackPath = "/signin-google"; // 要和 Google 控制台一致
                    options.SaveTokens = true;               // 如需後續調用 Google API 可保留
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

            // 放在較前位置以攔截完整流程
            app.UseRawHttpLogging();

            #region 使用 CORS 中介軟體 - 必須放在管道的早期位置
            app.UseCors("AllowReactApp");
            #endregion

            app.UseAuthentication();
            app.UseAuthorization();

            #region 新的服務端點
            app.MapGet("/login", async ctx =>
            {
                // 登入完成後回到前端首頁
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

            // 公開端點：檢查目前登入狀態
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

            // 受保護端點：必須登入
            app.MapGet("/api/secure/data", [Authorize] () =>
            {
                return Results.Ok(new
                {
                    secret = "只有登入的人看得到的資料",
                    at = DateTimeOffset.UtcNow
                });
            });
            #endregion

            app.MapControllers();

            app.Run();
        }
    }
}
