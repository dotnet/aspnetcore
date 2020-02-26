// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
