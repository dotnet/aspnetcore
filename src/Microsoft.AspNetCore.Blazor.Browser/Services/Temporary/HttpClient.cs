// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using System.Collections.Generic;
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

        // Making the constructor internal to be sure people only get instances from
        // the service provider. It doesn't make any difference right now, but when
        // we switch to System.Net.Http.HttpClient, there may be a period where it
        // only works when you get an instance from the service provider because it
        // has to be configured with a browser-specific HTTP handler. In the long
        // term, it should be possible to use System.Net.Http.HttpClient directly
        // without any browser-specific constructor args.
        internal HttpClient()
        {
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

        // <summary>
        /// Sends a GET request to the specified URI and returns the response as
        /// an instance of <see cref="HttpResponseMessage"/> in an asynchronous
        /// operation.
        /// </summary>
        /// <param name="requestUri">The URI the request is sent to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            int id;
            lock (_idLock)
            {
                id = _nextRequestId++;
                _pendingRequests.Add(id, tcs);
            }

            RegisteredFunction.Invoke<object>($"{typeof(HttpClient).FullName}.Send", id, requestUri);

            return tcs.Task;
        }

        private static void ReceiveResponse(string id, string statusCode, string responseText, string errorText)
        {
            TaskCompletionSource<HttpResponseMessage> tcs;
            var idVal = int.Parse(id);
            lock (_idLock)
            {
                tcs = _pendingRequests[idVal];
                _pendingRequests.Remove(idVal);
            }

            if (errorText == null)
            {
                tcs.SetResult(new HttpResponseMessage
                {
                    StatusCode = (HttpStatusCode)int.Parse(statusCode),
                    Content = new StringContent(responseText)
                });
            }
            else
            {
                tcs.SetException(new HttpRequestException(errorText));
            }
        }
    }
}
