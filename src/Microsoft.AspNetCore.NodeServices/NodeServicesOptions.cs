namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeServicesOptions
    {
        public NodeServicesOptions()
        {
            HostingModel = NodeHostingModel.Http;
        }

        public NodeHostingModel HostingModel { get; set; }
        public string ProjectPath { get; set; }
        public string[] WatchFileExtensions { get; set; }
    }
}