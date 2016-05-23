namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeInvocationInfo
    {
        public string ModuleName { get; set; }
        public string ExportedFunctionName { get; set; }
        public object[] Args { get; set; }
    }
}
