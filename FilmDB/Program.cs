//nuget Microsoft.EntityFrameworkCore
//nuget Microsoft.EntityFrameworkCore.SqlServer
//nuget Microsoft.EntityFrameworkCore.Tools
//appsettings.Json, add:  "DefaultConnection": "Server=JSX2016\\SQLEXPRESS;Database=FilmDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
using Microsoft.EntityFrameworkCore;
using FilmDB.Data;

namespace FilmDB
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseWebSockets();
            app.UseDeveloperExceptionPage();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(name: "default",pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

            app.Run();
        }
    }
}
