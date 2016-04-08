using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using AutoMapper;
using MusicStore.Apis;
using MusicStore.Models;

namespace MusicStore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            
            // Add EF services to the service container
            services.AddEntityFramework()
                .AddEntityFrameworkSqlite()
                .AddDbContext<MusicStoreContext>(options => {
                    options.UseSqlite("Data Source=music-db.sqlite");
                });

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders();

            // Configure Auth
            services.Configure<AuthorizationOptions>(options =>
            {
                options.AddPolicy("app-ManageStore", new AuthorizationPolicyBuilder().RequireClaim("app-ManageStore", "Allowed").Build());
            });

            Mapper.CreateMap<AlbumChangeDto, Album>();
            Mapper.CreateMap<Album, AlbumChangeDto>();
            Mapper.CreateMap<Album, AlbumResultDto>();
            Mapper.CreateMap<AlbumResultDto, Album>();
            Mapper.CreateMap<Artist, ArtistResultDto>();
            Mapper.CreateMap<ArtistResultDto, Artist>();
            Mapper.CreateMap<Genre, GenreResultDto>();
            Mapper.CreateMap<GenreResultDto, Genre>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            
            // Initialize the sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();

            app.UseStaticFiles();
            loggerFactory.AddConsole();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                // Matches requests that correspond to an existent controller/action pair
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                // Matches any other request that doesn't appear to have a filename extension (defined as 'having a dot in the last URI segment').
                // This means you'll correctly get 404s for /some/dir/non-existent-image.png instead of returning the SPA HTML.
                // However, it means requests like /customers/isaac.newton will *not* be mapped into the SPA, so if you need to accept
                // URIs like that you'll need to match all URIs, e.g.:
                //    routes.MapRoute("spa-fallback", "{*anything}", new { controller = "Home", action = "Index" });
                // (which of course will match /customers/isaac.png too, so in that case it would serve the PNG image at that URL if one is on disk,
                // or the SPA HTML if not).
                routes.MapSpaFallbackRoute("spa-fallback", new { controller = "Home", action = "Index" });
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseDefaultHostingConfiguration(args)
                .UseIISPlatformHandlerUrl()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
