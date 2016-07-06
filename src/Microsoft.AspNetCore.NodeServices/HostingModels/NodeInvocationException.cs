using System;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    public class NodeInvocationException : Exception
    {
        public NodeInvocationException(string message, string details)
            : base(message + Environment.NewLine + details)
        {
        }
    }
}