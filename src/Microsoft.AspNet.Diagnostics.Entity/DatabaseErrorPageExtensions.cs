// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using System;

namespace Microsoft.AspNet.Builder
{
    public static class DatabaseErrorPageExtensions
    {
        public static IApplicationBuilder UseDatabaseErrorPage([NotNull] this IApplicationBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            return builder.UseDatabaseErrorPage(options => options.EnableAll());
        }

        public static IApplicationBuilder UseDatabaseErrorPage([NotNull] this IApplicationBuilder builder, [NotNull] Action<DatabaseErrorPageOptions> optionsAction)
        {
            Check.NotNull(builder, nameof(builder));
            Check.NotNull(optionsAction, nameof(optionsAction));

            var options = new DatabaseErrorPageOptions();
            optionsAction(options);

            builder = builder.UseMiddleware<DatabaseErrorPageMiddleware>(options);

            if(options.EnableMigrationCommands)
            {
                builder.UseMigrationsEndPoint(o => o.Path = options.MigrationsEndPointPath);
            }

            return builder;
        }

        public static void EnableAll([NotNull] this DatabaseErrorPageOptions options)
        {
            Check.NotNull(options, nameof(options));

            options.ShowExceptionDetails = true;
            options.ListMigrations = true;
            options.EnableMigrationCommands = true;
            options.MigrationsEndPointPath = MigrationsEndPointOptions.DefaultPath;
        }
    }
}
