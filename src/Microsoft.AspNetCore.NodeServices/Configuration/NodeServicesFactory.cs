using System;
using Microsoft.AspNetCore.NodeServices.HostingModels;

namespace Microsoft.AspNetCore.NodeServices
{
    public static class NodeServicesFactory
    {
        public static INodeServices CreateNodeServices(NodeServicesOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof (options));
            }
            
            return new NodeServicesImpl(() => CreateNodeInstance(options));
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
                switch (options.HostingModel)
                {
                    case NodeHostingModel.Http:
                        return new HttpNodeInstance(options.ProjectPath, options.WatchFileExtensions, options.NodeInstanceOutputLogger, 
                            options.EnvironmentVariables, options.InvocationTimeoutMilliseconds, options.LaunchWithDebugging, options.DebuggingPort, /* port */ 0);
                    case NodeHostingModel.Socket:
                        var pipeName = "pni-" + Guid.NewGuid().ToString("D"); // Arbitrary non-clashing string
                        return new SocketNodeInstance(options.ProjectPath, options.WatchFileExtensions, pipeName, options.NodeInstanceOutputLogger,
                            options.EnvironmentVariables, options.InvocationTimeoutMilliseconds, options.LaunchWithDebugging, options.DebuggingPort);
                    default:
                        throw new ArgumentException("Unknown hosting model: " + options.HostingModel);
                }
            }
        }
    }
}