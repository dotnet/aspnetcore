using System;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// Represents an exception caused by invoking Node.js code.
    /// </summary>
    public class NodeInvocationException : Exception
    {
        /// <summary>
        /// If true, indicates that the invocation failed because the Node.js instance could not be reached. For example,
        /// it might have already shut down or previously crashed.
        /// </summary>
        public bool NodeInstanceUnavailable { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="NodeInvocationException"/>.
        /// </summary>
        /// <param name="message">A description of the exception.</param>
        /// <param name="details">Additional information, such as a Node.js stack trace, representing the exception.</param>
        public NodeInvocationException(string message, string details)
            : base(message + Environment.NewLine + details)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="NodeInvocationException"/>.
        /// </summary>
        /// <param name="message">A description of the exception.</param>
        /// <param name="details">Additional information, such as a Node.js stack trace, representing the exception.</param>
        /// <param name="nodeInstanceUnavailable">Specifies a value for the <see cref="NodeInstanceUnavailable"/> flag.</param>
        public NodeInvocationException(string message, string details, bool nodeInstanceUnavailable)
            : this(message, details)
        {
            NodeInstanceUnavailable = nodeInstanceUnavailable;
        }
    }
}