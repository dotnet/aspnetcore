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
        public Startup()
        {
            Configuration = new Configuration()
                        .AddJsonFile("Config.json")
                        .AddEnvironmentVariables();
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SiteSettings>(settings =>
            {
                settings.DefaultAdminUsername = Configuration.Get("DefaultAdminUsername");
                settings.DefaultAdminPassword = Configuration.Get("DefaultAdminPassword");
            });

            // Add MVC services to the service container
            services.AddMvc();

            // Add EF services to the service container
            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<MusicStoreContext>(options =>
                {
                    options.UseSqlServer(Configuration.Get("Data:DefaultConnection:ConnectionString"));
                });

            // Add Identity services to the services container
            services.AddDefaultIdentity<MusicStoreContext, ApplicationUser, IdentityRole>(Configuration);

            // Add application services to the service container
            services.AddTransient(typeof(IHtmlHelper<>), typeof(AngularHtmlHelper<>));
        }

        public void Configure(IApplicationBuilder app)
        {
            // Initialize the sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();

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
