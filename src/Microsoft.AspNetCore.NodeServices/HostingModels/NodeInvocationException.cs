using System;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    public class NodeInvocationException : Exception
    {
        public bool NodeInstanceUnavailable { get; private set; }

        public NodeInvocationException(string message, string details)
            : base(message + Environment.NewLine + details)
        {
        }

        public NodeInvocationException(string message, string details, bool nodeInstanceUnavailable)
            : this(message, details)
        {
            NodeInstanceUnavailable = nodeInstanceUnavailable;
        }
    }
}