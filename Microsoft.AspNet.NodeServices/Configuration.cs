using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.NodeServices {
    public static class Configuration {
        public static void AddNodeServices(this IServiceCollection serviceCollection, NodeHostingModel hostingModel = NodeHostingModel.Http) {
            serviceCollection.AddSingleton(typeof(INodeServices), (serviceProvider) => {
                return CreateNodeServices(hostingModel);
            });
        }

        private static INodeServices CreateNodeServices(NodeHostingModel hostingModel)
        {
            switch (hostingModel)
            {
                case NodeHostingModel.Http:
                    return new HttpNodeInstance();
                case NodeHostingModel.InputOutputStream:
                    return new InputOutputStreamNodeInstance();
                default:
                    throw new System.ArgumentException("Unknown hosting model: " + hostingModel.ToString());
            }
        }
    }
}
