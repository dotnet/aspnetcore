// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    /// <summary>
    /// Processes requests to execute migrations operations. The middleware will listen for requests to the path configured in the supplied options.
    /// </summary>
    public class MigrationsEndPointMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly MigrationsEndPointOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationsEndPointMiddleware"/> class
        /// </summary>
        /// <param name="next">Delegate to execute the next piece of middleware in the request pipeline.</param>
        /// <param name="logger">The <see cref="Logger{T}"/> to write messages to.</param>
        /// <param name="options">The options to control the behavior of the middleware.</param>
        public MigrationsEndPointMiddleware(
            RequestDelegate next, 
            ILogger<MigrationsEndPointMiddleware> logger, 
            IOptions<MigrationsEndPointOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="context">The context for the current request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Request.Path.Equals(_options.Path))
            {
                _logger.RequestPathMatched(context.Request.Path);

                var db = await GetDbContext(context, _logger);

                if (db != null)
                {
                    try
                    {
                        _logger.ApplyingMigrations(db.GetType().FullName);

                        db.Database.Migrate();

                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                        context.Response.Headers.Add("Pragma", new[] { "no-cache" });
                        context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });

                        _logger.MigrationsApplied(db.GetType().FullName);
                    }
                    catch (Exception ex)
                    {
                        var message = Strings.FormatMigrationsEndPointMiddleware_Exception(db.GetType().FullName) + ex;

                        _logger.MigrationsEndPointMiddlewareException(db.GetType().FullName, ex);

                        throw new InvalidOperationException(message, ex);
                    }
                }
            }
            else
            {
                await _next(context);
            }
        }

        private static async Task<DbContext> GetDbContext(HttpContext context, ILogger logger)
        {
            var form = await context.Request.ReadFormAsync();
            var contextTypeName = form["context"];

            if (string.IsNullOrWhiteSpace(contextTypeName))
            {
                logger.NoContextType();

                await WriteErrorToResponse(context.Response, Strings.MigrationsEndPointMiddleware_NoContextType);

                return null;
            }

            var contextType = Type.GetType(contextTypeName);

            if (contextType == null)
            {
                var message = Strings.FormatMigrationsEndPointMiddleware_InvalidContextType(contextTypeName);

                logger.InvalidContextType(contextTypeName);

                await WriteErrorToResponse(context.Response, message);

                return null;
            }

            var db = (DbContext)context.RequestServices.GetService(contextType);

            if (db == null)
            {
                var message = Strings.FormatMigrationsEndPointMiddleware_ContextNotRegistered(contextType.FullName);

                logger.ContextNotRegistered(contextType.FullName);

                await WriteErrorToResponse(context.Response, message);

                return null;
            }

            return db;
        }

        private static async Task WriteErrorToResponse(HttpResponse response, string error)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Headers.Add("Pragma", new[] { "no-cache" });
            response.Headers.Add("Cache-Control", new[] { "no-cache" });
            response.ContentType = "text/plain";

            // Padding to >512 to ensure IE doesn't hide the message
            // http://stackoverflow.com/questions/16741062/what-rules-does-ie-use-to-determine-whether-to-show-the-entity-body
            await response.WriteAsync(error.PadRight(513));
        }
    }
}
