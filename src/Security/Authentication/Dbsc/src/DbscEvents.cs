// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Allows subscribing to events raised during Device Bound Session Credentials processing.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class DbscEvents
{
    /// <summary>
    /// Invoked before the DBSC registration header is written.
    /// </summary>
    public Func<DbscRegistrationHeaderCreatingContext, Task> OnRegistrationHeaderCreating { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked after a DBSC session is successfully registered.
    /// </summary>
    public Func<DbscRegisteredContext, Task> OnSessionRegistered { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked after a DBSC session is successfully refreshed.
    /// </summary>
    public Func<DbscRefreshedContext, Task> OnSessionRefreshed { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked before the DBSC registration header is written.
    /// </summary>
    /// <param name="context">The <see cref="DbscRegistrationHeaderCreatingContext"/>.</param>
    public virtual Task RegistrationHeaderCreating(DbscRegistrationHeaderCreatingContext context)
        => OnRegistrationHeaderCreating(context);

    /// <summary>
    /// Invoked after a DBSC session is successfully registered.
    /// </summary>
    /// <param name="context">The <see cref="DbscRegisteredContext"/>.</param>
    public virtual Task SessionRegistered(DbscRegisteredContext context)
        => OnSessionRegistered(context);

    /// <summary>
    /// Invoked after a DBSC session is successfully refreshed.
    /// </summary>
    /// <param name="context">The <see cref="DbscRefreshedContext"/>.</param>
    public virtual Task SessionRefreshed(DbscRefreshedContext context)
        => OnSessionRefreshed(context);
}