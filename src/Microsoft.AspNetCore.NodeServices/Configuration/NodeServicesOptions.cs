using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeServicesOptions
    {
        public const NodeHostingModel DefaultNodeHostingModel = NodeHostingModel.Http;
        internal const string TimeoutConfigPropertyName = nameof(InvocationTimeoutMilliseconds);
        private const int DefaultInvocationTimeoutMilliseconds = 60 * 1000;
        private const string LogCategoryName = "Microsoft.AspNetCore.NodeServices";
        private static readonly string[] DefaultWatchFileExtensions = { ".js", ".jsx", ".ts", ".tsx", ".json", ".html" };

        public NodeServicesOptions(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof (serviceProvider));
            }

            EnvironmentVariables = new Dictionary<string, string>();
            InvocationTimeoutMilliseconds = DefaultInvocationTimeoutMilliseconds;
            HostingModel = DefaultNodeHostingModel;
            WatchFileExtensions = (string[])DefaultWatchFileExtensions.Clone();

            // In an ASP.NET environment, we can use the IHostingEnvironment data to auto-populate a few
            // things that you'd otherwise have to specify manually
            var hostEnv = serviceProvider.GetService<IHostingEnvironment>();
            if (hostEnv != null)
            {
                ProjectPath = hostEnv.ContentRootPath;
                EnvironmentVariables["NODE_ENV"] = hostEnv.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
            }

            // If the DI system gives us a logger, use it. Otherwise, set up a default one.
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            NodeInstanceOutputLogger = loggerFactory != null
                ? loggerFactory.CreateLogger(LogCategoryName)
                : new ConsoleLogger(LogCategoryName, null, false);
        }

        public NodeHostingModel HostingModel { get; set; }
        public Func<INodeInstance> NodeInstanceFactory { get; set; }
        public string ProjectPath { get; set; }
        public string[] WatchFileExtensions { get; set; }
        public ILogger NodeInstanceOutputLogger { get; set; }
        public bool LaunchWithDebugging { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public int DebuggingPort { get; set; }
        public int InvocationTimeoutMilliseconds { get; set; }
    }
}