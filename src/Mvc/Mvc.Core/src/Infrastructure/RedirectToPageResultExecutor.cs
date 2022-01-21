// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{RedirectToPageResult}"/> for <see cref="RedirectToPageResult"/>.
/// </summary>
public partial class RedirectToPageResultExecutor : IActionResultExecutor<RedirectToPageResult>
{
    private readonly ILogger _logger;
    private readonly IUrlHelperFactory _urlHelperFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="RedirectToPageResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    /// <param name="urlHelperFactory">The factory used to create url helpers.</param>
    public RedirectToPageResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory)
    {
        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        if (urlHelperFactory == null)
        {
            throw new ArgumentNullException(nameof(urlHelperFactory));
        }

        _logger = loggerFactory.CreateLogger<RedirectToRouteResult>();
        _urlHelperFactory = urlHelperFactory;
    }

    /// <inheritdoc />
    public virtual Task ExecuteAsync(ActionContext context, RedirectToPageResult result)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);
        var destinationUrl = urlHelper.Page(
            result.PageName,
            result.PageHandler,
            result.RouteValues,
            result.Protocol,
            result.Host,
            fragment: result.Fragment);

        if (string.IsNullOrEmpty(destinationUrl))
        {
            throw new InvalidOperationException(Resources.FormatNoRoutesMatchedForPage(result.PageName));
        }

        Log.RedirectToPageResultExecuting(_logger, result.PageName);

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
        [LoggerMessage(1, LogLevel.Information, "Executing RedirectToPageResult, redirecting to {Page}.", EventName = "RedirectToPageResultExecuting")]
        public static partial void RedirectToPageResultExecuting(ILogger logger, string? page);
    }
}
