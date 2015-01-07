// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.AspNet.Diagnostics.Entity.Views;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Relational.Migrations;
using Microsoft.Data.Entity.Relational.Migrations.Infrastructure;
using Microsoft.Data.Entity.Relational.Migrations.Utilities;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DatabaseErrorPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DatabaseErrorPageOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly DataStoreErrorLoggerProvider _loggerProvider;

        public DatabaseErrorPageMiddleware([NotNull] RequestDelegate next, [NotNull] IServiceProvider serviceProvider, [NotNull] ILoggerFactory loggerFactory, [NotNull] DatabaseErrorPageOptions options)
        {
            Check.NotNull(next, "next");
            Check.NotNull(serviceProvider, "serviceProvider");
            Check.NotNull(loggerFactory, "loggerFactory");
            Check.NotNull(options, "options");

            _next = next;
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = loggerFactory.Create<DatabaseErrorPageMiddleware>();

            _loggerProvider = new DataStoreErrorLoggerProvider();
            loggerFactory.AddProvider(_loggerProvider);
        }

        public virtual async Task Invoke([NotNull] HttpContext context)
        {
            Check.NotNull(context, "context");

            try
            {
#if !ASPNETCORE50
                // TODO This probably isn't the correct place for this workaround, it 
                //      needs to be called before anything is written to CallContext
                // http://msdn.microsoft.com/en-us/library/dn458353(v=vs.110).aspx
                System.Configuration.ConfigurationManager.GetSection("system.xml/xmlReader");
#endif
                _loggerProvider.Logger.StartLoggingForCurrentCallContext();

                await _next(context).WithCurrentCulture();
            }
            catch (Exception ex)
            {
                try
                {
                    if (ShouldDisplayErrorPage(_loggerProvider.Logger.LastError, ex, _logger))
                    {
                        using (RequestServicesContainer.EnsureRequestServices(context, _serviceProvider))
                        {
                            var dbContextType = _loggerProvider.Logger.LastError.ContextType;
                            var dbContext = (DbContext)context.RequestServices.GetService(dbContextType);
                            if (dbContext == null)
                            {
                                _logger.WriteError(Strings.FormatDatabaseErrorPageMiddleware_ContextNotRegistered(dbContextType.FullName));
                            }
                            else
                            {
                                if (!(dbContext.Database is RelationalDatabase))
                                {
                                    _logger.WriteVerbose(Strings.DatabaseErrorPage_NotRelationalDatabase);
                                }
                                else
                                {
                                    var databaseExists = dbContext.Database.AsRelational().Exists();

                                    var databaseInternals = (IMigrationsEnabledDatabaseInternals)dbContext.Database;
                                    var migrator = databaseInternals.Migrator;

                                    var pendingMigrations = migrator.GetPendingMigrations().Select(m => m.GetMigrationId());

                                    var pendingModelChanges = true;
                                    var snapshot = migrator.MigrationAssembly.ModelSnapshot;
                                    if (snapshot != null)
                                    {
                                        pendingModelChanges = migrator.ModelDiffer.Diff(snapshot.Model, dbContext.Model).Any();
                                    }

                                    if ((!databaseExists && pendingMigrations.Any()) || pendingMigrations.Any() || pendingModelChanges)
                                    {
                                        var page = new DatabaseErrorPage();
                                        page.Model = new DatabaseErrorPageModel(dbContextType, ex, databaseExists, pendingModelChanges, pendingMigrations, _options);
                                        await page.ExecuteAsync(context).WithCurrentCulture();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.WriteError(Strings.DatabaseErrorPageMiddleware_Exception, e);
                }

                throw;
            }
        }

        private static bool ShouldDisplayErrorPage(DataStoreErrorLogger.DataStoreErrorLog lastError, Exception exception, ILogger logger)
        {
            logger.WriteVerbose(Strings.FormatDatabaseErrorPage_AttemptingToMatchException(exception.GetType()));

            if (!lastError.IsErrorLogged)
            {
                logger.WriteVerbose(Strings.DatabaseErrorPage_NoRecordedException);
                return false;
            }
            
            bool match = false;
            for (var e = exception; e != null && !match; e = e.InnerException)
            {
                match = lastError.Exception == e;
            }

            if (!match)
            {
                logger.WriteVerbose(Strings.DatabaseErrorPage_NoMatch);
                return false;
            }

            logger.WriteVerbose(Strings.DatabaseErrorPage_Matched);
            return true;
        }
    }
}
