// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Browser.Http
{
    /// <summary>
    /// A browser-compatible implementation of <see cref="HttpMessageHandler"/>
    /// </summary>
    public class BrowserHttpMessageHandler : HttpMessageHandler
    {
        static object _idLock = new object();
        static int _nextRequestId = 0;
        static IDictionary<int, TaskCompletionSource<HttpResponseMessage>> _pendingRequests
            = new Dictionary<int, TaskCompletionSource<HttpResponseMessage>>();

        public const string FetchArgs = "BrowserHttpMessageHandler.FetchArgs";

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

            request.Properties.TryGetValue(FetchArgs, out var fetchArgs);

            RegisteredFunction.Invoke<object>(
                $"{typeof(BrowserHttpMessageHandler).FullName}.Send",
                id,
                request.Method.Method,
                request.RequestUri,
                request.Content == null ? null : await GetContentAsString(request.Content),
                SerializeHeadersAsJson(request),
                fetchArgs);

            return await tcs.Task;
        }

        private string SerializeHeadersAsJson(HttpRequestMessage request)
            => JsonUtil.Serialize(
                (from header in request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                 from headerValue in header.Value // There can be more than one value for each name
                 select new[] { header.Key, headerValue }).ToList()
            );

        private static async Task<string> GetContentAsString(HttpContent content)
            => content is StringContent stringContent
                ? await stringContent.ReadAsStringAsync()
                : throw new InvalidOperationException($"Currently, {typeof(HttpClient).FullName} " +
                    $"only supports contents of type {nameof(StringContent)}, but you supplied " +
                    $"{content.GetType().FullName}.");

        private static void ReceiveResponse(
            string id,
            string responseDescriptorJson,
            string responseBodyText,
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
                var responseDescriptor = JsonUtil.Deserialize<ResponseDescriptor>(responseDescriptorJson);
                var responseContent = responseBodyText == null ? null : new StringContent(responseBodyText);
                var responseMessage = responseDescriptor.ToResponseMessage(responseContent);
                tcs.SetResult(responseMessage);
            }
        }

        // Keep in sync with TypeScript class in Http.ts
        private class ResponseDescriptor
        {
            #pragma warning disable 0649
            public int StatusCode { get; set; }
            public string[][] Headers { get; set; }
            #pragma warning restore 0649

            public HttpResponseMessage ToResponseMessage(HttpContent content)
            {
                var result = new HttpResponseMessage((HttpStatusCode)StatusCode);
                result.Content = content;
                var headers = result.Headers;
                var contentHeaders = result.Content?.Headers;
                foreach (var pair in Headers)
                {
                    if (!headers.TryAddWithoutValidation(pair[0], pair[1]))
                    {
                        contentHeaders?.TryAddWithoutValidation(pair[0], pair[1]);
                    }
                }

                return result;
            }
        }
    }
}
