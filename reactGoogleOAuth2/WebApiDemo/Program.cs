
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using WebApiDemo.Services;

namespace WebApiDemo;

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
        app.MapGet("/do-login", async ctx =>
        {
            // �n�J������^��e�ݭ���
            var defaultReturn = "http://localhost:49158/"; // ���a returnUrl �ɪ��w�]�^��
            var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = "/login"
            };
            await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);


            //var qsReturn = ctx.Request.Query["returnUrl"].ToString();
            //// ���\�^�����e�ݨӷ��]�קK Open Redirect�^
            //var allowedFrontends = new[] { "http://localhost:49158", "https://localhost:49158" };
            //var defaultReturn = "http://localhost:49158/"; // ���a returnUrl �ɪ��w�]�^��

            //string redirect = defaultReturn;
            //if (!string.IsNullOrWhiteSpace(qsReturn) &&
            //    Uri.TryCreate(qsReturn, UriKind.Absolute, out var uri) &&
            //    allowedFrontends.Any(o => qsReturn.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
            //{
            //    redirect = qsReturn;
            //}

            //var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = redirect };
            //await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
        });

        app.MapGet("/login", async ctx =>
        {
            // ���o cookie ����
            var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Authentication failed.");
                return;
            }
            ctx.User = authResult.Principal;
            // �ϥ� ClaimTypes �`��Ū��
            var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var name = ctx.User.FindFirstValue(ClaimTypes.Name) ?? "";
            var givenName = ctx.User.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var surname = ctx.User.FindFirstValue(ClaimTypes.Surname) ?? "";
            // Email �Y�����ӥ������� ClaimTypes.Email�A�i�A�ƴ��j�M�`������
            var email = ctx.User.FindFirstValue(ClaimTypes.Email)
                             ?? ctx.User.Claims.FirstOrDefault(c =>
                                    c.Type.EndsWith("/email", StringComparison.OrdinalIgnoreCase) ||
                                    c.Type.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase))?.Value
                             ?? "";

            // �w�n�J�G���� SaveTokens = true �~�|�� Token �s�� Cookie ���� AuthenticationProperties
            var accessToken = authResult.Properties?.GetTokenValue("access_token");
            var refreshToken = authResult.Properties?.GetTokenValue("refresh_token");
            var expiresAt = authResult.Properties?.GetTokenValue("expires_at");

            // �n�J������^��e�ݭ���
            var returnUrl = ctx.Request.Query["returnUrl"].ToString();
            var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = "/"
            };
            await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);


            //var qsReturn = ctx.Request.Query["returnUrl"].ToString();
            //// ���\�^�����e�ݨӷ��]�קK Open Redirect�^
            //var allowedFrontends = new[] { "http://localhost:49158", "https://localhost:49158" };
            //var defaultReturn = "http://localhost:49158/"; // ���a returnUrl �ɪ��w�]�^��

            //string redirect = defaultReturn;
            //if (!string.IsNullOrWhiteSpace(qsReturn) &&
            //    Uri.TryCreate(qsReturn, UriKind.Absolute, out var uri) &&
            //    allowedFrontends.Any(o => qsReturn.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
            //{
            //    redirect = qsReturn;
            //}

            //var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = redirect };
            //await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
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
