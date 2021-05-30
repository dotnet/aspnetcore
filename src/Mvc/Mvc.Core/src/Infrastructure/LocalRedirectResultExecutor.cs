// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A <see cref="IActionResultExecutor{LocalRedirectResult}"/> that handles <see cref="LocalRedirectResult"/>.
    /// </summary>
    public class LocalRedirectResultExecutor : IActionResultExecutor<LocalRedirectResult>
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
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (urlHelperFactory == null)
            {
                throw new ArgumentNullException(nameof(urlHelperFactory));
            }

            _logger = loggerFactory.CreateLogger<LocalRedirectResultExecutor>();
            _urlHelperFactory = urlHelperFactory;
        }

        /// <inheritdoc />
        public virtual Task ExecuteAsync(ActionContext context, LocalRedirectResult result)
        {
            var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

            return ExecuteAsyncInternal(
                context.HttpContext,
                result,
                urlHelper.IsLocalUrl,
                urlHelper.Content,
                _logger);
        }

        internal static Task ExecuteAsyncInternal(
            HttpContext httpContext,
            LocalRedirectResult result,
            Func<string, bool> isLocalUrl,
            Func<string, string> getContent,
            ILogger logger
            )
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            // IsLocalUrl is called to handle  Urls starting with '~/'.
            if (!isLocalUrl(result.Url))
            {
                throw new InvalidOperationException(Resources.UrlNotLocal);
            }

            var destinationUrl = getContent(result.Url);
            logger.LocalRedirectResultExecuting(destinationUrl);

            if (result.PreserveMethod)
            {
                httpContext.Response.StatusCode = result.Permanent ?
                    StatusCodes.Status308PermanentRedirect : StatusCodes.Status307TemporaryRedirect;
                httpContext.Response.Headers.Location = destinationUrl;
            }
            else
            {
                httpContext.Response.Redirect(destinationUrl, result.Permanent);
            }

            return Task.CompletedTask;
        }
    }
}
