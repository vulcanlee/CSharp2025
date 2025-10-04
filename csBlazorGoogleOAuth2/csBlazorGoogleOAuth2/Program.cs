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

            #region �ץ�

            builder.Services.AddSingleton<ConfigurationService>();

            // ���o�`�J��ConfigurationService����
            var configService = builder.Services.BuildServiceProvider().GetRequiredService<ConfigurationService>();
            // 2) AuthN / AuthZ
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; // �w�]�H Google �o�_�D��
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/denied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7); // �i�̻ݨD�վ�
            })
            .AddGoogle(options =>
            {
                options.ClientId = configService.Id;
                options.ClientSecret = configService.PW;
                // �w�] CallbackPath = /signin-google�A�i���]
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.SaveTokens = true; // �s access_token / refresh_token�]�p���^�� auth properties

                // �i�� Google �^�Ǫ� JSON ���M�g�� Claims
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            });

            builder.Services.AddAuthorization();

            // 3) �� Blazor ����o AuthenticationState�]AuthorizeView �|�Ψ�^
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

            #region �ץ�
            // Auth Middlewares
            app.UseAuthentication();
            app.UseAuthorization();

            // 4) ²�檺�n�J/�n�X���I
            app.MapGet("/login", async context =>
            {
                // Ĳ�o�~���n�J�D�ԡA�n�J���\��ɦ^�쭶�έ���
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
