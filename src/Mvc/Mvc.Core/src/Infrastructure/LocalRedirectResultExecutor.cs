// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class LocalRedirectResultExecutor : IActionResultExecutor<LocalRedirectResult>
    {
        private readonly ILogger _logger;
        private readonly IUrlHelperFactory _urlHelperFactory;

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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var urlHelper = result.UrlHelper ?? _urlHelperFactory.GetUrlHelper(context);

            // IsLocalUrl is called to handle  Urls starting with '~/'.
            if (!urlHelper.IsLocalUrl(result.Url))
            {
                throw new InvalidOperationException(Resources.UrlNotLocal);
            }

            var destinationUrl = urlHelper.Content(result.Url);
            _logger.LocalRedirectResultExecuting(destinationUrl);

            if (result.PreserveMethod)
            {
                context.HttpContext.Response.StatusCode = result.Permanent ?
                    StatusCodes.Status308PermanentRedirect : StatusCodes.Status307TemporaryRedirect;
                context.HttpContext.Response.Headers[HeaderNames.Location] = destinationUrl;
            }
            else
            {
                context.HttpContext.Response.Redirect(destinationUrl, result.Permanent);
            }

            return Task.CompletedTask;
        }
    }
}
