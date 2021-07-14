// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Result
{
    internal sealed partial class RedirectResult : IResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectResult"/> class with the values
        /// provided.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
        public RedirectResult(string url, bool permanent, bool preserveMethod)
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
            Url = url;
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
        /// Gets or sets the URL to redirect to.
        /// </summary>
        public string Url { get; }

        /// <inheritdoc />
        public Task ExecuteAsync(HttpContext httpContext)
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<RedirectResult>>();

            // IsLocalUrl is called to handle URLs starting with '~/'.
            var destinationUrl = SharedUrlHelper.IsLocalUrl(Url) ? SharedUrlHelper.Content(httpContext, Url) : Url;

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
}
