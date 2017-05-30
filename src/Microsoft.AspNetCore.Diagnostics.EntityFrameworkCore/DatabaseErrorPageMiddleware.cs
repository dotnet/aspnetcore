// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    /// <summary>
    /// Captures synchronous and asynchronous database related exceptions from the pipeline that may be resolved using Entity Framework
    /// migrations. When these exceptions occur an HTML response with details of possible actions to resolve the issue is generated.
    /// </summary>
    public class DatabaseErrorPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DatabaseErrorPageOptions _options;
        private readonly ILogger _logger;
        private readonly DataStoreErrorLoggerProvider _loggerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseErrorPageMiddleware"/> class
        /// </summary>
        /// <param name="next">Delegate to execute the next piece of middleware in the request pipeline.</param>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> for the application. This middleware both produces logging messages and
        /// consumes them to detect database related exception.
        /// </param>
        /// <param name="options">The options to control what information is displayed on the error page.</param>
        public DatabaseErrorPageMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<DatabaseErrorPageOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<DatabaseErrorPageMiddleware>();

            _loggerProvider = new DataStoreErrorLoggerProvider();
#pragma warning disable CS0618 // Type or member is obsolete
            loggerFactory.AddProvider(_loggerProvider);
#pragma warning restore CS0618 // Type or member is obsolete
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

            try
            {
                _loggerProvider.Logger.StartLoggingForCurrentCallContext();

                await _next(context);
            }
            catch (Exception ex)
            {
                try
                {
                    if (ShouldDisplayErrorPage(_loggerProvider.Logger.LastError, ex, _logger))
                    {
                        var dbContextType = _loggerProvider.Logger.LastError.ContextType;
                        var dbContext = (DbContext) context.RequestServices.GetService(dbContextType);

                        if (dbContext == null)
                        {
                            _logger.LogError(
                                Strings.FormatDatabaseErrorPageMiddleware_ContextNotRegistered(dbContextType.FullName));
                        }
                        else
                        {
                            var creator = dbContext.GetService<IDatabaseCreator>() as IRelationalDatabaseCreator;

                            if (creator == null)
                            {
                                _logger.LogDebug(Strings.DatabaseErrorPage_NotRelationalDatabase);
                            }
                            else
                            {
                                var databaseExists = creator.Exists();

                                var historyRepository = dbContext.GetService<IHistoryRepository>();
                                var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
                                var modelDiffer = dbContext.GetService<IMigrationsModelDiffer>();

                                var appliedMigrations = historyRepository.GetAppliedMigrations();

                                var pendingMigrations
                                    = (from m in migrationsAssembly.Migrations
                                        where !appliedMigrations.Any(
                                            r => string.Equals(r.MigrationId, m.Key,
                                                StringComparison.OrdinalIgnoreCase))
                                        select m.Key)
                                    .ToList();

                                // HasDifferences will return true if there is no model snapshot, but if there is an existing database
                                // and no model snapshot then we don't want to show the error page since they are most likely targeting
                                // and existing database and have just misconfigured their model
                                var pendingModelChanges
                                    = (migrationsAssembly.ModelSnapshot != null || !databaseExists)
                                      && modelDiffer.HasDifferences(migrationsAssembly.ModelSnapshot?.Model,
                                          dbContext.Model);

                                if (!databaseExists && pendingMigrations.Any()
                                    || pendingMigrations.Any()
                                    || pendingModelChanges)
                                {
                                    var page = new DatabaseErrorPage
                                    {
                                        Model = new DatabaseErrorPageModel(
                                            dbContextType, ex, databaseExists, pendingModelChanges, pendingMigrations,
                                            _options)
                                    };

                                    await page.ExecuteAsync(context);

                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(0, e, Strings.DatabaseErrorPageMiddleware_Exception);
                }

                throw;
            }
        }

        private static bool ShouldDisplayErrorPage(DataStoreErrorLogger.DataStoreErrorLog lastError,
            Exception exception, ILogger logger)
        {
            logger.LogDebug(Strings.FormatDatabaseErrorPage_AttemptingToMatchException(exception.GetType()));

            if (!lastError.IsErrorLogged)
            {
                logger.LogDebug(Strings.DatabaseErrorPage_NoRecordedException);
                return false;
            }

            var match = false;

            for (var e = exception; e != null && !match; e = e.InnerException)
            {
                match = lastError.Exception == e;
            }

            if (!match)
            {
                logger.LogDebug(Strings.DatabaseErrorPage_NoMatch);

                return false;
            }

            logger.LogDebug(Strings.DatabaseErrorPage_Matched);

            return true;
        }
    }
}