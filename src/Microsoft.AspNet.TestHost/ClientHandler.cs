// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.TestHost
{
    /// <summary>
    /// This adapts HttpRequestMessages to ASP.NET requests, dispatches them through the pipeline, and returns the
    /// associated HttpResponseMessage.
    /// </summary>
    public class ClientHandler : HttpMessageHandler
    {
        private readonly Func<IFeatureCollection, Task> _next;
        private readonly PathString _pathBase;

        /// <summary>
        /// Create a new handler.
        /// </summary>
        /// <param name="next">The pipeline entry point.</param>
        public ClientHandler(Func<IFeatureCollection, Task> next, PathString pathBase)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;

            // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
            if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
            {
                pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
            }
            _pathBase = pathBase;
        }

        /// <summary>
        /// This adapts HttpRequestMessages to ASP.NET requests, dispatches them through the pipeline, and returns the
        /// associated HttpResponseMessage.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var state = new RequestState(request, _pathBase);
            var requestContent = request.Content ?? new StreamContent(Stream.Null);
            var body = await requestContent.ReadAsStreamAsync();
            if (body.CanSeek)
            {
                // This body may have been consumed before, rewind it.
                body.Seek(0, SeekOrigin.Begin);
            }
            state.HttpContext.Request.Body = body;
            var registration = cancellationToken.Register(state.AbortRequest);

            // Async offload, don't let the test code block the caller.
            var offload = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await _next(state.HttpContext.Features);
                        state.CompleteResponse();
                    }
                    catch (Exception ex)
                    {
                        state.Abort(ex);
                    }
                    finally
                    {
                        registration.Dispose();
                    }
                });

            return await state.ResponseTask;
        }

        private class RequestState
        {
            private readonly HttpRequestMessage _request;
            private TaskCompletionSource<HttpResponseMessage> _responseTcs;
            private ResponseStream _responseStream;
            private ResponseFeature _responseFeature;
            private CancellationTokenSource _requestAbortedSource;
            private bool _pipelineFinished;

            internal RequestState(HttpRequestMessage request, PathString pathBase)
            {
                _request = request;
                _responseTcs = new TaskCompletionSource<HttpResponseMessage>();
                _requestAbortedSource = new CancellationTokenSource();
                _pipelineFinished = false;

                if (request.RequestUri.IsDefaultPort)
                {
                    request.Headers.Host = request.RequestUri.Host;
                }
                else
                {
                    request.Headers.Host = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
                }

                HttpContext = new DefaultHttpContext();
                HttpContext.Features.Set<IHttpRequestFeature>(new RequestFeature());
                _responseFeature = new ResponseFeature();
                HttpContext.Features.Set<IHttpResponseFeature>(_responseFeature);
                var serverRequest = HttpContext.Request;
                serverRequest.Protocol = "HTTP/" + request.Version.ToString(2);
                serverRequest.Scheme = request.RequestUri.Scheme;
                serverRequest.Method = request.Method.ToString();

                var fullPath = PathString.FromUriComponent(request.RequestUri);
                PathString remainder;
                if (fullPath.StartsWithSegments(pathBase, out remainder))
                {
                    serverRequest.PathBase = pathBase;
                    serverRequest.Path = remainder;
                }
                else
                {
                    serverRequest.PathBase = PathString.Empty;
                    serverRequest.Path = fullPath;
                }

                serverRequest.QueryString = QueryString.FromUriComponent(request.RequestUri);

                foreach (var header in request.Headers)
                {
                    serverRequest.Headers.Append(header.Key, header.Value.ToArray());
                }
                var requestContent = request.Content;
                if (requestContent != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        serverRequest.Headers.Append(header.Key, header.Value.ToArray());
                    }
                }

                _responseStream = new ResponseStream(ReturnResponseMessage, AbortRequest);
                HttpContext.Response.Body = _responseStream;
                HttpContext.Response.StatusCode = 200;
                HttpContext.RequestAborted = _requestAbortedSource.Token;
            }

            public HttpContext HttpContext { get; private set; }

            public Task<HttpResponseMessage> ResponseTask
            {
                get { return _responseTcs.Task; }
            }

            internal void AbortRequest()
            {
                if (!_pipelineFinished)
                {
                    _requestAbortedSource.Cancel();
                }
                _responseStream.Complete();
            }

            internal void CompleteResponse()
            {
                _pipelineFinished = true;
                ReturnResponseMessage();
                _responseStream.Complete();
            }

            internal void ReturnResponseMessage()
            {
                if (!_responseTcs.Task.IsCompleted)
                {
                    var response = GenerateResponse();
                    _responseFeature.FireOnResponseCompleted();
                    // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
                    Task.Factory.StartNew(() => _responseTcs.TrySetResult(response));
                }
            }

            [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
                Justification = "HttpResposneMessage must be returned to the caller.")]
            private HttpResponseMessage GenerateResponse()
            {
                _responseFeature.FireOnSendingHeaders();

                var response = new HttpResponseMessage();
                response.StatusCode = (HttpStatusCode)HttpContext.Response.StatusCode;
                response.ReasonPhrase = HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase;
                response.RequestMessage = _request;
                // response.Version = owinResponse.Protocol;

                response.Content = new StreamContent(_responseStream);

                foreach (var header in HttpContext.Response.Headers)
                {
                    if (!response.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
                    {
                        bool success = response.Content.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                        Contract.Assert(success, "Bad header");
                    }
                }
                return response;
            }

            internal void Abort(Exception exception)
            {
                _pipelineFinished = true;
                _responseStream.Abort(exception);
                _responseTcs.TrySetException(exception);
            }
        }
    }
}
