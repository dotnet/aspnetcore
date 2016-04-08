using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNet.NodeServices {
    public static class Configuration {
        private readonly static string[] defaultWatchFileExtensions = new[] { ".js", ".jsx", ".ts", ".tsx", ".json", ".html" };
        private readonly static NodeServicesOptions defaultOptions = new NodeServicesOptions {
            HostingModel = NodeHostingModel.Http,
            WatchFileExtensions = defaultWatchFileExtensions
        };

        public static void AddNodeServices(this IServiceCollection serviceCollection) {
            AddNodeServices(serviceCollection, defaultOptions);
        }

        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeServicesOptions options) {
            serviceCollection.AddSingleton(typeof(INodeServices), (serviceProvider) => {
                var hostEnv = serviceProvider.GetRequiredService<IHostingEnvironment>();
                if (string.IsNullOrEmpty(options.ProjectPath)) {
                    options.ProjectPath = hostEnv.ContentRootPath;
                }
                return CreateNodeServices(options);
            });
        }

        public static INodeServices CreateNodeServices(NodeServicesOptions options)
        {
            var watchFileExtensions = options.WatchFileExtensions ?? defaultWatchFileExtensions;
            switch (options.HostingModel)
            {
                case NodeHostingModel.Http:
                    return new HttpNodeInstance(options.ProjectPath, /* port */ 0, watchFileExtensions);
                case NodeHostingModel.InputOutputStream:
                    return new InputOutputStreamNodeInstance(options.ProjectPath);
                default:
                    throw new System.ArgumentException("Unknown hosting model: " + options.HostingModel.ToString());
            }
        }
    }

    public class NodeServicesOptions {
        public NodeHostingModel HostingModel { get; set; }
        public string ProjectPath { get; set; }
        public string[] WatchFileExtensions { get; set; }

        public NodeServicesOptions() {
            this.HostingModel = NodeHostingModel.Http;
        }
    }
}
