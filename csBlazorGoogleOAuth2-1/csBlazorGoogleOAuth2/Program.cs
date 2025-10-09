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
            builder.Services.AddRazorComponents();

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
                options.ClientId = configService.ClientId;
                options.ClientSecret = configService.ClientSecret;
                // 預設 CallbackPath = /signin-google，可不設
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                //options.SaveTokens = true; // 存 access_token / refresh_token（如有）到 auth properties

                // 可把 Google 回傳的 JSON 欄位映射到 Claims
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");

                // 強制每次都重新驗證密碼
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    var sep = context.RedirectUri.Contains('?') ? "&" : "?";
                    context.Response.Redirect($"{context.RedirectUri}{sep}prompt=select_account+consent+login&max_age=0");
                    return Task.CompletedTask;
                };
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
                //var returnUrl = context.Request.Query["returnUrl"].ToString();
                //if (string.IsNullOrEmpty(returnUrl)) returnUrl = "/";
                //var props = new AuthenticationProperties { RedirectUri = returnUrl };
                //await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);

                var returnUrl = context.Request.Query["returnUrl"].ToString();
                if (string.IsNullOrEmpty(returnUrl)) returnUrl = "/";

                var props = new AuthenticationProperties
                {
                    RedirectUri = returnUrl,
                    //Items =
                    //{
                    //    { "prompt", "consent" },  // 強制顯示同意畫面和帳號選擇
                    //    { "max_age", "0" }         // 強制重新驗證
                    //}
                };

                await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, props);

            });

            app.MapPost("/logout", async context =>
            {
                //// 1. 登出本地 Cookie
                //await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                //// 2. 重定向到中介頁面
                //context.Response.Redirect("/logout-redirect");


                context.Response.Redirect("/logout-google");
            });

            // 中介頁面：在客戶端清除 Google session
            app.MapGet("/logout-redirect", async context =>
            {
                var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}/";
                var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>登出中...</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            height: 100vh;
                            margin: 0;
                            background: #f5f5f5;
                        }}
                        .container {{
                            text-align: center;
                            background: white;
                            padding: 40px;
                            border-radius: 8px;
                            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                        }}
                        .spinner {{
                            border: 4px solid #f3f3f3;
                            border-top: 4px solid #4285f4;
                            border-radius: 50%;
                            width: 40px;
                            height: 40px;
                            animation: spin 1s linear infinite;
                            margin: 20px auto;
                        }}
                        @keyframes spin {{
                            0% {{ transform: rotate(0deg); }}
                            100% {{ transform: rotate(360deg); }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>登出中</h2>
                        <div class='spinner'></div>
                        <p>正在清除登入狀態，請稍候...</p>
                    </div>
                    <iframe id='logoutFrame' style=''></iframe>
                    <script>
                        // 方法1: 使用 iframe 清除 Google session
                        var iframe = document.getElementById('logoutFrame');
                        iframe.src = 'https://accounts.google.com/Logout';
                        
                        // 等待 2 秒後重定向回首頁
                        setTimeout(function() {{
                            window.location.href = '{returnUrl}';
                        }}, 20000);
                    </script>
                </body>
                </html>";

                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(html);
            });

            // 登出端點 - 方案2: 只登出本地 + 提示
            app.MapPost("/logout-local", async context =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Redirect("/?logout=success");
            });

            // 完全清除 session 的登出（包含 Google）
            app.MapGet("/logout-google", async context =>
            {
                // 先登出本地
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // 重定向到 Google 登出（會在新分頁開啟，然後關閉）

                var html = """
                <!DOCTYPE html>
                <html>
                <head><title>登出中...</title></head>
                <body>
                    <p>正在清除 Google 登入狀態...</p>
                    <button id="openBtn" style="display:none">開啟 Google 登出視窗</button>
                    <script>
                        const url = 'https://accounts.google.com/Logout';
                        let w = window.open(url, 'googleLogout',
                            'popup,noopener,noreferrer,width=520,height=640');

                        // 若被瀏覽器封鎖彈窗，顯示按鈕讓使用者手動點擊開啟（符合使用者手勢）
                        if (!w) {
                            const btn = document.getElementById('openBtn');
                            btn.style.display = 'inline-block';
                            btn.onclick = () => {
                                w = window.open(url, 'googleLogout',
                                    'popup,noopener,noreferrer,width=520,height=640');
                                watch();
                            };
                        } else {
                            watch();
                        }

                        // 偵測彈出視窗關閉後再導回首頁
                        function watch() {
                            const t = setInterval(function () {
                                if (!w || w.closed) {
                                    clearInterval(t);
                                    window.location.href = '/';
                                }
                            }, 500);
                        }
                    </script>
                </body>
                </html>
                """;

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(html);
            });

            app.MapGet("/denied", () => Results.Content("Access Denied"));

            #endregion

            app.MapRazorComponents<App>();

            app.Run();
        }
    }
}
