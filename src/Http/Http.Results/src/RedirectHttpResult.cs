// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that returns a Found (302), Moved Permanently (301), Temporary Redirect (307),
/// or Permanent Redirect (308) response with a Location header to the supplied URL.
/// </summary>
public sealed partial class RedirectHttpResult : IResult, IRedirectHttpResult
{
    private readonly string _url;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <param name="acceptLocalUrlOnly">If set to true, only local URLs are accepted and
    /// will throw an exception when the supplied URL is not considered local. (Default: false)</param>
    internal RedirectHttpResult(string url, bool acceptLocalUrlOnly = false)
         : this(url, permanent: false, acceptLocalUrlOnly)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="acceptLocalUrlOnly">If set to true, only local URLs are accepted
    /// and will throw an exception when the supplied URL is not considered local. (Default: false)</param>
    internal RedirectHttpResult(string url, bool permanent, bool acceptLocalUrlOnly = false)
        : this(url, permanent, preserveMethod: false, acceptLocalUrlOnly)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307)
    /// or permanent redirect (308) preserve the initial request method.</param>
    /// <param name="acceptLocalUrlOnly">If set to true, only local URLs are accepted
    /// and will throw an exception when the supplied URL is not considered local. (Default: false)</param>
    internal RedirectHttpResult(string url, bool permanent, bool preserveMethod, bool acceptLocalUrlOnly = false)
    {
        if (url == null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("Argument cannot be null or empty", nameof(url));
        }

        Permanent = permanent;
        PreserveMethod = preserveMethod;
        AcceptLocalUrlOnly = acceptLocalUrlOnly;

        _url = url;
    }

    /// <inheritdoc/>
    public bool Permanent { get; }

    /// <inheritdoc/>
    public bool PreserveMethod { get; }

    /// <inheritdoc/>
    public string? Url => _url;

    /// <inheritdoc/>
    public bool AcceptLocalUrlOnly { get; }

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<RedirectHttpResult>>();
        var isLocalUrl = SharedUrlHelper.IsLocalUrl(Url);

        if (AcceptLocalUrlOnly && !isLocalUrl)
        {
            throw new InvalidOperationException("The supplied URL is not local. A URL with an absolute path is considered local if it does not have a host/authority part. URLs using virtual paths ('~/') are also local.");
        }

        // IsLocalUrl is called to handle URLs starting with '~/'.
        var destinationUrl = isLocalUrl ? SharedUrlHelper.Content(httpContext, contentPath: _url) : _url;

        Log.RedirectResultExecuting(logger, destinationUrl);

        if (PreserveMethod)
        {
            httpContext.Response.StatusCode = Permanent
                ? StatusCodes.Status308PermanentRedirect
                : StatusCodes.Status307TemporaryRedirect;
            httpContext.Response.Headers.Location = destinationUrl;
        }
        else
        {
            httpContext.Response.Redirect(destinationUrl, Permanent);
        }

        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information,
            "Executing RedirectResult, redirecting to {Destination}.",
            EventName = "RedirectResultExecuting")]
        public static partial void RedirectResultExecuting(ILogger logger, string destination);
    }
}
