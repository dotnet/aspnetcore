// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace System.Net.Http
{
    /// <summary>
    /// Extensions methods for <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static class HttpClientFactoryExtensions
    {
        /// <summary>
        /// Creates a new <see cref="HttpClient"/> using the default configuration.
        /// </summary>
        /// <param name="factory">The <see cref="IHttpClientFactory"/>.</param>
        /// <returns>An <see cref="HttpClient"/> configured using the default configuration.</returns>
        public static HttpClient CreateClient(this IHttpClientFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.CreateClient(Options.DefaultName);
        }
    }
}
