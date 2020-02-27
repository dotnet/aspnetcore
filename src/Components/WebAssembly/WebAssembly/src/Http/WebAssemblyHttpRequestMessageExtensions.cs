// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace System.Net.Http
{
    /// <summary>
    /// Extension methods for configuring an instance of <see cref="HttpRequestMessage"/> with browser-specific options.
    /// </summary>
    public static class WebAssemblyHttpRequestMessageExtensions
    {
        /// <summary>
        /// Configures a value for the 'credentials' option for the HTTP request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestCredentials">The <see cref="RequestCredentials"/> option.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        /// <remarks>
        /// See https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials
        /// </remarks>
        public static HttpRequestMessage SetRequestCredentials(this HttpRequestMessage requestMessage, RequestCredentials requestCredentials)
        {
            if (requestMessage is null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            var stringOption = requestCredentials switch
            {
                RequestCredentials.Omit => "omit",
                RequestCredentials.SameOrigin => "same-origin",
                RequestCredentials.Include => "include",
                _ => throw new InvalidOperationException($"Unsupported enum value {requestCredentials}.")
            };

            return SetFetchOption(requestMessage, "credentials", stringOption);
        }

        /// <summary>
        /// Configures a value for the 'cache' option for the HTTP request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestCache">The <see cref="RequestCache"/> option.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>\
        /// <remarks>
        /// See https://developer.mozilla.org/en-US/docs/Web/API/Request/cache
        /// </remarks>
        public static HttpRequestMessage SetRequestCache(this HttpRequestMessage requestMessage, RequestCache requestCache)
        {
            if (requestMessage is null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            var stringOption = requestCache switch
            {
                RequestCache.Default => "default",
                RequestCache.NoStore => "no-store",
                RequestCache.Reload => "reload",
                RequestCache.NoCache => "no-cache",
                RequestCache.ForceCache => "force-cache",
                RequestCache.OnlyIfCached => "only-if-cached",
                _ => throw new InvalidOperationException($"Unsupported enum value {requestCache}.")
            };

            return SetFetchOption(requestMessage, "cache", stringOption);
        }

        /// <summary>
        /// Configures a value for the 'mode' option for the HTTP request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestMode">The <see cref="RequestMode"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>\
        /// <remarks>
        /// See https://developer.mozilla.org/en-US/docs/Web/API/Request/mode
        /// </remarks>
        public static HttpRequestMessage SetRequestMode(this HttpRequestMessage requestMessage, RequestMode requestMode)
        {
            if (requestMessage is null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            var stringOption = requestMode switch
            {
                RequestMode.SameOrigin => "same-origin",
                RequestMode.NoCors => "no-cors",
                RequestMode.Cors => "cors",
                RequestMode.Navigate => "navigate",
                _ => throw new InvalidOperationException($"Unsupported enum value {requestMode}.")
            };

            return SetFetchOption(requestMessage, "mode", stringOption);
        }

        /// <summary>
        /// Configures a value for the 'integrity' option for the HTTP request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="integrity">The subresource integrity descriptor.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        public static HttpRequestMessage SetIntegrity(this HttpRequestMessage requestMessage, string integrity)
            => SetFetchOption(requestMessage, "integrity", integrity);

        /// <summary>
        /// Configures a value for the HTTP request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="name">The name of the HTTP fetch option.</param>
        /// <param name="value">The value, which must be JSON-serializable.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        /// <remarks>
        /// See https://developer.mozilla.org/en-US/docs/Web/API/WindowOrWorkerGlobalScope/fetch
        /// </remarks>
        public static HttpRequestMessage SetFetchOption(this HttpRequestMessage requestMessage, string name, object value)
        {
            if (requestMessage is null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            const string FetchRequestOptionsKey = "FetchRequestOptions";
            IDictionary<string, object> fetchOptions;

            if (requestMessage.Properties.TryGetValue(FetchRequestOptionsKey, out var entry))
            {
                fetchOptions = (IDictionary<string, object>)entry;
            }
            else
            {
                fetchOptions = new Dictionary<string, object>();
                requestMessage.Properties[FetchRequestOptionsKey] = fetchOptions;
            }

            fetchOptions[name] = value;

            return requestMessage;
        }

        /// <summary>
        /// Configures streaming response for the HTTP request.
        /// </summary>
        /// <param name="requestMessage">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="streamingEnabled"><see langword="true"> if streaming is enabled; otherwise false.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        /// <remarks>
        /// This API is only effective when the browser HTTP Fetch supports streaming.
        /// See https://developer.mozilla.org/en-US/docs/Web/API/ReadableStream.
        /// </remarks>
        public static HttpRequestMessage SetStreamingEnabled(this HttpRequestMessage requestMessage, bool streamingEnabled)
        {
            if (requestMessage is null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            requestMessage.Properties["StreamingEnabled"] = streamingEnabled;

            return requestMessage;
        }
    }
}
