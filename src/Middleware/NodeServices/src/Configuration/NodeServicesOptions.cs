// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Describes options used to configure an <see cref="INodeServices"/> instance.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public class NodeServicesOptions
    {
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
            WatchFileExtensions = (string[])DefaultWatchFileExtensions.Clone();

            var hostEnv = serviceProvider.GetService<IWebHostEnvironment>();
            if (hostEnv != null)
            {
                // In an ASP.NET environment, we can use the IHostingEnvironment data to auto-populate a few
                // things that you'd otherwise have to specify manually
                ProjectPath = hostEnv.ContentRootPath;
                EnvironmentVariables["NODE_ENV"] = hostEnv.IsDevelopment() ? "development" : "production"; // De-facto standard values for Node
            }
            else
            {
                ProjectPath = Directory.GetCurrentDirectory();
            }

            var applicationLifetime = serviceProvider.GetService<IHostApplicationLifetime>();
            if (applicationLifetime != null)
            {
                ApplicationStoppingToken = applicationLifetime.ApplicationStopping;
            }

            // If the DI system gives us a logger, use it. Otherwise, set up a default one.
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            NodeInstanceOutputLogger = loggerFactory != null
                ? loggerFactory.CreateLogger(LogCategoryName)
                : NullLogger.Instance;
            // By default, we use this package's built-in out-of-process-via-HTTP hosting/transport
            this.UseHttpHosting();
        }

        /// <summary>
        /// Specifies how to construct Node.js instances. An <see cref="INodeInstance"/> encapsulates all details about
        /// how Node.js instances are launched and communicated with. A new <see cref="INodeInstance"/> will be created
        /// automatically if the previous instance has terminated (e.g., because a source file changed).
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

        /// <summary>
        /// A token that indicates when the host application is stopping.
        /// </summary>
        public CancellationToken ApplicationStoppingToken { get; set; }
    }
}
