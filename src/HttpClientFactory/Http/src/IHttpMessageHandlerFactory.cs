// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Http
{
    /// <summary>
    /// A factory abstraction for a component that can create <see cref="HttpMessageHandler"/> instances with custom
    /// configuration for a given logical name.
    /// </summary>
    /// <remarks>
    /// A default <see cref="IHttpMessageHandlerFactory"/> can be registered in an <see cref="IServiceCollection"/>
    /// by calling <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient(IServiceCollection)"/>.
    /// The default <see cref="IHttpMessageHandlerFactory"/> will be registered in the service collection as a singleton.
    /// </remarks>
    public interface IHttpMessageHandlerFactory
    {
        /// <summary>
        /// Creates and configures an <see cref="HttpMessageHandler"/> instance using the configuration that corresponds
        /// to the logical name specified by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The logical name of the message handler to create.</param>
        /// <returns>A new <see cref="HttpMessageHandler"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// The default <see cref="IHttpMessageHandlerFactory"/> implementation may cache the underlying
        /// <see cref="HttpMessageHandler"/> instances to improve performance.
        /// </para>
        /// <para>
        /// The default <see cref="IHttpMessageHandlerFactory"/> implementation also manages the lifetime of the
        /// handler created, so disposing of the <see cref="HttpMessageHandler"/> returned by this method may
        /// have no effect.
        /// </para>
        /// </remarks>
        HttpMessageHandler CreateHandler(string name);
    }
}
