// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.TestHost
{
    /// <summary>
    /// This adapts HttpRequestMessages to ASP.NET Core requests, dispatches them through the pipeline, and returns the
    /// associated HttpResponseMessage.
    /// </summary>
    public class ClientHandler : HttpMessageHandler
    {
        private readonly ApplicationWrapper _application;
        private readonly PathString _pathBase;

        /// <summary>
        /// Create a new handler.
        /// </summary>
        /// <param name="pathBase">The base path.</param>
        /// <param name="application">The <see cref="IHttpApplication{TContext}"/>.</param>
        internal ClientHandler(PathString pathBase, ApplicationWrapper application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));

            // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
            if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
            {
                pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
            }
            _pathBase = pathBase;
        }

        internal bool AllowSynchronousIO { get; set; }

        internal bool PreserveExecutionContext { get; set; }

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

            var contextBuilder = new HttpContextBuilder(_application, AllowSynchronousIO, PreserveExecutionContext);

            var requestContent = request.Content ?? new StreamContent(Stream.Null);

            // Read content from the request HttpContent into a pipe in a background task. This will allow the request
            // delegate to start before the request HttpContent is complete. A background task allows duplex streaming scenarios.
            contextBuilder.SendRequestStream(async writer =>
            {
                if (requestContent is StreamContent)
                {
                    // This is odd but required for backwards compat. If StreamContent is passed in then seek to beginning.
                    // This is safe because StreamContent.ReadAsStreamAsync doesn't block. It will return the inner stream.
                    var body = await requestContent.ReadAsStreamAsync();
                    if (body.CanSeek)
                    {
                        // This body may have been consumed before, rewind it.
                        body.Seek(0, SeekOrigin.Begin);
                    }

                    await body.CopyToAsync(writer);
                }
                else
                {
                    await requestContent.CopyToAsync(writer.AsStream());
                }

                await writer.CompleteAsync();
            });

            contextBuilder.Configure((context, reader) =>
            {
                var req = context.Request;

                if (request.Version == HttpVersion.Version20)
                {
                    // https://tools.ietf.org/html/rfc7540
                    req.Protocol =  HttpProtocol.Http2;
                }
                else
                {
                    req.Protocol = "HTTP/" + request.Version.ToString(fieldCount: 2);
                }
                req.Method = request.Method.ToString();

                req.Scheme = request.RequestUri.Scheme;

                foreach (var header in request.Headers)
                {
                    // User-Agent is a space delineated single line header but HttpRequestHeaders parses it as multiple elements.
                    if (string.Equals(header.Key, HeaderNames.UserAgent, StringComparison.OrdinalIgnoreCase))
                    {
                        req.Headers.Append(header.Key, string.Join(" ", header.Value));
                    }
                    else
                    {
                        req.Headers.Append(header.Key, header.Value.ToArray());
                    }
                }

                if (!req.Host.HasValue)
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

                req.Body = new AsyncStreamWrapper(reader.AsStream(), () => contextBuilder.AllowSynchronousIO);
            });

            var response = new HttpResponseMessage();

            // Copy trailers to the response message when the response stream is complete
            contextBuilder.RegisterResponseReadCompleteCallback(context =>
            {
                var responseTrailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

                foreach (var trailer in responseTrailersFeature.Trailers)
                {
                    bool success = response.TrailingHeaders.TryAddWithoutValidation(trailer.Key, (IEnumerable<string>)trailer.Value);
                    Contract.Assert(success, "Bad trailer");
                }
            });

            var httpContext = await contextBuilder.SendAsync(cancellationToken);

            response.StatusCode = (HttpStatusCode)httpContext.Response.StatusCode;
            response.ReasonPhrase = httpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase;
            response.RequestMessage = request;
            response.Version = request.Version;

            response.Content = new StreamContent(httpContext.Response.Body);

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
