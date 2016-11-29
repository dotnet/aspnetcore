namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Represents a way of creating and invoking code in a Node.js environment.
    /// </summary>
    public enum NodeHostingModel
    {
        /// <summary>
        /// An out-of-process Node.js instance where RPC calls are made via HTTP.
        /// </summary>
        Http,

        /// <summary>
        /// An out-of-process Node.js instance where RPC calls are made over binary sockets.
        /// </summary>
        Socket,
    }
}
