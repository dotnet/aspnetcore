using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace InlineConstraints
{
    public class Startup
    {
        public Action<IRouteBuilder> RouteCollectionProvider { get; set; }

        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            configuration.AddEnvironmentVariables();

            var commandLineBuilder = app.ApplicationServices.GetRequiredService<ICommandLineArgumentBuilder>();
            string appConfigPath;
            if (configuration.TryGet("AppConfigPath", out appConfigPath))
            {
                configuration.AddJsonFile(appConfigPath);
            }
            else if (commandLineBuilder != null)
            {
                var args = commandLineBuilder.Build();
                configuration.AddCommandLine(args.ToArray());
            }
            else
            {
                var basePath = app.ApplicationServices.GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;
                configuration.AddJsonFile(Path.Combine(basePath, @"App_Data\config.json"));
            }

            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc(configuration);
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
                        {
                            foreach (var item in GetDataFromConfig(configuration))
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
