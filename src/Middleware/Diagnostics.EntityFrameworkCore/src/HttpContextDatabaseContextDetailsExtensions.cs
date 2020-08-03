// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    internal static class HttpContextDatabaseContextDetailsExtensions
    {
        public static async Task<DatabaseContextDetails> GetContextDetailsAsync(this HttpContext httpContext, Type dbcontextType, ILogger logger)
        {
            // TODO: Decouple
            var context = (DbContext)httpContext.RequestServices.GetService(dbcontextType);
            // TODO: Decouple
            var relationalDatabaseCreator = context.GetService<IDatabaseCreator>() as IRelationalDatabaseCreator;
            if (relationalDatabaseCreator == null)
            {
                logger.NotRelationalDatabase();
            }
            else
            {
                var databaseExists = await relationalDatabaseCreator.ExistsAsync();

                if (databaseExists)
                {
                    databaseExists = await relationalDatabaseCreator.HasTablesAsync();
                }

                // TODO: Decouple
                var migrationsAssembly = context.GetService<IMigrationsAssembly>();
                // TODO: Decouple
                var modelDiffer = context.GetService<IMigrationsModelDiffer>();

                var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
                // TODO: Decouple
                if (snapshotModel is IConventionModel conventionModel)
                {
                    // TODO: Decouple
                    var conventionSet = context.GetService<IConventionSetBuilder>().CreateConventionSet();

                    // TODO: Decouple
                    var typeMappingConvention = conventionSet.ModelFinalizingConventions.OfType<TypeMappingConvention>().FirstOrDefault();
                    if (typeMappingConvention != null)
                    {
                        typeMappingConvention.ProcessModelFinalizing(conventionModel.Builder, null);
                    }

                    // TODO: Decouple
                    var relationalModelConvention = conventionSet.ModelFinalizedConventions.OfType<RelationalModelConvention>().FirstOrDefault();
                    if (relationalModelConvention != null)
                    {
                        snapshotModel = relationalModelConvention.ProcessModelFinalized(conventionModel);
                    }
                }

                // TODO: Decouple
                if (snapshotModel is IMutableModel mutableModel)
                {
                    snapshotModel = mutableModel.FinalizeModel();
                }

                // HasDifferences will return true if there is no model snapshot, but if there is an existing database
                // and no model snapshot then we don't want to show the error page since they are most likely targeting
                // and existing database and have just misconfigured their model

                return new DatabaseContextDetails
                {
                    Type = dbcontextType,
                    DatabaseExists = databaseExists,
                    PendingModelChanges = (!databaseExists || migrationsAssembly.ModelSnapshot != null)
                        && modelDiffer.HasDifferences(snapshotModel?.GetRelationalModel(), context.Model.GetRelationalModel()),
                    PendingMigrations = databaseExists
                        ? await context.Database.GetPendingMigrationsAsync()
                        : context.Database.GetMigrations()
                };
            }

            return null;
        }
    }
}
