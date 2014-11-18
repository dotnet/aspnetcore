// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Framework.DependencyInjection;
using System.Net;
using Microsoft.Framework.Logging;
using Microsoft.AspNet.RequestContainer;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class MigrationsEndPointMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly MigrationsEndPointOptions _options;

        public MigrationsEndPointMiddleware([NotNull] RequestDelegate next, [NotNull] IServiceProvider serviceProvider, [NotNull] ILoggerFactory loggerFactory, [NotNull] MigrationsEndPointOptions options)
        {
            Check.NotNull(next, "next");
            Check.NotNull(serviceProvider, "serviceProvider");
            Check.NotNull(loggerFactory, "loggerFactory");
            Check.NotNull(options, "options");

            _next = next;
            _serviceProvider = serviceProvider;
            _logger = loggerFactory.Create<MigrationsEndPointMiddleware>();
            _options = options;
        }

        public virtual async Task Invoke([NotNull] HttpContext context)
        {
            Check.NotNull(context, "context");

            if (context.Request.Path.Equals(_options.Path))
            {
                _logger.WriteVerbose(Strings.FormatMigrationsEndPointMiddleware_RequestPathMatched(context.Request.Path));

                using (RequestServicesContainer.EnsureRequestServices(context, _serviceProvider))
                { 
                    var db = await GetDbContext(context, _logger).WithCurrentCulture();
                    if (db != null)
                    {
                        try
                        {
                            _logger.WriteVerbose(Strings.FormatMigrationsEndPointMiddleware_ApplyingMigrations(db.GetType().FullName));

                            db.Database.AsMigrationsEnabled().ApplyMigrations();

                            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                            context.Response.Headers.Add("Pragma", new[] { "no-cache" });
                            context.Response.Headers.Add("Cache-Control", new[] { "no-cache" });

                            _logger.WriteVerbose(Strings.FormatMigrationsEndPointMiddleware_Applied(db.GetType().FullName));
                        }
                        catch (Exception ex)
                        {
                            var message = Strings.FormatMigrationsEndPointMiddleware_Exception(db.GetType().FullName);
                            _logger.WriteError(message);
                            throw new InvalidOperationException(message, ex);
                        }
                    }
                }
            }
            else
            {
                await _next(context).WithCurrentCulture();
            }
        }

        private static async Task<DbContext> GetDbContext(HttpContext context, ILogger logger)
        {
            var form = await context.Request.GetFormAsync().WithCurrentCulture();
            var contextTypeName = form["context"];
            if (string.IsNullOrWhiteSpace(contextTypeName))
            {
                logger.WriteError(Strings.MigrationsEndPointMiddleware_NoContextType);
                await WriteErrorToResponse(context.Response, Strings.MigrationsEndPointMiddleware_NoContextType).WithCurrentCulture();
                return null;
            }

            var contextType = Type.GetType(contextTypeName);
            if (contextType == null)
            {
                var message = Strings.FormatMigrationsEndPointMiddleware_InvalidContextType(contextTypeName);
                logger.WriteError(message);
                await WriteErrorToResponse(context.Response, message).WithCurrentCulture();
                return null;
            }

            var db = (DbContext)context.RequestServices.GetService(contextType);
            if (db == null)
            {
                var message = Strings.FormatMigrationsEndPointMiddleware_ContextNotRegistered(contextType.FullName);
                logger.WriteError(message);
                await WriteErrorToResponse(context.Response, message).WithCurrentCulture();
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
            await response.WriteAsync(error.PadRight(513)).WithCurrentCulture();
        }
    }
}
