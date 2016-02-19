// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting.Server
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
        /// Start the server with an application.
        /// </summary>
        /// <param name="application">An instance of <see cref="IHttpApplication{TContext}"/>.</param>
        /// <typeparam name="TContext">The context associated with the application.</typeparam>
        void Start<TContext>(IHttpApplication<TContext> application);
    }
}
