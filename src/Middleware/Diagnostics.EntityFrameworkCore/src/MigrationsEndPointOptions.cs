// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for the <see cref="MigrationsEndPointMiddleware"/>.
    /// </summary>
    [Obsolete("This is obsolete and will be removed in a future version. Use the Package Manager Console in Visual Studio or dotnet-ef tool on the command line to apply migrations.", error: true)]
    public class MigrationsEndPointOptions
    {
        /// <summary>
        /// The default value for <see cref="Path"/>.
        /// </summary>
        [Obsolete("This is obsolete and will be removed in a future version. Use the Package Manager Console in Visual Studio or dotnet-ef tool on the command line to apply migrations.", error: true)]
        public static PathString DefaultPath = new PathString("/ApplyDatabaseMigrations");

        /// <summary>
        /// Gets or sets the path that the <see cref="MigrationsEndPointMiddleware"/> will listen
        /// for requests to execute migrations commands.
        /// </summary>
        public virtual PathString Path { get; set; } = DefaultPath;
    }
}
