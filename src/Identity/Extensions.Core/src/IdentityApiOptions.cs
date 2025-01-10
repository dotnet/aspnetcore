// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Options for configuring the Identity API.
/// </summary>
public class IdentityApiOptions
{
    /// <summary>
    /// Gets or sets the tag used for the Identity API.
    /// </summary>
    public string Tag { get; set; } = "identity";

    /// <summary>
    /// Gets or sets the group name used for the Identity API.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manage group name used for the Identity API.
    /// </summary>
    public string ManageGroupName { get; set; } = "manage";

    /// <summary>
    /// Gets or sets the endpoints options for the Identity API.
    /// </summary>
    public IdentityApiEndpointsOptions Endpoints { get; set; } = new IdentityApiEndpointsOptions();
}
