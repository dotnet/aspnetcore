// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.TestHost;

/// <summary>
/// This adapts HttpRequestMessages to ASP.NET Core requests, dispatches them through the pipeline, and returns the
/// associated HttpResponseMessage.
/// </summary>
public class ClientHandler : HttpMessageHandler
{
    private readonly ApplicationWrapper _application;
    private readonly Action<HttpContext> _additionalContextConfiguration;
    private readonly PathString _pathBase;

    /// <summary>
    /// Create a new handler.
    /// </summary>
    /// <param name="pathBase">The base path.</param>
    /// <param name="application">The <see cref="IHttpApplication{TContext}"/>.</param>
    /// <param name="additionalContextConfiguration">The action to additionally configure <see cref="HttpContext"/>.</param>
    internal ClientHandler(PathString pathBase, ApplicationWrapper application, Action<HttpContext>? additionalContextConfiguration = null)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _additionalContextConfiguration = additionalContextConfiguration ?? NoExtraConfiguration;

        // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
        if (pathBase.HasValue && pathBase.Value.EndsWith('/'))
        {
            pathBase = new PathString(pathBase.Value[..^1]); // All but the last character
        }
        _pathBase = pathBase;
    }

    internal bool AllowSynchronousIO { get; set; }

    internal bool PreserveExecutionContext { get; set; }

    /// <summary>
    /// This synchronous method is not supported due to the risk of threadpool exhaustion when running multiple tests in parallel. 
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <exception cref="NotSupportedException">Thrown unconditionally.</exception>
    /// <remarks>
    /// Use the asynchronous version of this method, <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>, instead.
    /// </remarks>
    protected override HttpResponseMessage Send(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "This synchronous method is not supported due to the risk of threadpool exhaustion " +
            "when running multiple tests in parallel. Use the asynchronous version of this method instead.");
    }

    /// <summary>
    /// This adapts HttpRequestMessages to ASP.NET Core requests, dispatches them through the pipeline, and returns the
    /// associated HttpResponseMessage.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task{TResult}"/> returning the <see cref="HttpResponseMessage"/>.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contextBuilder = new HttpContextBuilder(_application, AllowSynchronousIO, PreserveExecutionContext);

        var requestContent = request.Content;

        if (requestContent != null)
        {
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
        }

        contextBuilder.Configure((context, reader) =>
        {
            var req = context.Request;

            req.Protocol = HttpProtocol.GetHttpProtocol(request.Version);
            req.Method = request.Method.ToString();
            req.Scheme = request.RequestUri!.Scheme;

            var canHaveBody = false;
            if (requestContent != null)
            {
                canHaveBody = true;
                // Chunked takes precedence over Content-Length, don't create a request with both Content-Length and chunked.
                if (request.Headers.TransferEncodingChunked != true)
                {
                    // Reading the ContentLength will add it to the Headers‼
                    // https://github.com/dotnet/runtime/blob/874399ab15e47c2b4b7c6533cc37d27d47cb5242/src/libraries/System.Net.Http/src/System/Net/Http/Headers/HttpContentHeaders.cs#L68-L87
                    var contentLength = requestContent.Headers.ContentLength;
                    if (!contentLength.HasValue && request.Version == HttpVersion.Version11)
                    {
                        // HTTP/1.1 requests with a body require either Content-Length or Transfer-Encoding: chunked.
                        request.Headers.TransferEncodingChunked = true;
                    }
                    else if (contentLength == 0)
                    {
                        canHaveBody = false;
                    }
                }
                else
                {
                    // https://www.rfc-editor.org/rfc/rfc9112#section-6.2-2
                    // A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field.
                    requestContent.Headers.Remove(HeaderNames.ContentLength);
                }

                foreach (var header in requestContent.Headers)
                {
                    req.Headers.Append(header.Key, header.Value.ToArray());
                }

                if (canHaveBody)
                {
                    req.Body = new AsyncStreamWrapper(reader.AsStream(), () => contextBuilder.AllowSynchronousIO);
                }
            }
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(canHaveBody));

            foreach (var header in request.Headers)
            {
                // User-Agent is a space delineated single line header but HttpRequestHeaders parses it as multiple elements.
                if (string.Equals(header.Key, HeaderNames.UserAgent, StringComparison.OrdinalIgnoreCase))
                {
                    req.Headers.Append(header.Key, string.Join(' ', header.Value));
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
        });

        contextBuilder.Configure((context, _) => _additionalContextConfiguration(context));

        var response = new HttpResponseMessage();

        // Copy trailers to the response message when the response stream is complete
        contextBuilder.RegisterResponseReadCompleteCallback(context =>
        {
            var responseTrailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

            // Trailers collection is settable so double check the app hasn't set it to null.
            if (responseTrailersFeature?.Trailers != null)
            {
                foreach (var trailer in responseTrailersFeature.Trailers)
                {
                    bool success = response.TrailingHeaders.TryAddWithoutValidation(trailer.Key, (IEnumerable<string>)trailer.Value);
                    Contract.Assert(success, "Bad trailer");
                }
            }
        });

        var httpContext = await contextBuilder.SendAsync(cancellationToken);

        response.StatusCode = (HttpStatusCode)httpContext.Response.StatusCode;
        response.ReasonPhrase = httpContext.Features.GetRequiredFeature<IHttpResponseFeature>().ReasonPhrase;
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

    private static void NoExtraConfiguration(HttpContext context)
    {
        // Intentional no op
    }
}
