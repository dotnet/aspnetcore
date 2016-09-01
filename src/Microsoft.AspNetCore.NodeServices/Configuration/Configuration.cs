using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.NodeServices
{
    public static class Configuration
    {
        const string LogCategoryName = "Microsoft.AspNetCore.NodeServices";

        public static void AddNodeServices(this IServiceCollection serviceCollection)
            => AddNodeServices(serviceCollection, new NodeServicesOptions());

        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeServicesOptions options)
        {
            serviceCollection.AddSingleton(
                typeof(INodeServices),
                serviceProvider => CreateNodeServices(serviceProvider, options));
        }

        public static INodeServices CreateNodeServices(IServiceProvider serviceProvider, NodeServicesOptions options)
        {
            return new NodeServicesImpl(() => CreateNodeInstance(serviceProvider, options));
        }

        private static INodeInstance CreateNodeInstance(IServiceProvider serviceProvider, NodeServicesOptions options)
        {
            if (options.NodeInstanceFactory != null)
            {
                // If you've explicitly supplied an INodeInstance factory, we'll use that. This is useful for
                // custom INodeInstance implementations.
                return options.NodeInstanceFactory();
            }
            else
            {
                // Otherwise we'll construct the type of INodeInstance specified by the HostingModel property
                // (which itself has a useful default value), plus obtain config information from the DI system.
                var projectPath = options.ProjectPath;
                var envVars = options.EnvironmentVariables == null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(options.EnvironmentVariables);

                var hostEnv = serviceProvider.GetService<IHostingEnvironment>();
                if (hostEnv != null)
                {
                    // In an ASP.NET environment, we can use the IHostingEnvironment data to auto-populate a few
                    // things that you'd otherwise have to specify manually
                    if (string.IsNullOrEmpty(projectPath))
                    {
                        projectPath = hostEnv.ContentRootPath;
                    }

                    // Similarly, we can determine the 'is development' value from the hosting environment
                    if (!envVars.ContainsKey("NODE_ENV"))
                    {
                        // These strings are a de-facto standard in Node
                        envVars["NODE_ENV"] = hostEnv.IsDevelopment() ? "development" : "production";
                    }
                }

                // If no logger was specified explicitly, we should use the one from DI.
                // If it doesn't provide one, we'll set up a default one.
                var logger = options.NodeInstanceOutputLogger;
                if (logger == null)
                {
                    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                    logger = loggerFactory != null
                        ? loggerFactory.CreateLogger(LogCategoryName)
                        : new ConsoleLogger(LogCategoryName, null, false);
                }

                switch (options.HostingModel)
                {
                    case NodeHostingModel.Http:
                        return new HttpNodeInstance(projectPath, options.WatchFileExtensions, logger, 
                            envVars, options.LaunchWithDebugging, options.DebuggingPort, /* port */ 0);
                    case NodeHostingModel.Socket:
                        var pipeName = "pni-" + Guid.NewGuid().ToString("D"); // Arbitrary non-clashing string
                        return new SocketNodeInstance(projectPath, options.WatchFileExtensions, pipeName, logger,
                            envVars, options.LaunchWithDebugging, options.DebuggingPort);
                    default:
                        throw new ArgumentException("Unknown hosting model: " + options.HostingModel);
                }
            }
        }
    }
}