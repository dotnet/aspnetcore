// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Options for the <see cref="DatabaseErrorPageMiddleware"/>.
    /// </summary>
    public class DatabaseErrorPageOptions
    {
        /// <summary>
        /// Gets or sets the path that <see cref="MigrationsEndPointMiddleware"/> will listen
        /// for requests to execute migrations commands. The middleware is only registered if
        /// <see cref="EnableMigrationCommands"/> is set to true.
        /// </summary>
        public virtual PathString MigrationsEndPointPath { get; set; } = MigrationsEndPointOptions.DefaultPath;
    }
}
