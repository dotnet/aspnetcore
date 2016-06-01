using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.NodeServices
{
    public static class Configuration
    {
        public const NodeHostingModel DefaultNodeHostingModel = NodeHostingModel.Http;

        private static readonly string[] DefaultWatchFileExtensions = {".js", ".jsx", ".ts", ".tsx", ".json", ".html"};
        private static readonly NodeServicesOptions DefaultOptions = new NodeServicesOptions
        {
            HostingModel = DefaultNodeHostingModel,
            WatchFileExtensions = DefaultWatchFileExtensions
        };

        public static void AddNodeServices(this IServiceCollection serviceCollection)
            => AddNodeServices(serviceCollection, DefaultOptions);

        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeServicesOptions options)
        {
            serviceCollection.AddSingleton(typeof(INodeServices), serviceProvider =>
            {
                var hostEnv = serviceProvider.GetRequiredService<IHostingEnvironment>();
                if (string.IsNullOrEmpty(options.ProjectPath))
                {
                    options.ProjectPath = hostEnv.ContentRootPath;
                }

                return CreateNodeServices(options);
            });
        }

        public static INodeServices CreateNodeServices(NodeServicesOptions options)
        {
            var watchFileExtensions = options.WatchFileExtensions ?? DefaultWatchFileExtensions;
            switch (options.HostingModel)
            {
                case NodeHostingModel.Http:
                    return new HttpNodeInstance(options.ProjectPath, /* port */ 0, watchFileExtensions);
                case NodeHostingModel.Socket:
                    return new SocketNodeInstance(options.ProjectPath, watchFileExtensions);
                default:
                    throw new ArgumentException("Unknown hosting model: " + options.HostingModel);
            }
        }
    }
}