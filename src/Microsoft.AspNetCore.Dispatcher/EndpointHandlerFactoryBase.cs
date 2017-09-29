// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// <para>
    /// Base class for implementations that can create a middleware-like delegate from an <see cref="Endpoint"/>.
    /// </para>
    /// <para>
    /// Implementations registered in the application services using the service type <see cref="EndpointHandlerFactoryBase"/>
    /// will be automatically added to <see cref="DispatcherOptions.HandlerFactories"/>.
    /// </para>
    /// </summary>
    public abstract class EndpointHandlerFactoryBase
    {
        /// <summary>
        /// Creates a middleware-like delegate for the provided <see cref="Endpoint"/>.
        /// </summary>
        /// <param name="endpoint">The <see cref="Endpoint"/> that will execute for the current request.</param>
        /// <returns>An <see cref="Func{RequestDelegate, RequestDelegate}"/> or <c>null</c>.</returns>
        public abstract Func<RequestDelegate, RequestDelegate> CreateHandler(Endpoint endpoint);
    }
}
