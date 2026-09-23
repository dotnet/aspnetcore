// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Context for the event raised after a DBSC session is successfully registered.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class DbscRegisteredContext : PropertiesContext<DbscOptions>
{
    /// <summary>
    /// Creates a new instance of the context object.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <param name="scheme">The DBSC authentication scheme.</param>
    /// <param name="options">The DBSC authentication options.</param>
    /// <param name="principal">The principal associated with the registered session.</param>
    /// <param name="properties">The authentication properties associated with the registered session.</param>
    public DbscRegisteredContext(
        HttpContext context,
        AuthenticationScheme scheme,
        DbscOptions options,
        ClaimsPrincipal principal,
        AuthenticationProperties? properties)
        : base(context, scheme, options, properties)
    {
        Principal = principal;
    }

    /// <summary>
    /// Gets or sets the principal associated with the registered session.
    /// </summary>
    public ClaimsPrincipal Principal { get; set; }
}