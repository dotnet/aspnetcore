// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for the <see cref="MigrationsEndPointMiddleware"/>.
    /// </summary>
    public class MigrationsEndPointOptions
    {
        /// <summary>
        /// The default value for <see cref="Path"/>.
        /// </summary>
        public static PathString DefaultPath = new PathString("/ApplyDatabaseMigrations");

        /// <summary>
        /// Gets or sets the path that the <see cref="MigrationsEndPointMiddleware"/> will listen
        /// for requests to execute migrations commands.
        /// </summary>
        public virtual PathString Path { get; set; } = DefaultPath;
    }
}
