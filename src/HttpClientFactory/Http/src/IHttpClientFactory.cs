// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace System.Net.Http
{
    /// <summary>
    /// A factory abstraction for a component that can create <see cref="HttpClient"/> instances with custom
    /// configuration for a given logical name.
    /// </summary>
    /// <remarks>
    /// A default <see cref="IHttpClientFactory"/> can be registered in an <see cref="IServiceCollection"/>
    /// by calling <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient(IServiceCollection)"/>.
    /// The default <see cref="IHttpClientFactory"/> will be registered in the service collection as a singleton.
    /// </remarks>
    public interface IHttpClientFactory
    {
        /// <summary>
        /// Creates and configures an <see cref="HttpClient"/> instance using the configuration that corresponds
        /// to the logical name specified by <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The logical name of the client to create.</param>
        /// <returns>A new <see cref="HttpClient"/> instance.</returns>
        /// <remarks>
        /// <para>
        /// Each call to <see cref="CreateClient(string)"/> is guaranteed to return a new <see cref="HttpClient"/>
        /// instance. It is generally not necessary to dispose of the <see cref="HttpClient"/> as the
        /// <see cref="IHttpClientFactory"/> tracks and disposes resources used by the <see cref="HttpClient"/>.
        /// </para>
        /// <para>
        /// Callers are also free to mutate the returned <see cref="HttpClient"/> instance's public properties
        /// as desired.
        /// </para>
        /// </remarks>
        HttpClient CreateClient(string name);
    }
}
