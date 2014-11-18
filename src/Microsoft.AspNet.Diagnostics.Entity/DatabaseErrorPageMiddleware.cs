// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.AspNet.Diagnostics.Entity.Views;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.Migrations.Utilities;
using Microsoft.Data.Entity.Relational;
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
                    if (_loggerProvider.Logger.LastError.IsErrorLogged
                        && _loggerProvider.Logger.LastError.Exception == ex)
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
                                if (dbContext.Database is RelationalDatabase)
                                {
                                    var databaseExists = dbContext.Database.AsRelational().Exists();

                                    var services = (MigrationsDataStoreServices)dbContext.Configuration.DataStoreServices;

                                    var pendingMigrations = services.Migrator.GetPendingMigrations().Select(m => m.GetMigrationId());

                                    var pendingModelChanges = true;
                                    var snapshot = services.Migrator.MigrationAssembly.Model;
                                    if (snapshot != null)
                                    {
                                        pendingModelChanges = services.Migrator.ModelDiffer.Diff(snapshot, dbContext.Model).Any();
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
    }
}
