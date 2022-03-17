// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Models;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

/// <summary>
/// Constants for special values defined for specific <see cref="ApplicationProfilesPropertyNames" /> keys.
/// </summary>
public static class ApplicationProfilesPropertyValues
{
    /// <summary>
    /// The value given to <see cref="ApplicationProfilesPropertyNames.Clients"/> in <see cref="Resource.Properties"/> to indicate that the
    /// resource can be accessed by all configured clients.
    /// </summary>
    public const string AllowAllApplications = "*";

    /// <summary>
    /// The value given to <see cref="ApplicationProfilesPropertyNames.Source"/> in <see cref="Resource.Properties"/> or <see cref="Client.Properties"/>
    /// to indicate that the application was defined in configuration.
    /// </summary>
    public const string Configuration = nameof(Configuration);

    /// <summary>
    /// The value given to <see cref="ApplicationProfilesPropertyNames.Source"/> in <see cref="Resource.Properties"/>
    /// to indicate that the resource was defined as a default identity resource.
    /// </summary>
    public const string Default = nameof(Default);
}
