// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Defines the set of data required to apply authorization rules to a resource.
/// </summary>
public interface IAuthorizeData
{
    /// <summary>
    /// Gets or sets the policy name that determines access to the resource.
    /// </summary>
    string? Policy { get; set; }

    /// <summary>
    /// Gets or sets a comma delimited list of roles that are allowed to access the resource.
    /// </summary>
    string? Roles { get; set; }

    /// <summary>
    /// Gets or sets a comma delimited list of schemes from which user information is constructed.
    /// </summary>
    string? AuthenticationSchemes { get; set; }
}
