// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// Represents an exception caused by invoking Node.js code.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public class NodeInvocationException : Exception
    {
        /// <summary>
        /// If true, indicates that the invocation failed because the Node.js instance could not be reached. For example,
        /// it might have already shut down or previously crashed.
        /// </summary>
        public bool NodeInstanceUnavailable { get; private set; }

        /// <summary>
        /// If true, indicates that even though the invocation failed because the Node.js instance could not be reached
        /// or needs to be restarted, that Node.js instance may remain alive for a period in order to complete any
        /// outstanding requests.
        /// </summary>
        public bool AllowConnectionDraining { get; private set;}

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
        /// <param name="allowConnectionDraining">Specifies a value for the <see cref="AllowConnectionDraining"/> flag.</param>
        public NodeInvocationException(string message, string details, bool nodeInstanceUnavailable, bool allowConnectionDraining)
            : this(message, details)
        {
            // Reject a meaningless combination of flags
            if (allowConnectionDraining && !nodeInstanceUnavailable)
            {
                throw new ArgumentException(
                    $"The '${ nameof(allowConnectionDraining) }' parameter cannot be true " +
                    $"unless the '${ nameof(nodeInstanceUnavailable) }' parameter is also true.");
            }

            NodeInstanceUnavailable = nodeInstanceUnavailable;
            AllowConnectionDraining = allowConnectionDraining;
        }
    }
}
