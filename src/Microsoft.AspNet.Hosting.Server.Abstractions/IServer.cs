// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Hosting.Server
{
    /// <summary>
    /// Represents a server.
    /// </summary>
    public interface IServer : IDisposable
    {
        /// <summary>
        /// A collection of HTTP features of the server.
        /// </summary>
        IFeatureCollection Features { get; }

        /// <summary>
        /// Start the server with the given function that processes an HTTP request.
        /// </summary>
        /// <param name="requestDelegate">A function that processes an HTTP request.</param>
        void Start(RequestDelegate requestDelegate);
    }
}
