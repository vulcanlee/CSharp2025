using csBlazorGoogleOAuth2.Components;
using csBlazorGoogleOAuth2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace csBlazorGoogleOAuth2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            #region 修正

            builder.Services.AddSingleton<ConfigurationService>();

            // 取得注入的ConfigurationService物件
            var configService = builder.Services.BuildServiceProvider().GetRequiredService<ConfigurationService>();
            // 2) AuthN / AuthZ
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; // 預設以 Google 發起挑戰
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/denied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7); // 可依需求調整
            })
            .AddGoogle(options =>
            {
                options.ClientId = configService.Id;
                options.ClientSecret = configService.PW;
                // 預設 CallbackPath = /signin-google，可不設
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.SaveTokens = true; // 存 access_token / refresh_token（如有）到 auth properties

                // 可把 Google 回傳的 JSON 欄位映射到 Claims
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            });

            builder.Services.AddAuthorization();

            // 3) 讓 Blazor 能取得 AuthenticationState（AuthorizeView 會用到）
            builder.Services.AddCascadingAuthenticationState();
            #endregion

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();

            #region 修正
            // Auth Middlewares
            app.UseAuthentication();
            app.UseAuthorization();

            // 4) 簡單的登入/登出端點
            app.MapGet("/login", async context =>
            {
                // 觸發外部登入挑戰，登入成功後導回原頁或首頁
                var returnUrl = context.Request.Query["returnUrl"].ToString();
                if (string.IsNullOrEmpty(returnUrl)) returnUrl = "/";
                var props = new AuthenticationProperties { RedirectUri = returnUrl };
                await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
            });

            app.MapPost("/logout", async context =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Redirect("/");
            });

            app.MapGet("/denied", () => Results.Content("Access Denied"));

            #endregion

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
