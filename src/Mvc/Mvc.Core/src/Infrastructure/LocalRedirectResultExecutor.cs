// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A <see cref="IActionResultExecutor{LocalRedirectResult}"/> that handles <see cref="LocalRedirectResult"/>.
/// </summary>
public partial class LocalRedirectResultExecutor : IActionResultExecutor<LocalRedirectResult>
{
    private readonly ILogger _logger;
    private readonly IUrlHelperFactory _urlHelperFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalRedirectResultExecutor"/>.
    /// </summary>
    /// <param name="loggerFactory">Used to create loggers.</param>
    /// <param name="urlHelperFactory">Used to create url helpers.</param>
    public LocalRedirectResultExecutor(ILoggerFactory loggerFactory, IUrlHelperFactory urlHelperFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(urlHelperFactory);

        _logger = loggerFactory.CreateLogger<LocalRedirectResultExecutor>();
        _urlHelperFactory = urlHelperFactory;
    }

    /// <inheritdoc />
    public virtual Task ExecuteAsync(ActionContext context, LocalRedirectResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(result);

        var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

        // IsLocalUrl is called to handle  Urls starting with '~/'.
        if (!urlHelper.IsLocalUrl(result.Url))
        {
            throw new InvalidOperationException(Resources.UrlNotLocal);
        }

        var destinationUrl = urlHelper.Content(result.Url);
        Log.LocalRedirectResultExecuting(_logger, destinationUrl);

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
        [LoggerMessage(1, LogLevel.Information, "Executing LocalRedirectResult, redirecting to {Destination}.", EventName = "LocalRedirectResultExecuting")]
        public static partial void LocalRedirectResultExecuting(ILogger logger, string destination);
    }
}
