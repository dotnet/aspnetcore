// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Http
{
    /// <summary>
    /// A factory for creating <see cref="HttpMessageHandler"/> instances for performing HTTP operations in WebAssembly.
    /// </summary>
    public static class WebAssemblyHttpMessageHandlerFactory
    {
        /// <summary>
        /// Creates a new instance of the default <see cref="HttpMessageHandler"/> required to perform HTTP operations in WebAssembly.
        /// <para>
        /// <see cref="HttpClient"/> instances are configured by to use the default <see cref="HttpMessageHandler"/>. This method is useful
        /// when authoring a <see cref="DelegatingHandler"/> that performs additional work before a HTTP request is processed.
        /// </para>
        /// </summary>
        /// <returns>The <see cref="HttpMessageHandler"/>.</returns>
        public static HttpMessageHandler CreateDefault()
        {
            var type = Type.GetType("WebAssembly.Net.Http.HttpClient.WasmHttpMessageHandler, WebAssembly.Net.Http", throwOnError: true);
            return (HttpMessageHandler)Activator.CreateInstance(type);
        }
    }
}
