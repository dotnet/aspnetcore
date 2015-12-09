using System;
using System.Collections.Generic;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Routing;
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
                // Matches any request that doesn't appear to have a filename extension (defined as 'having a dot in the last URI segment').
                // This means you'll correctly get 404s for /some/dir/non-existent-image.png instead of returning the SPA HTML.
                // However, it means requests like /customers/isaac.newton will *not* be mapped into the SPA, so if you need to accept
                // URIs like that you'll need to match all URIs, e.g.:
                //    routes.MapbackRoute("spa-fallback", "{*anything}", new { controller = "Home", action = "Index" });
                // (which of course will match /customers/isaac.png too - maybe that is a real customer name, not a PNG image).
                routes.MapSpaFallbackRoute("spa-fallback", new { controller = "Home", action = "Index" });

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });
        }
    }
    internal static class SpaRouteExtensions {
        private const string ClientRouteTokenName = "clientRoute";

        public static void MapSpaFallbackRoute(this IRouteBuilder routeBuilder, string name, object defaults, object constraints = null, object dataTokens = null) {
            MapSpaFallbackRoute(routeBuilder, name, /* templatePrefix */ (string)null, defaults, constraints, dataTokens);
        }

        public static void MapSpaFallbackRoute(this IRouteBuilder routeBuilder, string name, string templatePrefix, object defaults, object constraints = null, object dataTokens = null)
        {
            var template = CreateRouteTemplate(templatePrefix);

            var constraintsDict = ObjectToDictionary(constraints);
            constraintsDict.Add(ClientRouteTokenName, new SpaRouteConstraint());

            routeBuilder.MapRoute(name, template, defaults, constraintsDict, dataTokens);
        }

        private static string CreateRouteTemplate(string templatePrefix)
        {
            templatePrefix = templatePrefix ?? string.Empty;

            if (templatePrefix.Contains("?")) {
                // TODO: Consider supporting this. The {*clientRoute} part should be added immediately before the '?'
                throw new ArgumentException("SPA fallback route templates don't support querystrings");
            }
            
            if (templatePrefix.Contains("#")) {
                throw new ArgumentException("SPA fallback route templates should not include # characters. The hash part of a URI does not get sent to the server.");
            }
            
            if (templatePrefix != string.Empty && !templatePrefix.EndsWith("/")) {
                templatePrefix += "/";
            }

            return templatePrefix + $"{{*{ ClientRouteTokenName }}}";
        }
        
        private static IDictionary<string, object> ObjectToDictionary(object value)
        {
            return value as IDictionary<string, object> ?? new RouteValueDictionary(value);
        }

        private class SpaRouteConstraint : IRouteConstraint
        {
            public bool Match(HttpContext httpContext, IRouter route, string routeKey, IDictionary<string, object> values, RouteDirection routeDirection)
            {
                var clientRouteValue = (values[ClientRouteTokenName] as string) ?? string.Empty;
                return !HasDotInLastSegment(clientRouteValue);
            }

            private bool HasDotInLastSegment(string uri)
            {
                var lastSegmentStartPos = uri.LastIndexOf('/');
                return uri.IndexOf('.', lastSegmentStartPos + 1) >= 0;
            }
        }
    }
}
