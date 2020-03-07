// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Rewrite.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Represents a middleware that rewrites urls
    /// </summary>
    public class RewriteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RewriteOptions _options;
        private readonly IFileProvider _fileProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of <see cref="RewriteMiddleware"/>
        /// </summary>
        /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
        /// <param name="hostingEnvironment">The Hosting Environment.</param>
        /// <param name="loggerFactory">The Logger Factory.</param>
        /// <param name="options">The middleware options, containing the rules to apply.</param>
        public RewriteMiddleware(
            RequestDelegate next,
            IWebHostEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory,
            IOptions<RewriteOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options.Value;
            _fileProvider = _options.StaticFileProvider ?? hostingEnvironment.WebRootFileProvider;
            _logger = loggerFactory.CreateLogger<RewriteMiddleware>();
        }

        /// <summary>
        /// Executes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A task that represents the execution of this middleware.</returns>
        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var rewriteContext = new RewriteContext
            {
                HttpContext = context,
                StaticFileProvider = _fileProvider,
                Logger = _logger,
                Result = RuleResult.ContinueRules
            };

            foreach (var rule in _options.Rules)
            {
                rule.ApplyRule(rewriteContext);
                switch (rewriteContext.Result)
                {
                    case RuleResult.ContinueRules:
                        _logger.RewriteMiddlewareRequestContinueResults(context.Request.GetEncodedUrl());
                        break;
                    case RuleResult.EndResponse:
                        _logger.RewriteMiddlewareRequestResponseComplete(
                            context.Response.Headers[HeaderNames.Location],
                            context.Response.StatusCode);
                        return Task.CompletedTask;
                    case RuleResult.SkipRemainingRules:
                        _logger.RewriteMiddlewareRequestStopRules(context.Request.GetEncodedUrl());
                        return _next(context);
                    default:
                        throw new ArgumentOutOfRangeException($"Invalid rule termination {rewriteContext.Result}");
                }
            }
            return _next(context);
        }
    }
}
