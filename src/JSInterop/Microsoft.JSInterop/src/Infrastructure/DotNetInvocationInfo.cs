// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop.Infrastructure
{
    /// <summary>
    /// Information about a JSInterop call from JavaScript to .NET.
    /// </summary>
    public readonly struct DotNetInvocationInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DotNetInvocationInfo"/>.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly containing the method.</param>
        /// <param name="methodIdentifier">The identifier of the method to be invoked.</param>
        /// <param name="dotNetObjectId">The object identifier for instance method calls.</param>
        /// <param name="callId">The call identifier.</param>
        public DotNetInvocationInfo(string assemblyName, string methodIdentifier, long dotNetObjectId, string callId)
        {
            CallId = callId;
            AssemblyName = assemblyName;
            MethodIdentifier = methodIdentifier;
            DotNetObjectId = dotNetObjectId;
        }

        /// <summary>
        /// Gets the name of the assembly containing the method.
        /// Only one of <see cref="DotNetObjectId"/> or <see cref="AssemblyName"/> may be specified.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets the identifier of the method to be invoked. This is the value specified in the <see cref="JSInvokableAttribute"/>.
        /// </summary>
        public string MethodIdentifier { get; }

        /// <summary>
        /// Gets the object identifier for instance method calls.
        /// Only one of <see cref="DotNetObjectId"/> or <see cref="AssemblyName"/> may be specified.
        /// </summary>
        public long DotNetObjectId { get; }

        /// <summary>
        /// Gets the call identifier. This value is <see langword="null"/> when the client does not expect a value to be returned.
        /// </summary>
        public string CallId { get; }
    }
}
