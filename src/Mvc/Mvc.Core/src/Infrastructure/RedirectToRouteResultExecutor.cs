// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{RedirectToRouteResult}"/> for <see cref="RedirectToRouteResult"/>.
/// </summary>
public partial class RedirectToRouteResultExecutor : IActionResultExecutor<RedirectToRouteResult>
{
    private readonly ILogger _logger;
    private readonly IUrlHelperFactory _urlHelperFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="RedirectToRouteResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create loggers.</param>
    /// <param name="urlHelperFactory">The factory used to create url helpers.</param>
    public RedirectToRouteResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(urlHelperFactory);

        _logger = loggerFactory.CreateLogger<RedirectToRouteResult>();
        _urlHelperFactory = urlHelperFactory;
    }

    /// <inheritdoc />
    public virtual Task ExecuteAsync(ActionContext context, RedirectToRouteResult result)
    {
        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

        var destinationUrl = urlHelper.RouteUrl(
            result.RouteName,
            result.RouteValues,
            protocol: null,
            host: null,
            fragment: result.Fragment);
        if (string.IsNullOrEmpty(destinationUrl))
        {
            throw new InvalidOperationException(Resources.NoRoutesMatched);
        }

        Log.RedirectToRouteResultExecuting(_logger, destinationUrl, result.RouteName);

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
        [LoggerMessage(1, LogLevel.Information, "Executing RedirectToRouteResult, redirecting to {Destination} from route {RouteName}.", EventName = "RedirectToRouteResultExecuting")]
        public static partial void RedirectToRouteResultExecuting(ILogger logger, string destination, string? routeName);
    }
}
