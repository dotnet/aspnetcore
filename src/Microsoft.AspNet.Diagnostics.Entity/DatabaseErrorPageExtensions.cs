// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;

namespace Microsoft.AspNet.Builder
{
    public static class DatabaseErrorPageExtensions
    {
        public static IApplicationBuilder UseDatabaseErrorPage([NotNull] this IApplicationBuilder builder)
        {
            Check.NotNull(builder, "builder");

            return builder.UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);
        }

        public static IApplicationBuilder UseDatabaseErrorPage([NotNull] this IApplicationBuilder builder, [NotNull] DatabaseErrorPageOptions options)
        {
            Check.NotNull(builder, "builder");
            Check.NotNull(options, "options");

            builder = builder.UseMiddleware<DatabaseErrorPageMiddleware>(options);

            if(options.EnableMigrationCommands)
            {
                builder.UseMigrationsEndPoint(new MigrationsEndPointOptions { Path = options.MigrationsEndPointPath });
            }

            return builder;
        }
    }
}
