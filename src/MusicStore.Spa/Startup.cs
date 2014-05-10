using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Security;
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
        public void Configure(IBuilder app)
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

                // Add Identity services to the service container
                services.AddIdentity<ApplicationUser>()
                    .AddEntityFramework<ApplicationUser, ApplicationDbContext>()
                    .AddHttpSignIn();

                // Add application services to the service container
                services.AddScoped<MusicStoreContext>();
                services.AddTransient(typeof(IHtmlHelper<>), typeof(AngularHtmlHelper<>));
            });

            // Initialize the sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
            SampleData.InitializeIdentityDatabaseAsync(app.ApplicationServices).Wait();

            // Configure the HTTP request pipeline

            // Add cookie auth
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login")
            });

            // Add static files
            app.UseStaticFiles(new StaticFileOptions { FileSystem = new PhysicalFileSystem("wwwroot") });

            // Add MVC
            app.UseMvc(routes =>
            {
                // TODO: Move these back to attribute routes when they're available
                routes.MapRoute(null, "api/genres/menu", new { controller = "GenresApi", action = "GenreMenuList" });
                routes.MapRoute(null, "api/genres", new { controller = "GenresApi", action = "GenreList" });
                routes.MapRoute(null, "api/genres/{genreId}/albums", new { controller = "GenresApi", action = "GenreAlbums" });
                routes.MapRoute(null, "api/albums/mostPopular", new { controller = "AlbumsApi", action = "MostPopular" });
                routes.MapRoute(null, "api/albums/all", new { controller = "AlbumsApi", action = "All" });
                routes.MapRoute(null, "api/albums/{albumId}", new { controller = "AlbumsApi", action = "Details" });
                routes.MapRoute(null, "{controller}/{action}", new { controller = "Home", action = "Index" });
            });
        }
    }
}
