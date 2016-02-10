using System;
using System.Collections.Generic;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
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
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SiteSettings>(settings =>
            {
                settings.DefaultAdminUsername = Configuration["DefaultAdminUsername"];
                settings.DefaultAdminPassword = Configuration["DefaultAdminPassword"];
            });

            // Add MVC services to the services container.
            services.AddMvc();

            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();
            
            // Add EF services to the service container
            services.AddEntityFramework()
                .AddSqlite()
                .AddDbContext<MusicStoreContext>(options => {
                    options.UseSqlite(Configuration["DbConnectionString"]);
                });

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders();

            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();

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

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Initialize the sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
            
            loggerFactory.MinimumLevel = LogLevel.Warning;
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            // Configure the HTTP request pipeline.

            // Add the platform handler to the request pipeline.
            app.UseIISPlatformHandler();

            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
            }
            
            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                // Matches requests that correspond to an existent controller/action pair
                routes.MapRoute("default", "{controller}/{action}/{id:int?}");
                
                // Matches any other request that doesn't appear to have a filename extension (defined as 'having a dot in the last URI segment').
                // This means you'll correctly get 404s for /some/dir/non-existent-image.png instead of returning the SPA HTML.
                // However, it means requests like /customers/isaac.newton will *not* be mapped into the SPA, so if you need to accept
                // URIs like that you'll need to match all URIs, e.g.:
                //    routes.MapRoute("spa-fallback", "{*anything}", new { controller = "Home", action = "Index" });
                // (which of course will match /customers/isaac.png too, so in that case it would serve the PNG image at that URL if one is on disk,
                // or the SPA HTML if not).
                routes.MapSpaFallbackRoute("spa-fallback", new { controller = "Home", action = "Index" });

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });
        }
    }
}
