// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace System.Net.Http
{
    /// <summary>
    /// Extensions methods for <see cref="IHttpMessageHandlerFactory"/>.
    /// </summary>
    public static class HttpMessageHandlerFactoryExtensions
    {
        /// <summary>
        /// Creates a new <see cref="HttpMessageHandler"/> using the default configuration.
        /// </summary>
        /// <param name="factory">The <see cref="IHttpMessageHandlerFactory"/>.</param>
        /// <returns>An <see cref="HttpMessageHandler"/> configured using the default configuration.</returns>
        public static HttpMessageHandler CreateHandler(this IHttpMessageHandlerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.CreateHandler(Options.DefaultName);
        }
    }
}
