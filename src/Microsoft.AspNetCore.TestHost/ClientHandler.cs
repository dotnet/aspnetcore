// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Context = Microsoft.AspNetCore.Hosting.Internal.HostingApplication.Context;

namespace Microsoft.AspNetCore.TestHost
{
    /// <summary>
    /// This adapts HttpRequestMessages to ASP.NET Core requests, dispatches them through the pipeline, and returns the
    /// associated HttpResponseMessage.
    /// </summary>
    public class ClientHandler : HttpMessageHandler
    {
        private readonly IHttpApplication<Context> _application;
        private readonly PathString _pathBase;

        /// <summary>
        /// Create a new handler.
        /// </summary>
        /// <param name="pathBase">The base path.</param>
        /// <param name="application">The <see cref="IHttpApplication{TContext}"/>.</param>
        public ClientHandler(PathString pathBase, IHttpApplication<Context> application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            _application = application;

            // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
            if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
            {
                pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
            }
            _pathBase = pathBase;
        }

        /// <summary>
        /// This adapts HttpRequestMessages to ASP.NET Core requests, dispatches them through the pipeline, and returns the
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

            var state = new RequestState(request, _pathBase, _application);
            var requestContent = request.Content ?? new StreamContent(Stream.Null);
            var body = await requestContent.ReadAsStreamAsync();
            if (body.CanSeek)
            {
                // This body may have been consumed before, rewind it.
                body.Seek(0, SeekOrigin.Begin);
            }
            state.Context.HttpContext.Request.Body = body;
            var registration = cancellationToken.Register(state.AbortRequest);

            // Async offload, don't let the test code block the caller.
            var offload = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await _application.ProcessRequestAsync(state.Context);
                        await state.CompleteResponseAsync();
                        state.ServerCleanup(exception: null);
                    }
                    catch (Exception ex)
                    {
                        state.Abort(ex);
                        state.ServerCleanup(ex);
                    }
                    finally
                    {
                        registration.Dispose();
                    }
                });

            return await state.ResponseTask.ConfigureAwait(false);
        }

        private class RequestState
        {
            private readonly HttpRequestMessage _request;
            private readonly IHttpApplication<Context> _application;
            private TaskCompletionSource<HttpResponseMessage> _responseTcs;
            private ResponseStream _responseStream;
            private ResponseFeature _responseFeature;
            private CancellationTokenSource _requestAbortedSource;
            private bool _pipelineFinished;

            internal RequestState(HttpRequestMessage request, PathString pathBase, IHttpApplication<Context> application)
            {
                _request = request;
                _application = application;
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

                var contextFeatures = new FeatureCollection();
                var requestFeature = new RequestFeature();
                contextFeatures.Set<IHttpRequestFeature>(requestFeature);
                _responseFeature = new ResponseFeature();
                contextFeatures.Set<IHttpResponseFeature>(_responseFeature);
                var requestLifetimeFeature = new HttpRequestLifetimeFeature();
                contextFeatures.Set<IHttpRequestLifetimeFeature>(requestLifetimeFeature);

                requestFeature.Protocol = "HTTP/" + request.Version.ToString(fieldCount: 2);
                requestFeature.Scheme = request.RequestUri.Scheme;
                requestFeature.Method = request.Method.ToString();

                var fullPath = PathString.FromUriComponent(request.RequestUri);
                PathString remainder;
                if (fullPath.StartsWithSegments(pathBase, out remainder))
                {
                    requestFeature.PathBase = pathBase.Value;
                    requestFeature.Path = remainder.Value;
                }
                else
                {
                    requestFeature.PathBase = string.Empty;
                    requestFeature.Path = fullPath.Value;
                }

                requestFeature.QueryString = QueryString.FromUriComponent(request.RequestUri).Value;

                foreach (var header in request.Headers)
                {
                    requestFeature.Headers.Append(header.Key, header.Value.ToArray());
                }
                var requestContent = request.Content;
                if (requestContent != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        requestFeature.Headers.Append(header.Key, header.Value.ToArray());
                    }
                }

                _responseStream = new ResponseStream(ReturnResponseMessageAsync, AbortRequest);
                _responseFeature.Body = _responseStream;
                _responseFeature.StatusCode = 200;
                requestLifetimeFeature.RequestAborted = _requestAbortedSource.Token;

                Context = application.CreateContext(contextFeatures);
            }

            public Context Context { get; private set; }

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

            internal async Task CompleteResponseAsync()
            {
                _pipelineFinished = true;
                await ReturnResponseMessageAsync();
                _responseStream.Complete();
                await _responseFeature.FireOnResponseCompletedAsync();
            }

            internal async Task ReturnResponseMessageAsync()
            {
                // Check if the response has already started because the TrySetResult below could happen a bit late
                // (as it happens on a different thread) by which point the CompleteResponseAsync could run and calls this
                // method again.
                if (!Context.HttpContext.Response.HasStarted)
                {
                    var response = await GenerateResponseAsync();
                    // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
                    var setResult = Task.Factory.StartNew(() => _responseTcs.TrySetResult(response));
                }
            }

            private async Task<HttpResponseMessage> GenerateResponseAsync()
            {
                await _responseFeature.FireOnSendingHeadersAsync();
                var httpContext = Context.HttpContext;

                var response = new HttpResponseMessage();
                response.StatusCode = (HttpStatusCode)httpContext.Response.StatusCode;
                response.ReasonPhrase = httpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase;
                response.RequestMessage = _request;
                // response.Version = owinResponse.Protocol;

                response.Content = new StreamContent(_responseStream);

                foreach (var header in httpContext.Response.Headers)
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

            internal void ServerCleanup(Exception exception)
            {
                _application.DisposeContext(Context, exception);
            }
        }
    }
}
