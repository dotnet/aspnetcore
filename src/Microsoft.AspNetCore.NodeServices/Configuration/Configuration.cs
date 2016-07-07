using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices.HostingModels;

namespace Microsoft.AspNetCore.NodeServices
{
    public static class Configuration
    {
        public static void AddNodeServices(this IServiceCollection serviceCollection)
            => AddNodeServices(serviceCollection, new NodeServicesOptions());

        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeServicesOptions options)
        {
            serviceCollection.AddSingleton(typeof(INodeServices), serviceProvider =>
            {
                // Since this instance is being created through DI, we can access the IHostingEnvironment
                // to populate options.ProjectPath if it wasn't explicitly specified.
                var hostEnv = serviceProvider.GetRequiredService<IHostingEnvironment>();
                if (string.IsNullOrEmpty(options.ProjectPath))
                {
                    options.ProjectPath = hostEnv.ContentRootPath;
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
                        return new HttpNodeInstance(options.ProjectPath, /* port */ 0, options.WatchFileExtensions);
                    case NodeHostingModel.Socket:
                        var pipeName = "pni-" + Guid.NewGuid().ToString("D"); // Arbitrary non-clashing string
                        return new SocketNodeInstance(options.ProjectPath, options.WatchFileExtensions, pipeName);
                    default:
                        throw new ArgumentException("Unknown hosting model: " + options.HostingModel);
                }
            }
        }
    }
}