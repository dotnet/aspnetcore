// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// <para>
    /// An interface for components that can create a middleware-like delegate from an <see cref="Endpoint"/>.
    /// </para>
    /// <para>
    /// Implementations registered in the application services using the service type <see cref="IHandlerFactory"/>
    /// will be automatically added to set of handler factories used by the default dispatcher.
    /// </para>
    /// </summary>
    public interface IHandlerFactory
    {
        /// <summary>
        /// Creates a middleware-like delegate for the provided <see cref="Endpoint"/>.
        /// </summary>
        /// <param name="endpoint">The <see cref="Endpoint"/> that will execute for the current request.</param>
        /// <returns>An <see cref="Func{RequestDelegate, RequestDelegate}"/> or <c>null</c>.</returns>
        Func<RequestDelegate, RequestDelegate> CreateHandler(Endpoint endpoint);
    }
}
