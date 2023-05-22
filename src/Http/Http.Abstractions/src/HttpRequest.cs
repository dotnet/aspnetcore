// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the incoming side of an individual HTTP request.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(HttpRequestDebugView))]
public abstract class HttpRequest
{
    /// <summary>
    /// Gets the <see cref="HttpContext"/> for this request.
    /// </summary>
    public abstract HttpContext HttpContext { get; }

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    /// <returns>The HTTP method.</returns>
    public abstract string Method { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request scheme.
    /// </summary>
    /// <returns>The HTTP request scheme.</returns>
    public abstract string Scheme { get; set; }

    /// <summary>
    /// Returns true if the RequestScheme is https.
    /// </summary>
    /// <returns>true if this request is using https; otherwise, false.</returns>
    public abstract bool IsHttps { get; set; }

    /// <summary>
    /// Gets or sets the Host header. May include the port.
    /// </summary>
    /// <return>The Host header.</return>
    public abstract HostString Host { get; set; }

    /// <summary>
    /// Gets or sets the base path for the request. The path base should not end with a trailing slash.
    /// </summary>
    /// <returns>The base path for the request.</returns>
    public abstract PathString PathBase { get; set; }

    /// <summary>
    /// Gets or sets the portion of the request path that identifies the requested resource.
    /// <para>
    /// The value may be <see cref="PathString.Empty"/> if <see cref="PathBase"/> contains the full path,
    /// or for 'OPTIONS *' requests.
    /// The path is fully decoded by the server except for '%2F', which would decode to '/' and
    /// change the meaning of the path segments. '%2F' can only be replaced after splitting the path into segments.
    /// </para>
    /// </summary>
    public abstract PathString Path { get; set; }

    /// <summary>
    /// Gets or sets the raw query string used to create the query collection in Request.Query.
    /// </summary>
    /// <returns>The raw query string.</returns>
    public abstract QueryString QueryString { get; set; }

    /// <summary>
    /// Gets the query value collection parsed from Request.QueryString.
    /// </summary>
    /// <returns>The query value collection parsed from Request.QueryString.</returns>
    public abstract IQueryCollection Query { get; set; }

    /// <summary>
    /// Gets or sets the request protocol (e.g. HTTP/1.1).
    /// </summary>
    /// <returns>The request protocol.</returns>
    public abstract string Protocol { get; set; }

    /// <summary>
    /// Gets the request headers.
    /// </summary>
    /// <returns>The request headers.</returns>
    public abstract IHeaderDictionary Headers { get; }

    /// <summary>
    /// Gets the collection of Cookies for this request.
    /// </summary>
    /// <returns>The collection of Cookies for this request.</returns>
    public abstract IRequestCookieCollection Cookies { get; set; }

    /// <summary>
    /// Gets or sets the Content-Length header.
    /// </summary>
    /// <returns>The value of the Content-Length header, if any.</returns>
    public abstract long? ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the Content-Type header.
    /// </summary>
    /// <returns>The Content-Type header.</returns>
    public abstract string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the request body <see cref="Stream"/>.
    /// </summary>
    /// <value>The request body <see cref="Stream"/>.</value>
    public abstract Stream Body { get; set; }

    /// <summary>
    /// Gets the request body <see cref="PipeReader"/>.
    /// </summary>
    /// <value>The request body <see cref="PipeReader"/>.</value>
    public virtual PipeReader BodyReader { get => throw new NotImplementedException(); }

    /// <summary>
    /// Checks the Content-Type header for form types.
    /// </summary>
    /// <returns>true if the Content-Type header represents a form content type; otherwise, false.</returns>
    public abstract bool HasFormContentType { get; }

    /// <summary>
    /// Gets or sets the request body as a form.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    ///     incorrect content-type.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///     Invoking this property could result in thread exhaustion since it's wrapping an asynchronous implementation.
    ///     To prevent this the method <see cref="ReadFormAsync(CancellationToken)"/> can be used.
    ///     For more information, see <see href="https://aka.ms/aspnet/forms-async" />.
    ///     </para>
    /// </remarks>
    public abstract IFormCollection Form { get; set; }

    /// <summary>
    /// Reads the request body if it is a form.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    ///     incorrect content-type.
    /// </exception>
    /// <returns></returns>
    public abstract Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken());

    /// <summary>
    /// Gets the collection of route values for this request.
    /// </summary>
    /// <returns>The collection of route values for this request.</returns>
    public virtual RouteValueDictionary RouteValues { get; set; } = null!;

    private string DebuggerToString()
    {
        return HttpContextDebugFormatter.RequestToString(this);
    }

    private sealed class HttpRequestDebugView(HttpRequest request)
    {
        private readonly HttpRequest _request = request;

        public string Method => _request.Method;
        public string Scheme => _request.Scheme;
        public bool IsHttps => _request.IsHttps;
        public HostString Host => _request.Host;
        public PathString PathBase => _request.PathBase;
        public PathString Path => _request.Path;
        public QueryString QueryString => _request.QueryString;
        public IQueryCollection Query => _request.Query;
        public string Protocol => _request.Protocol;
        public IHeaderDictionary Headers => _request.Headers;
        public IRequestCookieCollection Cookies => _request.Cookies;
        public long? ContentLength => _request.ContentLength;
        public string? ContentType => _request.ContentType;
        public bool HasFormContentType => _request.HasFormContentType;
        public IFormCollection? Form => _request.HttpContext.Features.Get<IFormFeature>()?.Form;
        public RouteValueDictionary RouteValues => _request.RouteValues;
    }
}
