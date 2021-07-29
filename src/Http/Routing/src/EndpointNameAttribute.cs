 // Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
 
 namespace Microsoft.AspNetCore.Routing
 {
    /// <summary>
    /// Specifies the endpoint name in Microsoft.AspNetCore.Http.Endpoint.Metadata.
    /// </summary>
    /// <remarks>
    /// Endpoint names must be unique within an application, and can be used to unambiguously
    /// identify a desired endpoint for URI generation using Microsoft.AspNetCore.Routing.LinkGenerator.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
    public sealed class EndpointNameAttribute : Attribute, IEndpointNameMetadata
    {
        /// <summary>
        /// Initializes an instance of the EndpointNameAttribute.
        /// </summary>
        /// <param name="endpointName">The endpoint name.</param>
        public EndpointNameAttribute(string endpointName)
        {
            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }
    
            EndpointName = endpointName;
        }

        /// <summary>
        /// The endpoint name.
        /// </summary>
        public string EndpointName { get; }
    }
 }