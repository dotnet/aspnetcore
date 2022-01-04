// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result;

/// <summary>
/// An <see cref="IResult"/> that returns a Found (302), Moved Permanently (301), Temporary Redirect (307),
/// or Permanent Redirect (308) response with a Location header to the supplied local URL.
/// </summary>
internal sealed partial class LocalRedirectResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    public LocalRedirectResult(string localUrl)
         : this(localUrl, permanent: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    public LocalRedirectResult(string localUrl, bool permanent)
        : this(localUrl, permanent, preserveMethod: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalRedirectResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request's method.</param>
    public LocalRedirectResult(string localUrl, bool permanent, bool preserveMethod)
    {
        if (string.IsNullOrEmpty(localUrl))
        {
            throw new ArgumentException("Argument cannot be null or empty", nameof(localUrl));
        }

        Permanent = permanent;
        PreserveMethod = preserveMethod;
        Url = localUrl;
    }

    /// <summary>
    /// Gets or sets the value that specifies that the redirect should be permanent if true or temporary if false.
    /// </summary>
    public bool Permanent { get; }

    /// <summary>
    /// Gets or sets an indication that the redirect preserves the initial request method.
    /// </summary>
    public bool PreserveMethod { get; }

    /// <summary>
    /// Gets or sets the local URL to redirect to.
    /// </summary>
    public string Url { get; }

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
    {
        if (!SharedUrlHelper.IsLocalUrl(Url))
        {
            throw new InvalidOperationException("The supplied URL is not local. A URL with an absolute path is considered local if it does not have a host/authority part. URLs using virtual paths ('~/') are also local.");
        }

        var destinationUrl = SharedUrlHelper.Content(httpContext, Url);

        // IsLocalUrl is called to handle URLs starting with '~/'.
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<LocalRedirectResult>>();

        Log.LocalRedirectResultExecuting(logger, destinationUrl);

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
            "Executing LocalRedirectResult, redirecting to {Destination}.",
            EventName = "LocalRedirectResultExecuting")]
        public static partial void LocalRedirectResultExecuting(ILogger logger, string destination);
    }
}
