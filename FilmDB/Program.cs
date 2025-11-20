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
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddControllersWithViews();
            builder.Services.AddMemoryCache();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddRateLimiter(options =>{options.AddFixedWindowLimiter("default", opt =>{opt.Window = TimeSpan.FromSeconds(10);opt.PermitLimit = 20;opt.QueueLimit = 0;});});
            builder.Services.AddResponseCaching();

            WebApplication app = builder.Build();

            app.UseSwagger();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwaggerUI();
            }
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.Use(async (context, next) =>
            {
                var ua = context.Request.Headers.UserAgent.ToString();
                if (string.IsNullOrEmpty(ua) || ua.Contains("bot", StringComparison.OrdinalIgnoreCase) || ua.Contains("crawl", StringComparison.OrdinalIgnoreCase)        || ua.Contains("spider", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    return;
                }

                await next();
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseWebSockets();
            app.UseDeveloperExceptionPage();
            app.UseResponseCaching();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.UseRateLimiter();
            app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; script-src 'self' https://filmdb20250207212520.azurewebsites.net https://cdn.jsdelivr.net/npm/chart.js https://cdn.jsdelivr.net/npm/nouislider/distribute/nouislider.min.js https://code.jquery.com  http://localhost aspnetcore-browser-refresh 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';");

                await next();
            });

            app.Run();
        }
    }
}
