//nuget Microsoft.EntityFrameworkCore
//nuget Microsoft.EntityFrameworkCore.SqlServer
//nuget Microsoft.EntityFrameworkCore.Tools
//appsettings.Json, add:  "DefaultConnection": "Server=JSX2016\\SQLEXPRESS;Database=FilmDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
using FilmDB.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Authentication.ExtendedProtection;

namespace FilmDB
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddControllersWithViews();
            builder.Services.AddMemoryCache();

            // Configure DbContext with SQL query logging
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

                // Enable detailed query logging in Development
                if (builder.Environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging(); // Shows parameter values
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, new[] {
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted
                    }, Microsoft.Extensions.Logging.LogLevel.Information);
                }
            });

            builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("default", opt => { opt.Window = TimeSpan.FromSeconds(10); opt.PermitLimit = 10; opt.QueueLimit = 0; }));
            builder.Services.AddResponseCaching();

            WebApplication app = builder.Build();

            // Apply pending migrations
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }
            app.Use(async (context, next) =>
            {
                var ua = context.Request.Headers.UserAgent.ToString();
                if (string.IsNullOrEmpty(ua) || ua.Contains("bot", StringComparison.OrdinalIgnoreCase) || ua.Contains("crawl", StringComparison.OrdinalIgnoreCase) || ua.Contains("spider", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                await next();
            });

            // Performance monitoring middleware
            app.Use(async (context, next) =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await next();
                sw.Stop();

                // Log slow requests (> 500ms)
                if (sw.ElapsedMilliseconds > 500)
                {
                    Console.WriteLine($"[SLOW REQUEST] {context.Request.Method} {context.Request.Path} - {sw.ElapsedMilliseconds}ms");
                }
                else if (app.Environment.IsDevelopment())
                {
                    Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path} - {sw.ElapsedMilliseconds}ms");
                }
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseResponseCaching();
            app.UseAuthorization();
            app.Use(async (context, next) =>
            {
                // CSP
                var csp = BuildContentSecurityPolicy(app.Environment.IsDevelopment());
                context.Response.Headers.Append("Content-Security-Policy", csp);
                // Security headers
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
                await next();
            });
            app.MapStaticAssets();
            app.UseRateLimiter();
            app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
            app.Run();
        }
        static string BuildContentSecurityPolicy(bool isDevelopment)
        {
            var scriptSources = new List<string>
            {
                "'self'",
                "https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.js",
                "https://cdn.jsdelivr.net/npm/nouislider@15.7.1/dist/nouislider.min.js",
                "https://code.jquery.com/jquery-3.7.1.min.js"
            };
            var styleSources = new List<string>
            {
                "'self'",
                "'unsafe-inline'", //check what actually needs this (bootstrap?)
                "https://cdn.jsdelivr.net/npm/nouislider@15.7.1/dist/nouislider.min.css"
            };
            if (isDevelopment)
            {
                scriptSources.Add("http://localhost:*");
                scriptSources.Add("ws://localhost:*");
            }
            var csp = $"default-src 'self'; " +
                      $"script-src {string.Join(" ", scriptSources)}; " +
                      $"style-src {string.Join(" ", styleSources)}; " +
                      $"font-src 'self'; " +
                      $"img-src 'self' data:; " +
                      $"connect-src 'self'" +
                      (isDevelopment ? " ws://localhost:* http://localhost:*" : "");
            return csp;
        }
    }
}
