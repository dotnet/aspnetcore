namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// Describes an RPC call sent from .NET code to Node.js code.
    /// </summary>
    public class NodeInvocationInfo
    {
        /// <summary>
        /// Specifies the path to the Node.js module (i.e., .js file) relative to the project root.
        /// </summary>
        public string ModuleName { get; set; }

        /// <summary>
        /// If set, specifies the name of CommonJS function export to be invoked.
        /// If not set, the Node.js module's default export must itself be a function to be invoked.
        /// </summary>
        public string ExportedFunctionName { get; set; }

        /// <summary>
        /// A sequence of JSON-serializable arguments to be passed to the Node.js function being invoked.
        /// </summary>
        public object[] Args { get; set; }
    }
}
