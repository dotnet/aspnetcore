// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    /// <summary>
    /// Options for the <see cref="DatabaseErrorPageMiddleware"/>.
    /// </summary>
    public class DatabaseErrorPageOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether details about the exception that occurred
        /// are displayed on the error page.
        /// </summary>
        public virtual bool ShowExceptionDetails { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the names of pending migrations are listed
        /// on the error page.
        /// </summary>
        public virtual bool ListMigrations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the error page will allow the execution of
        /// migrations related commands when they may help solve the current error.
        /// </summary>
        public virtual bool EnableMigrationCommands { get; set; }

        /// <summary>
        /// Gets or sets the path that <see cref="MigrationsEndPointMiddleware"/> will listen
        /// for requests to execute migrations commands. The middleware is only registered if
        /// <see cref="EnableMigrationCommands"/> is set to true.
        /// </summary>
        public virtual PathString MigrationsEndPointPath { get; set; }
    }
}
