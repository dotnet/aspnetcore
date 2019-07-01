// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Blazor.Http
{
    /// <summary>
    /// A browser-compatible implementation of <see cref="HttpMessageHandler"/>
    /// </summary>
    public class WebAssemblyHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>
        /// Gets or sets the default value of the 'credentials' option on outbound HTTP requests.
        /// Defaults to <see cref="FetchCredentialsOption.SameOrigin"/>.
        /// </summary>
        public static FetchCredentialsOption DefaultCredentials { get; set; }
            = FetchCredentialsOption.SameOrigin;

        private static readonly object _idLock = new object();
        private static readonly IDictionary<int, TaskCompletionSource<HttpResponseMessage>> _pendingRequests
            = new Dictionary<int, TaskCompletionSource<HttpResponseMessage>>();
        private static int _nextRequestId = 0;

        /// <summary>
        /// The name of a well-known property that can be added to <see cref="HttpRequestMessage.Properties"/>
        /// to control the arguments passed to the underlying JavaScript <code>fetch</code> API.
        /// </summary>
        public const string FetchArgs = "WebAssemblyHttpMessageHandler.FetchArgs";

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            cancellationToken.Register(() => tcs.TrySetCanceled());

            int id;
            lock (_idLock)
            {
                id = _nextRequestId++;
                _pendingRequests.Add(id, tcs);
            }

            var options = new FetchOptions();
            if (request.Properties.TryGetValue(FetchArgs, out var fetchArgs))
            {
                options.RequestInitOverrides = fetchArgs;
            }

            options.RequestInit = new RequestInit
            {
                Credentials = GetDefaultCredentialsString(),
                Headers = GetHeaders(request),
                Method = request.Method.Method
            };

            options.RequestUri = request.RequestUri.ToString();
            WebAssemblyJSRuntime.Instance.InvokeUnmarshalled<int, byte[], string, object>(
                "Blazor._internal.http.sendAsync",
                id,
                request.Content == null ? null : await request.Content.ReadAsByteArrayAsync(),
                JsonSerializer.Serialize(options, JsonSerializerOptionsProvider.Options));

            return await tcs.Task;
        }

        /// <remarks>
        /// While it may be tempting to remove this method because it appears to be unused,
        /// this method is referenced by client code and must persist.
        /// </remarks>
#pragma warning disable IDE0051 // Remove unused private members
        private static void ReceiveResponse(
#pragma warning restore IDE0051 // Remove unused private members
            string id,
            string responseDescriptorJson,
            byte[] responseBodyData,
            string errorText)
        {
            TaskCompletionSource<HttpResponseMessage> tcs;
            var idVal = int.Parse(id);
            lock (_idLock)
            {
                tcs = _pendingRequests[idVal];
                _pendingRequests.Remove(idVal);
            }

            if (errorText != null)
            {
                tcs.SetException(new HttpRequestException(errorText));
            }
            else
            {
                var responseDescriptor = JsonSerializer.Deserialize<ResponseDescriptor>(responseDescriptorJson, JsonSerializerOptionsProvider.Options);
                var responseContent = responseBodyData == null ? null : new ByteArrayContent(responseBodyData);
                var responseMessage = responseDescriptor.ToResponseMessage(responseContent);
                tcs.SetResult(responseMessage);
            }
        }

        /// <remarks>
        /// While it may be tempting to remove this method because it appears to be unused,
        /// this method is referenced by client code and must persist.
        /// </remarks>
#pragma warning disable IDE0051 // Remove unused private members
        private static byte[] AllocateArray(string length) => new byte[int.Parse(length)];
#pragma warning restore IDE0051 // Remove unused private members

        private static IReadOnlyList<Header> GetHeaders(HttpRequestMessage request)
        {
            var requestHeaders = request.Headers.AsEnumerable();
            if (request.Content?.Headers != null)
            {
                requestHeaders = requestHeaders.Concat(request.Content.Headers);
            }

            var headers = new List<Header>();
            foreach (var item in requestHeaders)
            {
                foreach (var headerValue in item.Value)
                {
                    headers.Add(new Header { Name = item.Key, Value = headerValue });
                }
            }

            return headers;
        }

        private static string GetDefaultCredentialsString()
        {
            // See https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials for
            // standard values and meanings
            switch (DefaultCredentials)
            {
                case FetchCredentialsOption.Omit:
                    return "omit";
                case FetchCredentialsOption.SameOrigin:
                    return "same-origin";
                case FetchCredentialsOption.Include:
                    return "include";
                default:
                    throw new ArgumentException($"Unknown credentials option '{DefaultCredentials}'.");
            }
        }

        // Keep these in sync with TypeScript class in Http.ts
        private class FetchOptions
        {
            public string RequestUri { get; set; }
            public RequestInit RequestInit { get; set; }
            public object RequestInitOverrides { get; set; }
        }

        private class RequestInit
        {
            public string Credentials { get; set; }
            public IReadOnlyList<Header> Headers { get; set; }
            public string Method { get; set; }
        }

        private class ResponseDescriptor
        {
#pragma warning disable 0649
            public int StatusCode { get; set; }
            public string StatusText { get; set; }
            public IReadOnlyList<Header> Headers { get; set; }
#pragma warning restore 0649

            public HttpResponseMessage ToResponseMessage(HttpContent content)
            {
                var result = new HttpResponseMessage((HttpStatusCode)StatusCode);
                result.ReasonPhrase = StatusText;
                result.Content = content;
                var headers = result.Headers;
                var contentHeaders = result.Content?.Headers;
                foreach (var pair in Headers)
                {
                    if (!headers.TryAddWithoutValidation(pair.Name, pair.Value))
                    {
                        contentHeaders?.TryAddWithoutValidation(pair.Name, pair.Value);
                    }
                }

                return result;
            }
        }

        private class Header
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }
    }
}
