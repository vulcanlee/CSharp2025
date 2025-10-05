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
        var isDev = builder.Environment.IsDevelopment();

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        #region ���U�A��
        builder.Services.AddSingleton<Services.ConfigurationService>();
        #endregion

        #region �K�[ CORS �A��
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowReactApp", policy =>
            {
                // �}�o�P�ɤ��\ http/https �e��
                var origins = isDev
                    ? new[] { "http://localhost:49158", "https://localhost:49158" }
                    : new[] { "https://localhost:49158" };

                policy.WithOrigins(origins)
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

                if (isDev)
                {
                    // �}�o���q���\ HTTP�GCookie �b HTTP �]�|�Q�e�X
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax; // �P�� XHR �|�a Cookie
                }
                else
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax; // �������ҫe��ݬҬ� HTTPS
                }

                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";

                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
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
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false; // �קK�� Token ��i Cookie �y���L�j�]�ݭn�ɦA�����A�����x�s�^
            });

        builder.Services.AddAuthorization();
        #endregion

        var app = builder.Build();

        // �u�b�D�}�o���ұj�� HTTPS �ɦV
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseRawHttpLogging();

        app.UseCors("AllowReactApp");

        app.UseAuthentication();
        app.UseAuthorization();

        #region �s���A�Ⱥ��I

        // ���\�^�����e�ݨӷ��]�קK Open Redirect�^
        var allowedFrontends = isDev
            ? new[] { "http://localhost:49158", "https://localhost:49158" }
            : new[] { "https://localhost:49158" };

        var defaultFrontend = isDev ? "http://localhost:49158/" : "https://localhost:49158/";

        app.MapGet("/do-login", async ctx =>
        {
            var qsReturn = ctx.Request.Query["returnUrl"].ToString();
            string frontendReturn = defaultFrontend;
            if (!string.IsNullOrWhiteSpace(qsReturn)
                && Uri.TryCreate(qsReturn, UriKind.Absolute, out _)
                && allowedFrontends.Any(o => qsReturn.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
            {
                frontendReturn = qsReturn;
            }

            var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                RedirectUri = "/login?returnUrl=" + Uri.EscapeDataString(frontendReturn)
            };
            await ctx.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);
        });

        app.MapGet("/login", async ctx =>
        {
            var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authResult.Succeeded || authResult.Principal == null)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Authentication failed.");
                return;
            }

            // �Ȧ^���A���z�L URL �a�Ӹ�/Token
            var qsReturn = ctx.Request.Query["returnUrl"].ToString();
            string redirect = defaultFrontend;
            if (!string.IsNullOrWhiteSpace(qsReturn)
                && Uri.TryCreate(qsReturn, UriKind.Absolute, out _)
                && allowedFrontends.Any(o => qsReturn.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
            {
                redirect = qsReturn;
            }

            ctx.Response.Redirect(redirect, permanent: false);
        });

        app.MapPost("/logout", async ctx =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        app.MapGet("/api/me", async (HttpContext ctx) =>
        {
            var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (authResult.Succeeded && authResult.Principal is ClaimsPrincipal user && user.Identity?.IsAuthenticated == true)
            {
                var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var name = user.FindFirstValue(ClaimTypes.Name) ?? "";
                var givenName = user.FindFirstValue(ClaimTypes.GivenName) ?? "";
                var surname = user.FindFirstValue(ClaimTypes.Surname) ?? "";
                var email = user.FindFirstValue(ClaimTypes.Email)
                             ?? user.Claims.FirstOrDefault(c =>
                                    c.Type.EndsWith("/email", StringComparison.OrdinalIgnoreCase) ||
                                    c.Type.EndsWith("/emailaddress", StringComparison.OrdinalIgnoreCase))?.Value
                             ?? "";

                return Results.Ok(new
                {
                    isAuthenticated = true,
                    id,
                    name,
                    givenName,
                    surname,
                    email
                });
            }
            return Results.Ok(new { isAuthenticated = false });
        });

        app.MapGet("/api/secure/data", [Authorize] () =>
        {
            return Results.Ok(new
            {
                secret = "�u���n�J���H�ݱo�쪺���",
                at = DateTimeOffset.UtcNow
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/api/dev/token", async (HttpContext ctx) =>
            {
                var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!authResult.Succeeded) return Results.Unauthorized();

                var accessToken = authResult.Properties?.GetTokenValue("access_token");
                var expiresAt = authResult.Properties?.GetTokenValue("expires_at");
                return Results.Ok(new { accessToken, expiresAt });
            });
        }
        #endregion

        app.MapControllers();

        app.Run();
    }
}