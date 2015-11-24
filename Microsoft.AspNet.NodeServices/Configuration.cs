using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.NodeServices {
    public static class Configuration {
        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeHostingModel hostingModel = NodeHostingModel.Http) {
            serviceCollection.AddSingleton(typeof(INodeServices), (serviceProvider) => {
                var appEnv = serviceProvider.GetRequiredService<IApplicationEnvironment>();
                return CreateNodeServices(hostingModel, appEnv.ApplicationBasePath);
            });
        }

        public static INodeServices CreateNodeServices(NodeHostingModel hostingModel, string projectPath)
        {
            switch (hostingModel)
            {
                case NodeHostingModel.Http:
                    return new HttpNodeInstance(projectPath);
                case NodeHostingModel.InputOutputStream:
                    return new InputOutputStreamNodeInstance(projectPath);
                default:
                    throw new System.ArgumentException("Unknown hosting model: " + hostingModel.ToString());
            }
        }
    }
}
