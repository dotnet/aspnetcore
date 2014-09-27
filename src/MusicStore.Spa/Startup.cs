using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.StaticFiles;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;

namespace MusicStore.Spa
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = new Configuration()
                .AddJsonFile("Config.json")
                .AddEnvironmentVariables();

            app.UseServices(services =>
            {
                // Add options accessors to the service container
                services.SetupOptions<IdentityDbContextOptions>(options =>
                {
                    options.DefaultAdminUserName = configuration.Get("DefaultAdminUsername");
                    options.DefaultAdminPassword = configuration.Get("DefaultAdminPassword");
                    options.UseSqlServer(configuration.Get("Data:IdentityConnection:ConnectionString"));
                });

                services.SetupOptions<MusicStoreDbContextOptions>(options =>
                    options.UseSqlServer(configuration.Get("Data:DefaultConnection:ConnectionString")));

                // Add MVC services to the service container
                services.AddMvc();

                // Add EF services to the service container
                services.AddEntityFramework()
                    .AddSqlServer();

                // Add Identity services to the services container
                services.AddDefaultIdentity<ApplicationDbContext, ApplicationUser, IdentityRole>(configuration);

                // Add application services to the service container
                services.AddScoped<MusicStoreContext>();
                services.AddTransient(typeof(IHtmlHelper<>), typeof(AngularHtmlHelper<>));
            });

            // Initialize the sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
            SampleData.InitializeIdentityDatabaseAsync(app.ApplicationServices).Wait();

            // Configure the HTTP request pipeline

            // Add cookie auth
            app.UseIdentity();

            // Add static files
            app.UseStaticFiles();

            // Add MVC
            app.UseMvc();
        }
    }
}
