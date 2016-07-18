using System;
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
    }
}