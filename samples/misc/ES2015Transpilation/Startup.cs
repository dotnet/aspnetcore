using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ES2015Example
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
            // Add MVC services to the services container.
            services.AddMvc();

            // Enable Node Services
            services.AddNodeServices();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, INodeServices nodeServices)
        {
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
            
            // Dynamically transpile any .js files under the '/js/' directory
            app.Use(next => async context => {
                var requestPath = context.Request.Path.Value;
                if (requestPath.StartsWith("/js/") && requestPath.EndsWith(".js")) {
                    var fileInfo = env.WebRootFileProvider.GetFileInfo(requestPath); 
                    if (fileInfo.Exists) {
                        var transpiled = await nodeServices.Invoke<string>("transpilation.js", fileInfo.PhysicalPath, requestPath);
                        await context.Response.WriteAsync(transpiled);
                        return;
                    }
                }
                
                // Not a JS file, or doesn't exist - let some other middleware handle it
                await next.Invoke(context);
            });
            
            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action?}/{id?}",
                    defaults: new { controller="Home", action = "Index" });
            });
        }
    }
}
