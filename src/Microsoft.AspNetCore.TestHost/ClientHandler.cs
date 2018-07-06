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
            _application = application ?? throw new ArgumentNullException(nameof(application));

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

            var contextBuilder = new HttpContextBuilder(_application);

            Stream responseBody = null;
            var requestContent = request.Content ?? new StreamContent(Stream.Null);
            var body = await requestContent.ReadAsStreamAsync();
            contextBuilder.Configure(context =>
            {
                var req = context.Request;

                req.Protocol = "HTTP/" + request.Version.ToString(fieldCount: 2);
                req.Method = request.Method.ToString();

                req.Scheme = request.RequestUri.Scheme;

                foreach (var header in request.Headers)
                {
                    req.Headers.Append(header.Key, header.Value.ToArray());
                }

                if (req.Host == null || !req.Host.HasValue)
                {
                    // If Host wasn't explicitly set as a header, let's infer it from the Uri
                    req.Host = HostString.FromUriComponent(request.RequestUri);
                    if (request.RequestUri.IsDefaultPort)
                    {
                        req.Host = new HostString(req.Host.Host);
                    }
                }

                req.Path = PathString.FromUriComponent(request.RequestUri);
                req.PathBase = PathString.Empty;
                if (req.Path.StartsWithSegments(_pathBase, out var remainder))
                {
                    req.Path = remainder;
                    req.PathBase = _pathBase;
                }
                req.QueryString = QueryString.FromUriComponent(request.RequestUri);

                if (requestContent != null)
                {
                    foreach (var header in requestContent.Headers)
                    {
                        req.Headers.Append(header.Key, header.Value.ToArray());
                    }
                }

                if (body.CanSeek)
                {
                    // This body may have been consumed before, rewind it.
                    body.Seek(0, SeekOrigin.Begin);
                }
                req.Body = body;

                responseBody = context.Response.Body;
            });

            var httpContext = await contextBuilder.SendAsync(cancellationToken);

            var response = new HttpResponseMessage();
            response.StatusCode = (HttpStatusCode)httpContext.Response.StatusCode;
            response.ReasonPhrase = httpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase;
            response.RequestMessage = request;

            response.Content = new StreamContent(responseBody);

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
    }
}
