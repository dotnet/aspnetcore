// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides an interface for implmenting a construct that provides
/// access to specific configuration sections.
/// </summary>
public interface IAuthenticationConfigurationProvider
{
    /// <summary>
    /// Returns the specified <see cref="ConfigurationSection"/> object.
    /// </summary>
    /// <param name="authenticationScheme">The path to the section to be returned.</param>
    /// <returns>The specified <see cref="ConfigurationSection"/> object, or null if the requested section does not exist.</returns>
    IConfiguration GetAuthenticationSchemeConfiguration(string authenticationScheme);
}
