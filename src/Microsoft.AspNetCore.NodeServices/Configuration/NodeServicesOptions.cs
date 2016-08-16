using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeServicesOptions
    {
        public const NodeHostingModel DefaultNodeHostingModel = NodeHostingModel.Http;

        private static readonly string[] DefaultWatchFileExtensions = { ".js", ".jsx", ".ts", ".tsx", ".json", ".html" };

        public NodeServicesOptions()
        {
            HostingModel = DefaultNodeHostingModel;
            WatchFileExtensions = (string[])DefaultWatchFileExtensions.Clone();
        }
        public Action<System.Diagnostics.ProcessStartInfo> OnBeforeStartExternalProcess { get; set; }
        public NodeHostingModel HostingModel { get; set; }
        public Func<INodeInstance> NodeInstanceFactory { get; set; }
        public string ProjectPath { get; set; }
        public string[] WatchFileExtensions { get; set; }
        public ILogger NodeInstanceOutputLogger { get; set; }
        public bool LaunchWithDebugging { get; set; }
        public IDictionary<string, string> EnvironmentVariables { get; set; }
        public int? DebuggingPort { get; set; }

        public NodeServicesOptions AddDefaultEnvironmentVariables(bool isDevelopmentMode)
        {
            if (EnvironmentVariables == null)
            {
                EnvironmentVariables = new Dictionary<string, string>();
            }

            if (!EnvironmentVariables.ContainsKey("NODE_ENV"))
            {
                // These strings are a de-facto standard in Node
                EnvironmentVariables["NODE_ENV"] = isDevelopmentMode ? "development" : "production";
            }

            return this;
        }
    }
}