namespace Microsoft.AspNetCore.NodeServices {
    public class NodeInvocationInfo
    {
        public string ModuleName;
        public string ExportedFunctionName;
        public object[] Args;
    }
}
