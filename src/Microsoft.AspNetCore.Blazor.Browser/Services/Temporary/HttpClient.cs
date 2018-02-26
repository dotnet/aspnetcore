// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using Microsoft.AspNetCore.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Browser.Services.Temporary
{
    /// <summary>
    /// Provides mechanisms for sending HTTP requests.
    /// 
    /// This is intended to serve as an equivalent to <see cref="System.Net.Http.HttpClient"/>
    /// until we're able to use the real <see cref="System.Net.Http.HttpClient"/> inside Mono
    /// for WebAssembly.
    /// </summary>
    public class HttpClient
    {
        static object _idLock = new object();
        static int _nextRequestId = 0;
        static IDictionary<int, TaskCompletionSource<HttpResponseMessage>> _pendingRequests
            = new Dictionary<int, TaskCompletionSource<HttpResponseMessage>>();
        IUriHelper _uriHelper;

        // Making the constructor internal to be sure people only get instances from
        // the service provider. It doesn't make any difference right now, but when
        // we switch to System.Net.Http.HttpClient, there may be a period where it
        // only works when you get an instance from the service provider because it
        // has to be configured with a browser-specific HTTP handler. In the long
        // term, it should be possible to use System.Net.Http.HttpClient directly
        // without any browser-specific constructor args.
        internal HttpClient(IUriHelper uriHelper)
        {
            _uriHelper = uriHelper ?? throw new ArgumentNullException(nameof(uriHelper));
        }

        /// <summary>
        /// Sends a GET request to the specified URI and returns the response body as
        /// a string in an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The URI the request is sent to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<string> GetStringAsync(string requestUri)
        {
            var response = await GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"The response status code was {response.StatusCode}");
            }
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Sends a GET request to the specified URI and returns the response as
        /// an instance of <see cref="HttpResponseMessage"/> in an asynchronous
        /// operation.
        /// </summary>
        /// <param name="requestUri">The URI the request is sent to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<HttpResponseMessage> GetAsync(string requestUri)
            => SendAsync(new HttpRequestMessage(HttpMethod.Get, CreateUri(requestUri)));

        /// <summary>
        /// Sends a POST request to the specified URI and returns the response as
        /// an instance of <see cref="HttpResponseMessage"/> in an asynchronous
        /// operation.
        /// </summary>
        /// <param name="requestUri">The URI the request is sent to.</param>
        /// <param name="content">The content for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
            => SendAsync(new HttpRequestMessage(HttpMethod.Post, CreateUri(requestUri))
            {
                Content = content
            });

        /// <summary>
        /// Sends an HTTP request to the specified URI and returns the response as
        /// an instance of <see cref="HttpResponseMessage"/> in an asynchronous
        /// operation.
        /// </summary>
        /// <param name="request">The request to be sent.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            int id;
            lock (_idLock)
            {
                id = _nextRequestId++;
                _pendingRequests.Add(id, tcs);
            }

            RegisteredFunction.Invoke<object>(
                $"{typeof(HttpClient).FullName}.Send",
                id,
                request.Method.Method,
                ResolveRequestUri(request.RequestUri),
                request.Content == null ? null : await GetContentAsString(request.Content),
                SerializeHeadersAsJson(request));

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

        private Uri CreateUri(String uri)
            => new Uri(uri, UriKind.RelativeOrAbsolute);

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

        private string ResolveRequestUri(Uri requestUri)
            => _uriHelper.ToAbsoluteUri(requestUri.OriginalString).AbsoluteUri;

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
