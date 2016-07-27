// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Represents a middleware that rewrites urls imported from mod_rewrite, UrlRewrite, and code.
    /// </summary>
    public class RewriteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RewriteOptions _options;
        private readonly IFileProvider _fileProvider;

        /// <summary>
        /// Creates a new instance of <see cref="RewriteMiddleware"/> 
        /// </summary>
        /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
        /// <param name="hostingEnv">The Hosting Environment.</param>
        /// <param name="options">The middleware options, containing the rules to apply.</param>
        public RewriteMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, RewriteOptions options)
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
            _options = options;
            _fileProvider = _options.FileProvider ?? hostingEnv.WebRootFileProvider;
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
            var urlContext = new RewriteContext { HttpContext = context, FileProvider = _fileProvider };
            foreach (var rule in _options.Rules)
            {
                // Apply the rule
                var result = rule.ApplyRule(urlContext);
                switch (result.Result)
                {
                    case RuleTerminiation.Continue:
                        // Explicitly show that we continue executing rules
                        break;
                    case RuleTerminiation.ResponseComplete:
                        // TODO cache task for perf
                        return Task.FromResult(0);
                    case RuleTerminiation.StopRules:
                        return _next(context);
                }
            }
            return _next(context);
        }
    }
}
