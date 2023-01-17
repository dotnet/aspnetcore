// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{VirtualFileResult}"/> for <see cref="RedirectResult"/>.
/// </summary>
public partial class RedirectResultExecutor : IActionResultExecutor<RedirectResult>
{
    private readonly ILogger _logger;
    private readonly IUrlHelperFactory _urlHelperFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="RedirectResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    /// <param name="urlHelperFactory">The factory used to create url helpers.</param>
    public RedirectResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(urlHelperFactory);

        _logger = loggerFactory.CreateLogger<RedirectResultExecutor>();
        _urlHelperFactory = urlHelperFactory;
    }

    /// <inheritdoc />
    public virtual Task ExecuteAsync(ActionContext context, RedirectResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

        // IsLocalUrl is called to handle URLs starting with '~/'.
        var destinationUrl = result.Url;
        if (urlHelper.IsLocalUrl(destinationUrl))
        {
            destinationUrl = urlHelper.Content(result.Url);
        }

        Log.RedirectResultExecuting(_logger, destinationUrl);

        if (result.PreserveMethod)
        {
            context.HttpContext.Response.StatusCode = result.Permanent ?
                StatusCodes.Status308PermanentRedirect : StatusCodes.Status307TemporaryRedirect;
            context.HttpContext.Response.Headers.Location = destinationUrl;
        }
        else
        {
            context.HttpContext.Response.Redirect(destinationUrl, result.Permanent);
        }

        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Executing RedirectResult, redirecting to {Destination}.", EventName = "RedirectResultExecuting")]
        public static partial void RedirectResultExecuting(ILogger logger, string destination);
    }
}
