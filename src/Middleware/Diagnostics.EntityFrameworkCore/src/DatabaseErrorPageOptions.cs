// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for the <see cref="DatabaseErrorPageMiddleware"/>.
/// </summary>
public class DatabaseErrorPageOptions
{
    /// <summary>
    /// Gets or sets the path that <see cref="MigrationsEndPointMiddleware"/> will listen
    /// for requests to execute migrations commands.
    /// </summary>
    public virtual PathString MigrationsEndPointPath { get; set; } = MigrationsEndPointOptions.DefaultPath;
}
