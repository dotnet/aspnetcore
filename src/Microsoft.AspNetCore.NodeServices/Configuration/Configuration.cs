using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.AspNetCore.NodeServices
{
    public static class Configuration
    {
        const string LogCategoryName = "Microsoft.AspNetCore.NodeServices";

        public static void AddNodeServices(this IServiceCollection serviceCollection)
            => AddNodeServices(serviceCollection, new NodeServicesOptions());

        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeServicesOptions options)
        {
            serviceCollection.AddSingleton(typeof(INodeServices), serviceProvider =>
            {
                // Since this instance is being created through DI, we can access the IHostingEnvironment
                // to populate options.ProjectPath if it wasn't explicitly specified.                
                if (string.IsNullOrEmpty(options.ProjectPath))
                {
                    var hostEnv = serviceProvider.GetRequiredService<IHostingEnvironment>();
                    options.ProjectPath = hostEnv.ContentRootPath;
                }

                // Likewise, if no logger was specified explicitly, we should use the one from DI.
                // If it doesn't provide one, CreateNodeInstance will set up a default.
                if (options.NodeInstanceOutputLogger == null)
                {
                    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                    if (loggerFactory != null)
                    {
                        options.NodeInstanceOutputLogger = loggerFactory.CreateLogger(LogCategoryName); 
                    }
                }

                return new NodeServicesImpl(options, () => CreateNodeInstance(options));
            });
        }

        public static INodeServices CreateNodeServices(NodeServicesOptions options)
        {
            return new NodeServicesImpl(options, () => CreateNodeInstance(options));
        }

        private static INodeInstance CreateNodeInstance(NodeServicesOptions options)
        {
            // If you've specified no logger, fall back on a default console logger
            var logger = options.NodeInstanceOutputLogger;
            if (logger == null)
            {
                logger = new ConsoleLogger(LogCategoryName, null, false);
            }

            if (options.NodeInstanceFactory != null)
            {
                // If you've explicitly supplied an INodeInstance factory, we'll use that. This is useful for
                // custom INodeInstance implementations.
                return options.NodeInstanceFactory();
            }
            else
            {
                // Otherwise we'll construct the type of INodeInstance specified by the HostingModel property,
                // which itself has a useful default value.
                switch (options.HostingModel)
                {
                    case NodeHostingModel.Http:
                        return new HttpNodeInstance(options.ProjectPath, options.WatchFileExtensions, logger, 
                            options.LaunchWithDebugging, options.DebuggingPort, /* port */ 0);
                    case NodeHostingModel.Socket:
                        var pipeName = "pni-" + Guid.NewGuid().ToString("D"); // Arbitrary non-clashing string
                        return new SocketNodeInstance(options.ProjectPath, options.WatchFileExtensions, pipeName, logger,
                            options.LaunchWithDebugging, options.DebuggingPort);
                    default:
                        throw new ArgumentException("Unknown hosting model: " + options.HostingModel);
                }
            }
        }
    }
}