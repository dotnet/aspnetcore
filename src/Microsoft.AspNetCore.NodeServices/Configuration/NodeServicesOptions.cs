using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Describes options used to configure an <see cref="INodeServices"/> instance.
    /// </summary>
    public class NodeServicesOptions
    {
        /// <summary>
        /// Defines the default <see cref="NodeHostingModel"/>.
        /// </summary>
        public const NodeHostingModel DefaultNodeHostingModel = NodeHostingModel.Http;

        internal const string TimeoutConfigPropertyName = nameof(InvocationTimeoutMilliseconds);
        private const int DefaultInvocationTimeoutMilliseconds = 60 * 1000;
        private const string LogCategoryName = "Microsoft.AspNetCore.NodeServices";
        private static readonly string[] DefaultWatchFileExtensions = { ".js", ".jsx", ".ts", ".tsx", ".json", ".html" };

        /// <summary>
        /// Creates a new instance of <see cref="NodeServicesOptions"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
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

        /// <summary>
        /// Specifies which <see cref="NodeHostingModel"/> should be used.
        /// </summary>
        public NodeHostingModel HostingModel { get; set; }

        /// <summary>
        /// If set, this callback function will be invoked to supply the <see cref="INodeServices"/> instance.
        /// </summary>
        public Func<INodeInstance> NodeInstanceFactory { get; set; }

        /// <summary>
        /// If set, overrides the path to the root of your application. This path is used when locating Node.js modules relative to your project.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// If set, the Node.js instance should restart when any matching file on disk within your project changes.
        /// </summary>
        public string[] WatchFileExtensions { get; set; }

        /// <summary>
        /// The Node.js instance's stdout/stderr will be redirected to this <see cref="ILogger"/>.
        /// </summary>
        public ILogger NodeInstanceOutputLogger { get; set; }

        /// <summary>
        /// If true, the Node.js instance will accept incoming V8 debugger connections (e.g., from node-inspector).
        /// </summary>
        public bool LaunchWithDebugging { get; set; }

        /// <summary>
        /// If <see cref="LaunchWithDebugging"/> is true, the Node.js instance will listen for V8 debugger connections on this port.
        /// </summary>
        public int DebuggingPort { get; set; }

        /// <summary>
        /// If set, starts the Node.js instance with the specified environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Specifies the maximum duration, in milliseconds, that your .NET code should wait for Node.js RPC calls to return.
        /// </summary>
        public int InvocationTimeoutMilliseconds { get; set; }
    }
}