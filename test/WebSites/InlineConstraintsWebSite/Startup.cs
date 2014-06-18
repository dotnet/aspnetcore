using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace InlineConstraints
{
    public class Startup
    {
        public Action<IRouteBuilder> RouteCollectionProvider { get; set; }

        public void Configure(IBuilder app)
        {
            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc();

                // Add a custom assembly provider so that we add only controllers present in 
                // this assembly.
                services.AddTransient<IControllerAssemblyProvider, TestControllerAssemblyProvider>();
            });

            var config = new Configuration();
            config.AddEnvironmentVariables();
            var commandLineBuilder = app.ApplicationServices.GetService<ICommandLineArgumentBuilder>();
            string appConfigPath;
            if (config.TryGet("AppConfigPath", out appConfigPath))
            {
                config.AddJsonFile(appConfigPath);
            }
            else if (commandLineBuilder != null)
            {
                var args = commandLineBuilder.Build();
                config.AddCommandLine(args.ToArray());
            }
            else
            {
                var basePath = app.ApplicationServices.GetService<IApplicationEnvironment>().ApplicationBasePath;
                config.AddJsonFile(Path.Combine(basePath, @"App_Data\config.json"));
            }

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
                        {
                            foreach (var item in GetDataFromConfig(config))
                            {
                                routes.MapRoute(item.RouteName, item.RouteTemplateValue);
                            }
                        });
        }

        private IEnumerable<RouteConfigData> GetDataFromConfig(IConfiguration config)
        {
            foreach (var template in config.GetSubKey("TemplateCollection").GetSubKeys())
            {
                yield return 
                    new RouteConfigData()
                    {
                        RouteName = template.Key,
                        RouteTemplateValue = template.Value.Get("TemplateValue")
                    };
            }
        }

        private class RouteConfigData
        {
            public string RouteName { get; set; }
            public string RouteTemplateValue { get; set; }
        }
    }
}
